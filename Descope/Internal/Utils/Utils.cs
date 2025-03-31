namespace Descope.Internal
{
    internal static class Utils
    {
        internal static void EnforceRequiredArgs(params (string, object?)[] args)
        {
            foreach (var arg in args)
            {
                if (arg.Item2 == null || (arg.Item2 is string s && string.IsNullOrEmpty(s)))
                {
                    throw new DescopeException($"The {arg.Item1} argument is required");
                }
            }
        }

        // Extensions

        internal static string? GetRefreshJwt(this LoginOptions options)
        {
            return options.StepupRefreshJwt ?? options.MfaRefreshJwt;
        }

        internal static Dictionary<string, object?> ToDictionary(this LoginOptions options)
        {
            return new Dictionary<string, object?>{
                {"stepup", options.StepupRefreshJwt != null ? true : null},
                {"mfa", options.MfaRefreshJwt != null ? true : null},
                {"customClaims", options.CustomClaims},
            };
        }

        internal static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email.Trim());
                return addr.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }
    }
}
