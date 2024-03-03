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

        public async Task<DescopeUser> Create(string loginId, UserRequest? request, InviteOptions? options, bool testUser)
        {
            request ??= new UserRequest();
            var body = MakeCreateUserRequestBody(loginId, request, options, testUser);
            var result = await _httpClient.Post<UserResponse>(Routes.UserCreate, _managementKey, body);
            return result.User;
        }

        public async Task<BatchCreateUserResponse> CreateBatch(List<BatchUser> batchUsers, InviteOptions? options)
        {
            batchUsers ??= new List<BatchUser>();
            var body = MakeCreateBatchUsersRequestBody(batchUsers, options);
            return await _httpClient.Post<BatchCreateUserResponse>(Routes.UserCreateBatch, _managementKey, body);
        }

        public async Task<DescopeUser> Update(string loginId, UserRequest? request)
        {
            request ??= new UserRequest();
            var body = MakeUpdateUserRequestBody(loginId, request);
            var result = await _httpClient.Post<UserResponse>(Routes.UserUpdate, _managementKey, body);
            return result.User;
        }

        public async Task<DescopeUser> Activate(string loginId)
        {
            var result = await updateStatus(loginId, "enabled");
            return result;
        }

        public async Task<DescopeUser> Deactivate(string loginId)
        {
            var result = await updateStatus(loginId, "disabled");
            return result;
        }

        private async Task<DescopeUser> updateStatus(string loginId, string status)
        {
            var body = new { loginId, status };
            var result = await _httpClient.Post<UserResponse>(Routes.UserUpdateStatus, _managementKey, body);
            return result.User;
        }

        public async Task<DescopeUser> UpdateLoginId(string loginId, string? newLoginId)
        {
            var body = new { loginId, newLoginId };
            var result = await _httpClient.Post<UserResponse>(Routes.UserUpdateLoginId, _managementKey, body);
            return result.User;
        }

        public async Task<DescopeUser> UpdateEmail(string loginId, string? email, bool verified)
        {
            var body = new { loginId, email, verified };
            var result = await _httpClient.Post<UserResponse>(Routes.UserUpdateEmail, _managementKey, body);
            return result.User;
        }

        public async Task<DescopeUser> UpdatePhone(string loginId, string? phone, bool verified)
        {
            var body = new { loginId, phone, verified };
            var result = await _httpClient.Post<UserResponse>(Routes.UserUpdatePhone, _managementKey, body);
            return result.User;
        }

        public async Task<DescopeUser> UpdateDisplayName(string loginId, string? displayName)
        {
            var body = new { loginId, displayName };
            var result = await _httpClient.Post<UserResponse>(Routes.UserUpdateName, _managementKey, body);
            return result.User;
        }

        public async Task<DescopeUser> UpdateUserNames(string loginId, string? givenName, string? middleName, string? familyName)
        {
            var body = new { loginId, givenName, middleName, familyName };
            var result = await _httpClient.Post<UserResponse>(Routes.UserUpdateName, _managementKey, body);
            return result.User;
        }

        public async Task<DescopeUser> UpdatePicture(string loginId, string? picture)
        {
            var body = new { loginId, picture };
            var result = await _httpClient.Post<UserResponse>(Routes.UserUpdatePicture, _managementKey, body);
            return result.User;
        }

        public async Task<DescopeUser> UpdateCustomAttributes(string loginId, string attributeKey, object attributeValue)
        {
            var body = new { loginId, attributeKey, attributeValue };
            var result = await _httpClient.Post<UserResponse>(Routes.UserUpdateCustomAttribute, _managementKey, body);
            return result.User;
        }

        public async Task<DescopeUser> SetRoles(string loginId, List<string> roleNames, string? tenantId)
        {
            var body = new { loginId, roleNames, tenantId };
            var result = await _httpClient.Post<UserResponse>(Routes.UserRolesSet, _managementKey, body);
            return result.User;
        }

        public async Task<DescopeUser> AddRoles(string loginId, List<string> roleNames, string? tenantId)
        {
            var body = new { loginId, roleNames, tenantId };
            var result = await _httpClient.Post<UserResponse>(Routes.UserRolesAdd, _managementKey, body);
            return result.User;
        }

        public async Task<DescopeUser> RemoveRoles(string loginId, List<string> roleNames, string? tenantId)
        {
            var body = new { loginId, roleNames, tenantId };
            var result = await _httpClient.Post<UserResponse>(Routes.UserRoleRemove, _managementKey, body);
            return result.User;
        }

        public async Task<DescopeUser> SetSsoApps(string loginId, List<string> ssoAppIds)
        {
            var body = new { loginId, ssoAppIds };
            var result = await _httpClient.Post<UserResponse>(Routes.UserSsoAppSet, _managementKey, body);
            return result.User;
        }

        public async Task<DescopeUser> AddSsoApps(string loginId, List<string> ssoAppIds)
        {
            var body = new { loginId, ssoAppIds };
            var result = await _httpClient.Post<UserResponse>(Routes.UserSsoAppAdd, _managementKey, body);
            return result.User;
        }

        public async Task<DescopeUser> RemoveSsoApps(string loginId, List<string> ssoAppIds)
        {
            var body = new { loginId, ssoAppIds };
            var result = await _httpClient.Post<UserResponse>(Routes.UserSsoAppRemove, _managementKey, body);
            return result.User;
        }

        public async Task<DescopeUser> AddTenant(string loginId, string tenantId)
        {
            var body = new { loginId, tenantId };
            var result = await _httpClient.Post<UserResponse>(Routes.UserTenantAdd, _managementKey, body);
            return result.User;
        }

        public async Task<DescopeUser> RemoveTenant(string loginId, string tenantId)
        {
            var body = new { loginId, tenantId };
            var result = await _httpClient.Post<UserResponse>(Routes.UserTenantRemove, _managementKey, body);
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

        public async Task<DescopeUser> Load(string loginId)
        {
            var result = await _httpClient.Get<UserResponse>(Routes.UserLoad + $"?loginId={loginId}", _managementKey);
            return result.User;
        }

        public async Task<List<DescopeUser>> SearchAll(SearchUserOptions? options)
        {
            options ??= new SearchUserOptions();
            var result = await _httpClient.Post<UsersResponse>(Routes.UserSearch, _managementKey, options);
            return result.Users;
        }

        public async Task<UserTestOTPResponse> GenerateOtpForTestUser(DeliveryMethod deliveryMethod, string loginId, LoginOptions? loginOptions)
        {
            if (string.IsNullOrEmpty(loginId)) throw new DescopeException("loginId missing");
            var body = new { loginId, deliveryMethod = deliveryMethod.ToString(), loginOptions };
            return await _httpClient.Post<UserTestOTPResponse>(Routes.UserTestsGenerateOtp, _managementKey, body);
        }

        public async Task<UserTestMagicLinkResponse> GenerateMagicLinkForTestUser(DeliveryMethod deliveryMethod, string loginId, string? redirectUrl, LoginOptions? loginOptions)
        {
            if (string.IsNullOrEmpty(loginId)) throw new DescopeException("loginId missing");
            var body = new { loginId, deliveryMethod = deliveryMethod.ToString(), redirectUrl, loginOptions };
            return await _httpClient.Post<UserTestMagicLinkResponse>(Routes.UserTestsGenerateMagicLink, _managementKey, body);
        }

        public async Task<UserTestEnchantedLinkResponse> GenerateEnchantedLinkForTestUser(string loginId, string? redirectUrl, LoginOptions? loginOptions)
        {
            if (string.IsNullOrEmpty(loginId)) throw new DescopeException("loginId missing");
            var request = new { loginId, redirectUrl, loginOptions };
            return await _httpClient.Post<UserTestEnchantedLinkResponse>(Routes.UserTestsGenerateEnchantedLink, _managementKey, request);
        }

        public async Task<string> GenerateEmbeddedLink(string loginId, Dictionary<string, object>? customClaims)
        {
            if (string.IsNullOrEmpty(loginId)) throw new DescopeException("loginId missing");
            customClaims ??= new Dictionary<string, object>();
            var body = new { loginId, customClaims };
            var result = await _httpClient.Post<GenerateEmbeddedLinkResponse>(Routes.UserTestsGenerateEmbeddedLink, _managementKey, body);
            return result.Token;
        }

        #endregion IUser Implementation

        #region Internal

        private static Dictionary<string, object> MakeCreateUserRequestBody(string loginId, UserRequest request, InviteOptions? options, bool test)
        {
            var body = MakeUpdateUserRequestBody(loginId, request);
            body["test"] = test;
            if (options != null)
            {
                body["invite"] = true;
                body["inviteUrl"] = options.inviteUrl;
                body["sendMail"] = options.sendMail;
                body["sendSMS"] = options.sendSms;
            }
            return body;
        }

        private static Dictionary<string, object> MakeCreateBatchUsersRequestBody(List<BatchUser> users, InviteOptions? options)
        {
            var body = new Dictionary<string, object>();
            var userList = new List<Dictionary<string, object>>();
            foreach (var user in users)
            {
                var dict = MakeUpdateUserRequestBody(user.loginId, user);
                if (!string.IsNullOrEmpty(user.password?.cleartext))
                {
                    dict["password"] = user.password.cleartext;
                }
                if (user.password?.hashed != null)
                {
                    dict["hashedPassword"] = user.password.hashed;
                }
                userList.Add(dict);
            }
            body["users"] = userList;
            if (options != null)
            {
                body["invite"] = true;
                body["inviteUrl"] = options.inviteUrl;
                body["sendMail"] = options.sendMail;
                body["sendSMS"] = options.sendSms;
            }
            return body;
        }

        private static Dictionary<string, object> MakeUpdateUserRequestBody(string loginId, UserRequest request)
        {
            return new Dictionary<string, object>
            {
                {"loginId", loginId},
                {"email", request.email},
                {"phone", request.phone},
                {"displayName", request.name},
                {"givenName", request.givenName},
                {"middleName", request.middleName},
                {"familyName", request.familyName},
                {"roleNames", request.roleNames},
                {"userTenants", MakeAssociatedTenantList(request.userTenants)},
                {"customAttributes", request.customAttributes},
                {"picture", request.picture},
                {"additionalLoginIds", request.additionalLoginIds},
                {"verifiedEmail", request.verifiedEmail},
                {"verifiedPhone", request.verifiedPhone},
                {"ssoAppIDs", request.ssoAppIds}
            };
        }

        private static List<Dictionary<string, object>> MakeAssociatedTenantList(List<AssociatedTenant> tenants)
        {
            tenants ??= new List<AssociatedTenant>();
            var dict = new List<Dictionary<string, object>>();
            foreach (var tenant in tenants)
            {
                dict.Add(new Dictionary<string, object>
                {
                    {"tenantId", tenant.tenantId},
                    {"roleNames", tenant.roleNames},
                });
            };
            return dict;
        }

        #endregion Internal
    }

    internal class UserResponse
    {
        [JsonPropertyName("user")]
        public DescopeUser User { get; set; }

        public UserResponse(DescopeUser user)
        {
            User = user;
        }
    }

    internal class UsersResponse
    {
        [JsonPropertyName("users")]
        public List<DescopeUser> Users { get; set; }

        public UsersResponse(List<DescopeUser> users)
        {
            Users = users;
        }
    }

    internal class GenerateEmbeddedLinkResponse
    {
        [JsonPropertyName("token")]
        internal string Token { get; set; }

        internal GenerateEmbeddedLinkResponse(string token)
        {
            Token = token;
        }
    }
}
