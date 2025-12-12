using System.Text.Json;
using System.Text.Json.Serialization;

namespace Descope.Internal.Management
{
    internal class User : IUser
    {
        private readonly IHttpClient _httpClient;
        private readonly string _managementKey;

        internal User(IHttpClient httpClient, string managementKey)
        {
            _httpClient = httpClient;
            _managementKey = managementKey;
        }

        #region IUser Implementation

        public async Task<UserResponse> Create(string loginId, UserRequest? request, bool sendInvite, InviteOptions? inviteOptions, bool testUser)
        {
            request ??= new UserRequest();
            var body = MakeCreateUserRequestBody(loginId, request, sendInvite, inviteOptions, testUser);
            var result = await _httpClient.Post<WrappedUserResponse>(Routes.UserCreate, _managementKey, body);
            return result.User;
        }

        public async Task<BatchCreateUserResponse> CreateBatch(List<BatchUser> batchUsers, bool sendInvite, InviteOptions? inviteOptions)
        {
            batchUsers ??= new List<BatchUser>();
            var body = MakeCreateBatchUsersRequestBody(batchUsers, sendInvite, inviteOptions);
            return await _httpClient.Post<BatchCreateUserResponse>(Routes.UserCreateBatch, _managementKey, body);
        }

        public async Task<UserResponse> Update(string loginId, UserRequest? request)
        {
            request ??= new UserRequest();
            var body = MakeUpdateUserRequestBody(loginId, request);
            var result = await _httpClient.Post<WrappedUserResponse>(Routes.UserUpdate, _managementKey, body);
            return result.User;
        }

        public async Task<UserResponse> Patch(string loginId, UserRequest? request)
        {
            request ??= new UserRequest();
            var body = MakePatchUserRequestBody(loginId, request);
            var result = await _httpClient.Patch<WrappedUserResponse>(Routes.UserPatch, _managementKey, body);
            return result.User;
        }

        public async Task<UserResponse> Activate(string loginId)
        {
            var result = await updateStatus(loginId, "enabled");
            return result;
        }

        public async Task<UserResponse> Deactivate(string loginId)
        {
            var result = await updateStatus(loginId, "disabled");
            return result;
        }

        private async Task<UserResponse> updateStatus(string loginId, string status)
        {
            var body = new { loginId, status };
            var result = await _httpClient.Post<WrappedUserResponse>(Routes.UserUpdateStatus, _managementKey, body);
            return result.User;
        }

        public async Task<UserResponse> UpdateLoginId(string loginId, string? newLoginId)
        {
            var body = new { loginId, newLoginId };
            var result = await _httpClient.Post<WrappedUserResponse>(Routes.UserUpdateLoginId, _managementKey, body);
            return result.User;
        }

        public async Task<UserResponse> UpdateEmail(string loginId, string? email, bool verified)
        {
            var body = new { loginId, email, verified };
            var result = await _httpClient.Post<WrappedUserResponse>(Routes.UserUpdateEmail, _managementKey, body);
            return result.User;
        }

        public async Task<UserResponse> UpdatePhone(string loginId, string? phone, bool verified)
        {
            var body = new { loginId, phone, verified };
            var result = await _httpClient.Post<WrappedUserResponse>(Routes.UserUpdatePhone, _managementKey, body);
            return result.User;
        }

        public async Task<UserResponse> UpdateDisplayName(string loginId, string? displayName)
        {
            var body = new { loginId, displayName };
            var result = await _httpClient.Post<WrappedUserResponse>(Routes.UserUpdateName, _managementKey, body);
            return result.User;
        }

        public async Task<UserResponse> UpdateUserNames(string loginId, string? givenName, string? middleName, string? familyName)
        {
            var body = new { loginId, givenName, middleName, familyName };
            var result = await _httpClient.Post<WrappedUserResponse>(Routes.UserUpdateName, _managementKey, body);
            return result.User;
        }

        public async Task<UserResponse> UpdatePicture(string loginId, string? picture)
        {
            var body = new { loginId, picture };
            var result = await _httpClient.Post<WrappedUserResponse>(Routes.UserUpdatePicture, _managementKey, body);
            return result.User;
        }

        public async Task<UserResponse> UpdateCustomAttributes(string loginId, string attributeKey, object attributeValue)
        {
            var body = new { loginId, attributeKey, attributeValue };
            var result = await _httpClient.Post<WrappedUserResponse>(Routes.UserUpdateCustomAttribute, _managementKey, body);
            return result.User;
        }

