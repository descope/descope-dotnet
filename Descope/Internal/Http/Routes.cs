namespace Descope.Internal
{
    public static class Routes
    {
        #region Auth

        #region General Auth

        public const string AuthKeys = "/v2/keys/";
        public const string AuthMe = "/v1/auth/me";
        public const string AuthRefresh = "/v1/auth/refresh";
        public const string AuthSelectTenant = "/v1/auth/tenant/select";
        public const string AuthLogOut = "/v1/auth/logout";
        public const string AuthLogOutAll = "/v1/auth/logoutall";
        public const string AuthAccessKeyExchange = "/v1/auth/accesskey/exchange";

        #endregion General Auth

        #region OTP

        public const string OtpSignUp = "/v1/auth/otp/signup/";
        public const string OtpSignIn = "/v1/auth/otp/signin/";
        public const string OtpSignUpOrIn = "/v1/auth/otp/signup-in/";
        public const string OtpVerify = "/v1/auth/otp/verify/";
        public const string OtpUpdateEmail = "/v1/auth/otp/update/email";
        public const string OtpUpdatePhone = "/v1/auth/otp/update/phone";

        #endregion OTP

        #endregion Auth

        #region Management

        #region Tenant

        public const string TenantCreate = "/v1/mgmt/tenant/create";
        public const string TenantUpdate = "/v1/mgmt/tenant/update";
        public const string TenantDelete = "/v1/mgmt/tenant/delete";
        public const string TenantLoad = "/v1/mgmt/tenant";
        public const string TenantLoadAll = "/v1/mgmt/tenant/all";
        public const string TenantSearch = "/v1/mgmt/tenant/search";

        #endregion Tenant

        #region User

        public const string UserCreate = "/v1/mgmt/user/create";
        public const string UserCreateBatch = "/v1/mgmt/user/create/batch";
        public const string UserLoad = "/v1/mgmt/user";
        public const string UserSearch = "/v1/mgmt/user/search";
        public const string UserUpdate = "/v1/mgmt/user/update";
        public const string UserUpdateStatus = "/v1/mgmt/user/update/status";
        public const string UserUpdateEmail = "/v1/mgmt/user/update/email";
        public const string UserUpdatePhone = "/v1/mgmt/user/update/phone";
        public const string UserUpdateName = "/v1/mgmt/user/update/name";
        public const string UserUpdatePicture = "/v1/mgmt/user/update/picture";
        public const string UserUpdateCustomAttribute = "/v1/mgmt/user/update/customAttribute";
        public const string UserUpdateLoginId = "/v1/mgmt/user/update/loginid";
        public const string UserPasswordSet = "/v1/mgmt/user/password/set";
        public const string UserPasswordExpire = "/v1/mgmt/user/password/expire";
        public const string UserPasskeyRemoveAll = "/v1/mgmt/user/passkeys/delete";
        public const string UserLogout = "/v1/mgmt/user/logout";
        public const string UserDelete = "/v1/mgmt/user/delete";
        public const string UserTenantAdd = "/v1/mgmt/user/update/tenant/add";
        public const string UserTenantRemove = "/v1/mgmt/user/update/tenant/remove";
        public const string UserRolesSet = "/v1/mgmt/user/update/role/set";
        public const string UserRolesAdd = "/v1/mgmt/user/update/role/add";
        public const string UserRoleRemove = "/v1/mgmt/user/update/role/remove";
        public const string UserSsoAppSet = "/v1/mgmt/user/update/ssoapp/set";
        public const string UserSsoAppAdd = "/v1/mgmt/user/update/ssoapp/add";
        public const string UserSsoAppRemove = "/v1/mgmt/user/update/ssoapp/remove";
        public const string UserProviderToken = "/v1/mgmt/user/provider/token";
        public const string UserDeleteAllTestUsers = "/v1/mgmt/user/test/delete/all";
        public const string UserTestsGenerateOtp = "/v1/mgmt/tests/generate/otp";
        public const string UserTestsGenerateMagicLink = "/v1/mgmt/tests/generate/magiclink";
        public const string UserTestsGenerateEnchantedLink = "/v1/mgmt/tests/generate/enchantedlink";
        public const string UserTestsGenerateEmbeddedLink = "/v1/mgmt/user/signin/embeddedlink";

        #endregion User

        #region JWT

        public const string JwtUpdate = "/v1/mgmt/jwt/update";

        #endregion JWT

        #region AccessKey

        public const string AccessKeyCreate = "/v1/mgmt/accesskey/create";
        public const string AccessKeyLoad = "/v1/mgmt/accesskey";
        public const string AccessKeySearch = "/v1/mgmt/accesskey/search";
        public const string AccessKeyUpdate = "/v1/mgmt/accesskey/update";
        public const string AccessKeyActivate = "v1/mgmt/accesskey/activate";
        public const string AccessKeyDeactivate = "/v1/mgmt/accesskey/deactivate";
        public const string AccessKeyDelete = "/v1/mgmt/accesskey/delete";

        #endregion AccessKey

        #region Permission

        public const string PermissionCreate = "/v1/mgmt/permission/create";
        public const string PermissionUpdate = "/v1/mgmt/permission/update";
        public const string PermissionDelete = "/v1/mgmt/permission/delete";
        public const string PermissionLoadAll = "/v1/mgmt/permission/all";

        #endregion Permission

        #region Role

        public const string RoleCreate = "/v1/mgmt/role/create";
        public const string RoleUpdate = "/v1/mgmt/role/update";
        public const string RoleDelete = "/v1/mgmt/role/delete";
        public const string RoleLoadAll = "/v1/mgmt/role/all";
        public const string RoleSearchAll = "/v1/mgmt/role/search";

        #endregion Role

        #region Project

        public const string ProjectExport = "/v1/mgmt/project/export";
        public const string ProjectImport = "/v1/mgmt/project/import";
        public const string ProjectClone = "/v1/mgmt/project/clone";
        public const string ProjectRename = "/v1/mgmt/project/update/name";
        public const string ProjectDelete = "/v1/mgmt/project/delete";

        #endregion Project


        #endregion Management
    }
}
