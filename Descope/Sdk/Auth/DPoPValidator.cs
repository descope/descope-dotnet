using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;

namespace Descope;

/// <summary>
/// Validates DPoP (Demonstrated Proof of Possession) proofs per RFC 9449.
/// </summary>
internal static class DPoPValidator
{
    private const int MaxProofLength = 8192;
    private const int IatBackwardWindowSeconds = 60;
    private const int IatForwardWindowSeconds = 5;

    private static readonly HashSet<string> AllowedAlgorithms = new HashSet<string>(StringComparer.Ordinal)
    {
        "RS256", "RS384", "RS512",
        "ES256", "ES384", "ES512",
        "PS256", "PS384", "PS512",
        "EdDSA"
    };

    /// <summary>
    /// Validates a DPoP proof JWT against the provided access token.
    /// If the session token has no cnf.jkt claim, validation is skipped (token is not DPoP-bound).
    /// </summary>
    /// <param name="dpopProof">The DPoP proof JWT from the DPoP header.</param>
    /// <param name="method">The HTTP method (e.g. "GET", "POST").</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="sessionToken">The session/access token JWT string.</param>
    /// <exception cref="DescopeException">Thrown if the DPoP proof is invalid.</exception>
    public static void ValidateDPoPProof(string dpopProof, string method, string requestUrl, string sessionToken)
    {
        // Extract the JWK thumbprint from the session token.
        // If there is no cnf.jkt claim, the token is not DPoP-bound — skip validation.
        var storedJkt = GetJktFromToken(sessionToken);
        if (string.IsNullOrEmpty(storedJkt))
        {
            return;
        }

        dpopProof = dpopProof?.Trim() ?? string.Empty;

        if (dpopProof.Length > MaxProofLength)
            throw new DescopeException("DPoP proof exceeds maximum length");

        if (dpopProof.Length == 0)
            throw new DescopeException("DPoP proof required");

        var parts = dpopProof.Split('.');
        if (parts.Length != 3)
            throw new DescopeException("malformed DPoP JWT");

        // --- Parse header ---
        Dictionary<string, object>? header;
        try
        {
            var headerBytes = Base64UrlDecode(parts[0]);
            header = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(headerBytes);
        }
        catch (Exception ex)
        {
            throw new DescopeException("failed to parse DPoP header", ex);
        }

        if (header == null)
            throw new DescopeException("failed to parse DPoP header");

        if (!TryGetString(header, "typ", out var typ) || typ != "dpop+jwt")
            throw new DescopeException("typ must be dpop+jwt");

        if (!TryGetString(header, "alg", out var alg) || string.IsNullOrEmpty(alg))
            throw new DescopeException("missing alg in DPoP header");

        if (!AllowedAlgorithms.Contains(alg))
            throw new DescopeException($"rejected algorithm: {alg}");

        if (!TryGetObject(header, "jwk", out var jwk) || jwk == null)
            throw new DescopeException("missing jwk header");

        if (TryGetString(jwk, "kty", out var kty) && kty == "oct")
            throw new DescopeException("symmetric key not allowed");

        if (jwk.ContainsKey("d"))
            throw new DescopeException("jwk must not contain a private key");

        // --- Verify JWS signature ---
        var signingInput = Encoding.UTF8.GetBytes(parts[0] + "." + parts[1]);
        byte[] signatureBytes;
        try
        {
            signatureBytes = Base64UrlDecode(parts[2]);
        }
        catch (Exception ex)
        {
            throw new DescopeException("failed to decode DPoP signature", ex);
        }

        VerifySignature(alg, jwk, signingInput, signatureBytes);

        // --- Parse payload ---
        Dictionary<string, object>? payload;
        try
        {
            var payloadBytes = Base64UrlDecode(parts[1]);
            payload = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(payloadBytes);
        }
        catch (Exception ex)
        {
            throw new DescopeException("failed to parse DPoP payload", ex);
        }

        if (payload == null)
            throw new DescopeException("failed to parse DPoP payload");

        if (!TryGetString(payload, "jti", out var jti) || string.IsNullOrEmpty(jti))
            throw new DescopeException("missing or empty jti claim");

        if (!TryGetString(payload, "htm", out var htm) || string.IsNullOrEmpty(htm))
            throw new DescopeException("missing or empty htm claim");

        if (!TryGetString(payload, "htu", out var htu) || string.IsNullOrEmpty(htu))
            throw new DescopeException("missing or empty htu claim");

        if (!string.Equals(htm, method, StringComparison.OrdinalIgnoreCase))
            throw new DescopeException($"htm mismatch: expected {method}, got {htm}");

        if (!HtuMatches(htu, requestUrl))
            throw new DescopeException($"htu mismatch: expected {requestUrl}, got {htu}");

        // --- Validate iat ---
        long iat;
        if (!TryGetLong(payload, "iat", out iat))
            throw new DescopeException("missing or invalid iat claim");

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var diff = now - iat;

        if (diff <= -IatForwardWindowSeconds || diff >= IatBackwardWindowSeconds)
            throw new DescopeException("iat out of acceptable window");

        // --- Validate ath (access token hash) ---
        if (!TryGetString(payload, "ath", out var ath) || string.IsNullOrEmpty(ath))
            throw new DescopeException("missing ath claim");

        var tokenBytes = Encoding.UTF8.GetBytes(sessionToken);
#if NET6_0_OR_GREATER
        var tokenHash = SHA256.HashData(tokenBytes);
#else
        byte[] tokenHash;
        using (var sha = SHA256.Create())
        {
            tokenHash = sha.ComputeHash(tokenBytes);
        }
#endif
        var expectedAth = Base64UrlEncode(tokenHash);
        if (ath != expectedAth)
            throw new DescopeException("ath mismatch");

        // --- Validate JWK thumbprint against cnf.jkt ---
        if (!TryGetString(jwk, "kty", out var jwkKty) || string.IsNullOrEmpty(jwkKty))
            throw new DescopeException("missing kty in jwk");

        var thumbprint = ComputeJwkThumbprint(jwkKty, jwk);
        if (thumbprint != storedJkt)
            throw new DescopeException("DPoP key mismatch: jwk thumbprint does not match cnf.jkt");
    }