        public async Task<UserResponse> SetRoles(string loginId, List<string> roleNames, string? tenantId)
        {
            var body = new { loginId, roleNames, tenantId };
            var result = await _httpClient.Post<WrappedUserResponse>(Routes.UserRolesSet, _managementKey, body);
            return result.User;
        }

        public async Task<UserResponse> AddRoles(string loginId, List<string> roleNames, string? tenantId)
        {
            var body = new { loginId, roleNames, tenantId };
            var result = await _httpClient.Post<WrappedUserResponse>(Routes.UserRolesAdd, _managementKey, body);
            return result.User;
        }

        public async Task<UserResponse> RemoveRoles(string loginId, List<string> roleNames, string? tenantId)
        {
            var body = new { loginId, roleNames, tenantId };
            var result = await _httpClient.Post<WrappedUserResponse>(Routes.UserRoleRemove, _managementKey, body);
            return result.User;
        }

        public async Task<UserResponse> SetSsoApps(string loginId, List<string> ssoAppIds)
        {
            var body = new { loginId, ssoAppIds };
            var result = await _httpClient.Post<WrappedUserResponse>(Routes.UserSsoAppSet, _managementKey, body);
            return result.User;
        }

        public async Task<UserResponse> AddSsoApps(string loginId, List<string> ssoAppIds)
        {
            var body = new { loginId, ssoAppIds };
            var result = await _httpClient.Post<WrappedUserResponse>(Routes.UserSsoAppAdd, _managementKey, body);
            return result.User;
        }

        public async Task<UserResponse> RemoveSsoApps(string loginId, List<string> ssoAppIds)
        {
            var body = new { loginId, ssoAppIds };
            var result = await _httpClient.Post<WrappedUserResponse>(Routes.UserSsoAppRemove, _managementKey, body);
            return result.User;
        }

        public async Task<UserResponse> AddTenant(string loginId, string tenantId)
        {
            var body = new { loginId, tenantId };
            var result = await _httpClient.Post<WrappedUserResponse>(Routes.UserTenantAdd, _managementKey, body);
            return result.User;
        }

        public async Task<UserResponse> RemoveTenant(string loginId, string tenantId)
        {
            var body = new { loginId, tenantId };
            var result = await _httpClient.Post<WrappedUserResponse>(Routes.UserTenantRemove, _managementKey, body);
            return result.User;
        }

        public async Task SetTemporaryPassword(string loginId, string password)
        {
            await SetPassword(loginId, password, false);
        }

        public async Task SetActivePassword(string loginId, string password)
        {
            await SetPassword(loginId, password, true);
        }

        private async Task SetPassword(string loginId, string password, bool setActive)
        {
            var body = new { loginId, password, setActive };
            await _httpClient.Post<object>(Routes.UserPasswordSet, _managementKey, body);
        }

        public async Task ExpirePassword(string loginId)
        {
            var body = new { loginId };
            await _httpClient.Post<object>(Routes.UserPasswordExpire, _managementKey, body);
        }

        public async Task RemoveAllPasskeys(string loginId)
        {
            var body = new { loginId };
            await _httpClient.Post<object>(Routes.UserPasskeyRemoveAll, _managementKey, body);
        }

        public async Task<ProviderTokenResponse> GetProviderToken(string loginId, string provider)
        {
            var result = await _httpClient.Get<ProviderTokenResponse>(Routes.UserProviderToken + $"?loginId={loginId}&provider={provider}", _managementKey);
            return result;
        }

        public async Task Logout(string? loginId, string? userId)
        {
            if (string.IsNullOrEmpty(loginId) && string.IsNullOrEmpty(userId)) throw new DescopeException("User loginId or userId are required to log out");
            var body = new { loginId, userId };
            await _httpClient.Post<object>(Routes.UserLogout, _managementKey, body);
        }

        public async Task Delete(string loginId)
        {
            var body = new { loginId };
            await _httpClient.Post<object>(Routes.UserDelete, _managementKey, body);
        }

        public async Task DeleteAllTestUsers()
        {
            await _httpClient.Delete<object>(Routes.UserDeleteAllTestUsers, _managementKey);
        }

        public async Task<UserResponse> Load(string loginId)
        {
            var result = await _httpClient.Get<WrappedUserResponse>(Routes.UserLoad + $"?loginId={loginId}", _managementKey);
            return result.User;
        }

