using System.Text;
using System.Text.Json;

namespace Descope.Internal;

/// <summary>
/// Fixes OpenAPI inconsistency for endpoints which use the protobuf "response_body" option, which causes the response to return "flat" fields in the root object instead of nested under a specific field.
/// Affected endpoints:
/// Management Service:
/// - GET /v1/mgmt/sso/idp/app/load (app)
/// - GET /v1/mgmt/tenant (tenant)
/// - POST /v1/mgmt/user/history (usersAuthHistory)
/// - GET /v1/mgmt/thirdparty/app/load (app)
/// - POST /v1/mgmt/group/all (groups)
/// - POST /v1/mgmt/group/members (groups)
/// - POST /v1/mgmt/group/member/all (groups)
/// - GET /scim/v2/ResourceTypes (values)
/// - GET /scim/v2/Users/{userID} (user)
/// - POST /scim/v2/Users (user)
/// - PUT /scim/v2/Users/{userID} (user)
/// - PATCH /scim/v2/Users/{userID} (user)
/// OneTime Service:
/// - GET /v1/auth/me/history (authHistory)
/// - POST /v1/auth/validate (parsedJWT)
/// - GET /oauth2/v1/apps/userinfo (userInfoClaims)
/// - GET /oauth2/v1/apps/{projectId}/userinfo (userInfoClaims)
/// - POST /oauth2/v1/apps/userinfo (userInfoClaims)
/// - POST /oauth2/v1/apps/{projectId}/userinfo (userInfoClaims)
/// - GET /oauth2/v1/userinfo (userInfoClaims)
/// - GET /{ssoAppId}/oauth2/v1/userinfo (userInfoClaims)
/// - POST /oauth2/v1/userinfo (userInfoClaims)
/// - POST /{ssoAppId}/oauth2/v1/userinfo (userInfoClaims)
/// </summary>
internal class FixRootResponseBodyHandler : DelegatingHandler
{
    // Management Service endpoints
    private static readonly string LoadSsoAppEndpoint = "/v1/mgmt/sso/idp/app/load";
    private static readonly string LoadTenantEndpoint = "/v1/mgmt/tenant";
    private static readonly string UsersAuthHistoryEndpoint = "/v1/mgmt/user/history";
    private static readonly string LoadThirdPartyAppEndpoint = "/v1/mgmt/thirdparty/app/load";
    private static readonly string LoadGroupsEndpoint = "/v1/mgmt/group/all";
    private static readonly string LoadGroupMembersEndpoint = "/v1/mgmt/group/members";
    private static readonly string LoadMemberGroupsEndpoint = "/v1/mgmt/group/member/all";
    private static readonly string ScimResourceTypesEndpoint = "/scim/v2/ResourceTypes";
    private static readonly string ScimUsersEndpoint = "/scim/v2/Users";

    // OneTime Service endpoints
    private static readonly string MeAuthHistoryEndpoint = "/v1/auth/me/history";
    private static readonly string ThirdPartyAppUserInfoEndpoint = "/oauth2/v1/apps/userinfo";
    private static readonly string OidcUserInfoEndpoint = "/oauth2/v1/userinfo";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return response;
        }

        var requestPath = request.RequestUri?.AbsolutePath ?? "";

        // Management Service endpoints
        if (requestPath.EndsWith(LoadSsoAppEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            response = await WrapResponseInField(response, "app", "id", cancellationToken);
        }
        else if (requestPath.EndsWith(LoadTenantEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            response = await WrapResponseInField(response, "tenant", "id", cancellationToken);
        }
        else if (requestPath.EndsWith(UsersAuthHistoryEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            response = await WrapResponseInField(response, "usersAuthHistory", "userId", cancellationToken);
        }
        else if (requestPath.EndsWith(LoadThirdPartyAppEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            response = await WrapResponseInField(response, "app", "id", cancellationToken);
        }
        else if (requestPath.EndsWith(LoadGroupsEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            response = await WrapResponseInField(response, "groups", "id", cancellationToken);
        }
        else if (requestPath.EndsWith(LoadGroupMembersEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            response = await WrapResponseInField(response, "groups", "id", cancellationToken);
        }
        else if (requestPath.EndsWith(LoadMemberGroupsEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            response = await WrapResponseInField(response, "groups", "id", cancellationToken);
        }
        else if (requestPath.EndsWith(ScimResourceTypesEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            response = await WrapResponseInField(response, "values", "id", cancellationToken);
        }
        else if (requestPath.IndexOf(ScimUsersEndpoint, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Handles GET /scim/v2/Users/{userID}, POST /scim/v2/Users, PUT /scim/v2/Users/{userID}, PATCH /scim/v2/Users/{userID}
            response = await WrapResponseInField(response, "user", "id", cancellationToken);
        }
        // OneTime Service endpoints
        else if (requestPath.EndsWith(MeAuthHistoryEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            response = await WrapResponseInField(response, "authHistory", "city", cancellationToken);
        }
        else if (requestPath.IndexOf(ThirdPartyAppUserInfoEndpoint, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 requestPath.IndexOf(OidcUserInfoEndpoint, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Handles all userinfo endpoints that use userInfoClaims
            response = await WrapResponseInField(response, "userInfoClaims", "sub", cancellationToken);
        }

        return response;
    }

    private async Task<HttpResponseMessage> WrapResponseInField(
        HttpResponseMessage response,
        string wrapperFieldName,
        string detectionFieldName,
        CancellationToken cancellationToken)
    {
#if NETSTANDARD2_0
        var content = await response.Content.ReadAsStringAsync();
#else
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
#endif

        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                // Parse the response to check if it needs fixing
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                // If response has the detection field at root level (not wrapped), fix it
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(detectionFieldName, out _))
                {
                    // Check if wrapper field already exists
                    if (!root.TryGetProperty(wrapperFieldName, out _))
                    {
                        // Wrap the entire response in the wrapper field
                        var wrappedResponse = JsonSerializer.Serialize(new Dictionary<string, JsonElement>
                        {
                            [wrapperFieldName] = root
                        });

                        response.Content = new StringContent(wrappedResponse, Encoding.UTF8, "application/json");
                    }
                }
            }
            catch (JsonException)
            {
                // If JSON parsing fails, return original response
                // Don't throw - let the normal error handling take over
            }
        }

        return response;
    }
}