    /// <summary>
    /// Extracts the cnf.jkt claim from a raw session JWT string without full validation.
    /// Returns empty string if not present or on any parse error.
    /// </summary>
    public static string GetJktFromToken(string sessionToken)
    {
        if (string.IsNullOrEmpty(sessionToken))
            return string.Empty;

        try
        {
            var parts = sessionToken.Split('.');
            if (parts.Length < 2)
                return string.Empty;

            var payloadBytes = Base64UrlDecode(parts[1]);
            var payload = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(payloadBytes);
            if (payload == null)
                return string.Empty;

            if (!TryGetObject(payload, "cnf", out var cnf) || cnf == null)
                return string.Empty;

            if (!TryGetString(cnf, "jkt", out var jkt))
                return string.Empty;

            return jkt ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private static void VerifySignature(
        string alg,
        Dictionary<string, object> jwk,
        byte[] signingInput,
        byte[] signature)
    {
        try
        {
            if (alg.StartsWith("RS", StringComparison.Ordinal) || alg.StartsWith("PS", StringComparison.Ordinal))
            {
                VerifyRsaSignature(alg, jwk, signingInput, signature);
            }
            else if (alg.StartsWith("ES", StringComparison.Ordinal))
            {
                VerifyEcSignature(alg, jwk, signingInput, signature);
            }
            else if (alg == "EdDSA")
            {
                // EdDSA (Ed25519) is listed as an allowed algorithm in RFC 9449, but .NET does not
                // expose a stable public API for Ed25519 signature verification across all target
                // frameworks. Support may be added in a future release.
                throw new DescopeException("EdDSA DPoP proofs are not yet supported by this SDK");
            }
            else
            {
                throw new DescopeException($"unsupported algorithm: {alg}");
            }
        }
        catch (DescopeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new DescopeException("DPoP signature verification failed", ex);
        }
    }

    private static void VerifyRsaSignature(
        string alg,
        Dictionary<string, object> jwk,
        byte[] signingInput,
        byte[] signature)
    {
        if (!TryGetString(jwk, "n", out var n) || !TryGetString(jwk, "e", out var e)
            || string.IsNullOrEmpty(n) || string.IsNullOrEmpty(e))
            throw new DescopeException("RSA jwk missing n or e");

        using var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters
        {
            Modulus = Base64UrlDecode(n!),
            Exponent = Base64UrlDecode(e!),
        });

        var (hashAlg, padding) = alg switch
        {
            "RS256" => (HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
            "RS384" => (HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1),
            "RS512" => (HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1),
            "PS256" => (HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
            "PS384" => (HashAlgorithmName.SHA384, RSASignaturePadding.Pss),
            "PS512" => (HashAlgorithmName.SHA512, RSASignaturePadding.Pss),
            _ => throw new DescopeException($"unsupported RSA algorithm: {alg}")
        };

        if (!rsa.VerifyData(signingInput, signature, hashAlg, padding))
            throw new DescopeException("DPoP signature verification failed");
    }

    private static void VerifyEcSignature(
        string alg,
        Dictionary<string, object> jwk,
        byte[] signingInput,
        byte[] signature)
    {
        if (!TryGetString(jwk, "x", out var x) || !TryGetString(jwk, "y", out var y)
            || string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y))
            throw new DescopeException("EC jwk missing x or y");

        if (!TryGetString(jwk, "crv", out var crv) || string.IsNullOrEmpty(crv))
            throw new DescopeException("EC jwk missing crv");

        var (curve, hashAlg) = crv switch
        {
            "P-256" => (ECCurve.NamedCurves.nistP256, HashAlgorithmName.SHA256),
            "P-384" => (ECCurve.NamedCurves.nistP384, HashAlgorithmName.SHA384),
            "P-521" => (ECCurve.NamedCurves.nistP521, HashAlgorithmName.SHA512),
            _ => throw new DescopeException($"unsupported EC curve: {crv}")
        };

        using var ec = ECDsa.Create();
        ec.ImportParameters(new ECParameters
        {
            Curve = curve,
            Q = new ECPoint
            {
                X = Base64UrlDecode(x!),
                Y = Base64UrlDecode(y!),
            }
        });

        // DPoP/JWT EC signatures are in raw IEEE P1363 (R||S) format.
#if NET6_0_OR_GREATER
        if (!ec.VerifyData(signingInput, signature, hashAlg, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
            throw new DescopeException("DPoP signature verification failed");
#else
        // Convert raw R||S to DER for netstandard2.0 / older targets.
        var derSig = ConvertRawToDer(signature);
        if (!ec.VerifyData(signingInput, derSig, hashAlg))
            throw new DescopeException("DPoP signature verification failed");
#endif
    }

    /// <summary>
    /// Converts a raw R||S EC signature to DER format (for netstandard2.0 compatibility).
    /// </summary>
    private static byte[] ConvertRawToDer(byte[] rawSig)
    {
        int halfLen = rawSig.Length / 2;
        var r = new byte[halfLen];
        var s = new byte[halfLen];
        Array.Copy(rawSig, 0, r, 0, halfLen);
        Array.Copy(rawSig, halfLen, s, 0, halfLen);

        // Strip leading zeros, then add 0x00 prefix if high bit set.
        r = TrimAndPadInteger(r);
        s = TrimAndPadInteger(s);

        int seqContentLen = 2 + r.Length + 2 + s.Length;
        var der = new byte[2 + seqContentLen];
        int pos = 0;
        der[pos++] = 0x30; // SEQUENCE
        der[pos++] = (byte)seqContentLen;
        der[pos++] = 0x02; // INTEGER
        der[pos++] = (byte)r.Length;
        Array.Copy(r, 0, der, pos, r.Length);
        pos += r.Length;
        der[pos++] = 0x02; // INTEGER
        der[pos++] = (byte)s.Length;
        Array.Copy(s, 0, der, pos, s.Length);
        return der;
    }

    private static byte[] TrimAndPadInteger(byte[] val)
    {
        // Remove leading zeros.
        int start = 0;
        while (start < val.Length - 1 && val[start] == 0)
            start++;
        var trimmed = new byte[val.Length - start];
        Array.Copy(val, start, trimmed, 0, trimmed.Length);

        // If high bit set, prepend 0x00 to indicate positive integer.
        if (trimmed[0] >= 0x80)
        {
            var padded = new byte[trimmed.Length + 1];
            padded[0] = 0x00;
            Array.Copy(trimmed, 0, padded, 1, trimmed.Length);
            return padded;
        }
        return trimmed;
    }

    /// <summary>
    /// Checks that the htu claim matches the request URL (scheme+host+path, no query/fragment).
    /// </summary>
    private static bool HtuMatches(string htu, string requestUrl)
    {
        try
        {
            var htuUri = new Uri(htu);
            var reqUri = new Uri(requestUrl);

            // Normalize: lowercase scheme and host, strip default ports, strip query/fragment.
            var htuNorm = NormalizeUri(htuUri);
            var reqNorm = NormalizeUri(reqUri);
            return string.Equals(htuNorm, reqNorm, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeUri(Uri uri)
    {
        var scheme = uri.Scheme.ToLowerInvariant();
        var host = uri.Host.ToLowerInvariant();
        var path = uri.AbsolutePath;

        // Determine whether the port is the default for the scheme.
        bool isDefault = (scheme == "https" && uri.Port == 443)
                      || (scheme == "http" && uri.Port == 80)
                      || uri.IsDefaultPort;

        var authority = isDefault ? host : $"{host}:{uri.Port}";
        return $"{scheme}://{authority}{path}";
    }

    /// <summary>
    /// Computes the RFC 7638 JWK thumbprint.
    /// </summary>
    private static string ComputeJwkThumbprint(string kty, Dictionary<string, object> jwk)
    {
        // Required members per key type, in lexicographic order.
        var members = new SortedDictionary<string, string>(StringComparer.Ordinal);

        switch (kty)
        {
            case "EC":
                if (!TryGetString(jwk, "crv", out var ecCrv) || string.IsNullOrEmpty(ecCrv))
                    throw new DescopeException("EC jwk missing crv for thumbprint");
                if (!TryGetString(jwk, "x", out var ecX) || string.IsNullOrEmpty(ecX))
                    throw new DescopeException("EC jwk missing x for thumbprint");
                if (!TryGetString(jwk, "y", out var ecY) || string.IsNullOrEmpty(ecY))
                    throw new DescopeException("EC jwk missing y for thumbprint");
                members["crv"] = ecCrv!;
                members["kty"] = "EC";
                members["x"] = ecX!;
                members["y"] = ecY!;
                break;

            case "RSA":
                if (!TryGetString(jwk, "e", out var rsaE) || string.IsNullOrEmpty(rsaE))
                    throw new DescopeException("RSA jwk missing e for thumbprint");
                if (!TryGetString(jwk, "n", out var rsaN) || string.IsNullOrEmpty(rsaN))
                    throw new DescopeException("RSA jwk missing n for thumbprint");
                members["e"] = rsaE!;
                members["kty"] = "RSA";
                members["n"] = rsaN!;
                break;

            case "OKP":
                if (!TryGetString(jwk, "crv", out var okpCrv) || string.IsNullOrEmpty(okpCrv))
                    throw new DescopeException("OKP jwk missing crv for thumbprint");
                if (!TryGetString(jwk, "x", out var okpX) || string.IsNullOrEmpty(okpX))
                    throw new DescopeException("OKP jwk missing x for thumbprint");
                members["crv"] = okpCrv!;
                members["kty"] = "OKP";
                members["x"] = okpX!;
                break;

            default:
                throw new DescopeException($"unsupported kty for thumbprint: {kty}");
        }

        var json = JsonConvert.SerializeObject(members, Formatting.None);
        var jsonBytes = Encoding.UTF8.GetBytes(json);

#if NET6_0_OR_GREATER
        var digest = SHA256.HashData(jsonBytes);
#else
        byte[] digest;
        using (var sha = SHA256.Create())
        {
            digest = sha.ComputeHash(jsonBytes);
        }
#endif
        return Base64UrlEncode(digest);
    }

    // -----------------------------------------------------------------------
    // Base64Url helpers
    // -----------------------------------------------------------------------

    internal static byte[] Base64UrlDecode(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }

    internal static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    // -----------------------------------------------------------------------
    // Claim value extraction helpers
    // (handles both raw string and System.Text.Json.JsonElement values)
    // -----------------------------------------------------------------------

    private static bool TryGetString(Dictionary<string, object> dict, string key, out string? value)
    {
        value = null;
        if (!dict.TryGetValue(key, out var raw) || raw == null)
            return false;

        if (raw is string s)
        {
            value = s;
            return true;
        }

        if (raw is JsonElement el && el.ValueKind == JsonValueKind.String)
        {
            value = el.GetString();
            return true;
        }

        return false;
    }

    private static bool TryGetLong(Dictionary<string, object> dict, string key, out long value)
    {
        value = 0;
        if (!dict.TryGetValue(key, out var raw) || raw == null)
            return false;

        if (raw is long l) { value = l; return true; }
        if (raw is int i) { value = i; return true; }
        if (raw is double d) { value = (long)d; return true; }

        if (raw is JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out var v))
            {
                value = v;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetObject(Dictionary<string, object> dict, string key, out Dictionary<string, object>? value)
    {
        value = null;
        if (!dict.TryGetValue(key, out var raw) || raw == null)
            return false;

        if (raw is Dictionary<string, object> d)
        {
            value = d;
            return true;
        }

        if (raw is JsonElement el && el.ValueKind == JsonValueKind.Object)
        {
            try
            {
                value = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText());
                return value != null;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }
}