        public async Task<List<UserResponse>> SearchAll(SearchUserOptions? options)
        {
            options ??= new SearchUserOptions();

            var json = JsonSerializer.Serialize(options);
            var body = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();

            if (options.TenantRoleIds != null && options.TenantRoleIds.Count > 0)
            {
                body["tenantRoleIds"] = MapToValuesObject(options.TenantRoleIds);
            }
            if (options.TenantRoleNames != null && options.TenantRoleNames.Count > 0)
            {
                body["tenantRoleNames"] = MapToValuesObject(options.TenantRoleNames);
            }

            var result = await _httpClient.Post<WrappedUsersResponse>(Routes.UserSearch, _managementKey, body);
            return result.Users;
        }

        public async Task<UserTestOTPResponse> GenerateOtpForTestUser(DeliveryMethod deliveryMethod, string loginId, LoginOptions? loginOptions)
        {
            if (string.IsNullOrEmpty(loginId)) throw new DescopeException("loginId missing");
            var body = new { loginId, deliveryMethod = deliveryMethod.ToString().ToLower(), loginOptions = loginOptions?.ToDictionary() };
            return await _httpClient.Post<UserTestOTPResponse>(Routes.UserTestsGenerateOtp, _managementKey, body);
        }

        public async Task<UserTestMagicLinkResponse> GenerateMagicLinkForTestUser(DeliveryMethod deliveryMethod, string loginId, string? redirectUrl, LoginOptions? loginOptions)
        {
            if (string.IsNullOrEmpty(loginId)) throw new DescopeException("loginId missing");
            var body = new { loginId, deliveryMethod = deliveryMethod.ToString().ToLower(), redirectUrl, loginOptions = loginOptions?.ToDictionary() };
            return await _httpClient.Post<UserTestMagicLinkResponse>(Routes.UserTestsGenerateMagicLink, _managementKey, body);
        }

        public async Task<UserTestEnchantedLinkResponse> GenerateEnchantedLinkForTestUser(string loginId, string? redirectUrl, LoginOptions? loginOptions)
        {
            if (string.IsNullOrEmpty(loginId)) throw new DescopeException("loginId missing");
            var request = new { loginId, redirectUrl, loginOptions = loginOptions?.ToDictionary() };
            return await _httpClient.Post<UserTestEnchantedLinkResponse>(Routes.UserTestsGenerateEnchantedLink, _managementKey, request);
        }

        public async Task<string> GenerateEmbeddedLink(string loginId, Dictionary<string, object>? customClaims, int? timeout)
        {
            if (string.IsNullOrEmpty(loginId)) throw new DescopeException("loginId missing");
            customClaims ??= new Dictionary<string, object>();

            var body = new Dictionary<string, object>
            {
                { "loginId", loginId },
                { "customClaims", customClaims }
            };

            // Only add timeout field if it's not null and greater than 0
            if (timeout.HasValue && timeout.Value > 0)
            {
                body["timeout"] = timeout.Value;
            }

            var result = await _httpClient.Post<GenerateEmbeddedLinkResponse>(Routes.UserTestsGenerateEmbeddedLink, _managementKey, body);
            return result.Token;
        }

        #endregion IUser Implementation

        #region Internal

        private static Dictionary<string, object> MakeCreateUserRequestBody(string loginId, UserRequest request, bool sendInvite, InviteOptions? options, bool test)
        {
            var body = MakeUpdateUserRequestBody(loginId, request);
            body["test"] = test;
            body["invite"] = sendInvite;
            if (options != null)
            {
                if (!string.IsNullOrEmpty(options.InviteUrl)) body["inviteUrl"] = options.InviteUrl;
                body["sendMail"] = options.SendMail;
                body["sendSMS"] = options.SendSms;
            }
            return body;
        }

        private static Dictionary<string, object> MakeCreateBatchUsersRequestBody(List<BatchUser> users, bool sendInvite, InviteOptions? options)
        {
            var body = new Dictionary<string, object>();
            var userList = new List<Dictionary<string, object>>();
            foreach (var user in users)
            {
                var dict = MakeUpdateUserRequestBody(user.LoginId, user);
                if (!string.IsNullOrEmpty(user.Password?.Cleartext))
                {
                    dict["password"] = user.Password.Cleartext;
                }
                if (user.Password?.Hashed != null)
                {
                    dict["hashedPassword"] = user.Password.Hashed;
                }
                if (user.Status.HasValue)
                {
                    dict["status"] = user.Status.Value.ToStringValue();
                }
                userList.Add(dict);
            }
            body["users"] = userList;
            body["invite"] = sendInvite;
            if (options != null)
            {
                if (!string.IsNullOrEmpty(options.InviteUrl)) body["inviteUrl"] = options.InviteUrl;
                body["sendMail"] = options.SendMail;
                body["sendSMS"] = options.SendSms;
            }
            return body;
        }

        private static Dictionary<string, object> MakeUpdateUserRequestBody(string loginId, UserRequest request)
        {
            var body = new Dictionary<string, object>
            {
                {"loginId", loginId},
                {"verifiedEmail", request.VerifiedEmail},
                {"verifiedPhone", request.VerifiedPhone},
            };
            if (!string.IsNullOrEmpty(request.Email)) body["email"] = request.Email;
            if (!string.IsNullOrEmpty(request.Phone)) body["phone"] = request.Phone;
            if (!string.IsNullOrEmpty(request.Name)) body["displayName"] = request.Name;
            if (!string.IsNullOrEmpty(request.GivenName)) body["givenName"] = request.GivenName;
            if (!string.IsNullOrEmpty(request.MiddleName)) body["middleName"] = request.MiddleName;
            if (!string.IsNullOrEmpty(request.FamilyName)) body["familyName"] = request.FamilyName;
            if (request.RoleNames != null) body["roleNames"] = request.RoleNames;
            if (request.UserTenants != null) body["userTenants"] = MakeAssociatedTenantList(request.UserTenants);
            if (request.CustomAttributes != null) body["customAttributes"] = request.CustomAttributes;
            if (!string.IsNullOrEmpty(request.Picture)) body["picture"] = request.Picture;
            if (request.AdditionalLoginIds != null) body["additionalLoginIds"] = request.AdditionalLoginIds;
            if (request.SsoAppIds != null) body["ssoAppIDs"] = request.SsoAppIds;
            return body;
        }

        private static Dictionary<string, object> MakePatchUserRequestBody(string loginId, UserRequest request)
        {
            var body = new Dictionary<string, object>
            {
                {"loginId", loginId}
            };
            // For Patch: include field if it's not null (even if empty string), skip if null
            if (request.Email != null) body["email"] = request.Email;
            if (request.Phone != null) body["phone"] = request.Phone;
            if (request.Name != null) body["displayName"] = request.Name;
            if (request.GivenName != null) body["givenName"] = request.GivenName;
            if (request.MiddleName != null) body["middleName"] = request.MiddleName;
            if (request.FamilyName != null) body["familyName"] = request.FamilyName;
            if (request.Picture != null) body["picture"] = request.Picture;
            if (request.RoleNames != null) body["roleNames"] = request.RoleNames;
            if (request.UserTenants != null) body["userTenants"] = MakeAssociatedTenantList(request.UserTenants);
            if (request.CustomAttributes != null) body["customAttributes"] = request.CustomAttributes;
            if (request.AdditionalLoginIds != null) body["additionalLoginIds"] = request.AdditionalLoginIds;
            if (request.SsoAppIds != null) body["ssoAppIDs"] = request.SsoAppIds;
            return body;
        }

        private static List<Dictionary<string, object>> MakeAssociatedTenantList(List<AssociatedTenant> tenants)
        {
            tenants ??= new List<AssociatedTenant>();
            var list = new List<Dictionary<string, object>>();
            foreach (var tenant in tenants)
            {
                var dict = new Dictionary<string, object> { { "tenantId", tenant.TenantId } };
                if (tenant.RoleNames != null) dict["roleNames"] = tenant.RoleNames;
                list.Add(dict);
            }
            return list;
        }

        internal static Dictionary<string, object> MapToValuesObject(Dictionary<string, List<string>> inputMap)
        {
            var result = new Dictionary<string, object>();
            if (inputMap != null)
            {
                foreach (var kvp in inputMap)
                {
                    result[kvp.Key] = new Dictionary<string, object> { { "values", kvp.Value } };
                }
            }
            return result;
        }

        #endregion Internal
    }

    internal class WrappedUserResponse
    {
        [JsonPropertyName("user")]
        public UserResponse User { get; set; }

        public WrappedUserResponse(UserResponse user)
        {
            User = user;
        }
    }

    internal class WrappedUsersResponse
    {
        [JsonPropertyName("users")]
        public List<UserResponse> Users { get; set; }

        public WrappedUsersResponse(List<UserResponse> users)
        {
            Users = users;
        }
    }

    internal class GenerateEmbeddedLinkResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }
}
