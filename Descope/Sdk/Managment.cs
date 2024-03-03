namespace Descope
{

    /// <summary>
    /// Provides functions for managing tenants in a project.
    /// </summary>
    public interface ITenant
    {
        /// <summary>
        /// Create a new tenant with the given name.
        /// <para>
        /// <c>options.SelfProvisioningDomains</c> is an optional list of domains that are associated with this
        /// tenant. Users authenticating from these domains will be associated with this tenant.
        /// </para>
        /// <para>
        /// The tenant <c>tenantRequest.Name</c> must be unique per project.
        /// The tenant ID is generated automatically for the tenant, unless given explicitly by the ID.
        /// </para>
        /// </summary>
        /// <param name="options">The options to create a tenant according to</param>
        /// <param name="id">Optional ID to use for the tenant. Leave <c>null</c> to it automatically generated</param>
        /// <returns>The newly created tenant's ID</returns>
        Task<string> Create(TenantOptions options, string? id = null);

        /// <summary>
        /// Update an existing tenant's name and domains.
        /// <para>
        /// IMPORTANT: All parameters are required and will override whatever value is currently
        /// set in the existing tenant. Use carefully.
        /// </para>
        /// </summary>
        /// <param name="id">The ID of the tenant to update</param>
        /// <param name="options">The tenants updated details</param>
        Task Update(string id, TenantOptions options);

        /// <summary>
        /// Delete an existing tenant.
        /// <para>
        /// IMPORTANT: This action is irreversible. Use carefully.
        /// </para>
        /// </summary>
        /// <param name="id">The ID of the tenant to delete</param>
        Task Delete(string id);

        /// <summary>
        /// Load project tenant by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The loaded tenant</returns>
        Task<TenantResponse> LoadById(string id);

        /// <summary>
        /// Load all project tenants
        /// </summary>
        /// <returns></returns>
        Task<List<TenantResponse>> LoadAll();

        /// <summary>
        /// Search all tenants according to given filters
        /// <para>
        /// The options optional parameter allows to fine-tune the search filters
        /// and results. Using nil will result in a filter-less query with a set amount of
        /// results.
        /// </para>
        /// </summary>
        /// <param name="options">Fine tune filters for the search</param>
        /// <returns>A list of found tenants</returns>
        Task<List<TenantResponse>> SearchAll(TenantSearchOptions? options = null);
    }

    /// <summary>
    /// Provides functions for managing users in a project
    /// </summary>
    public interface IUser
    {
        /// <summary>
        /// Create a new user.
        /// <para>
        /// The loginID is required and will determine what the user will use to
        /// sign in.
        /// </para>
        /// <para>
        /// IMPORTANT: When opting into invitations, since the invitation is sent by email / phone, make sure either
        /// the email / phone is explicitly set, or the loginId itself is an email address / phone number.
        /// You must configure the invitation URL in the Descope console prior to
        /// calling the method.
        /// </para>
        /// </summary>
        /// <param name="loginId">A login ID to identify the created user</param>
        /// <param name="request">Optional information about the user being created</param>
        /// <param name="options">Optional invite options used to send an invitation to the created user</param>
        /// <param name="testUser">Optionally create a test user</param>
        /// <returns>The created user</returns>
        Task<DescopeUser> Create(string loginId, UserRequest? request = null, InviteOptions? options = null, bool testUser = false);

        /// <summary>
        /// Create users in batch.
        /// <para>
        /// Functions exactly the same as the <c>Create</c> function with the additional behavior that
        /// users can be created with a cleartext or hashed password.
        /// </para>
        /// <para>
        /// IMPORTANT: When opting into invitations, since the invitation is sent by email / phone, make sure either
        /// the email / phone is explicitly set, or the loginId itself is an email address / phone number.
        /// You must configure the invitation URL in the Descope console prior to
        /// calling the method.
        /// </para>
        /// </summary>
        /// <param name="batchUsers">The list of users to create</param>
        /// <param name="options">Optional invite options used to send an invitation to the created users</param>
        /// <returns>A list of created users and a list of failures if occurred</returns>
        Task<BatchCreateUserResponse> CreateBatch(List<BatchUser> batchUsers, InviteOptions? options = null);

        /// <summary>
        /// Update an existing user.
        /// <para>
        /// The parameters follow the same convention as those for the <c>Create</c> function.
        /// </para>
        /// <para>
        /// IMPORTANT: All parameters will override whatever values are currently set
        /// in the existing user. Use carefully.
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        /// <param name="request">The information to set</param>
        /// <returns>The updated user</returns>
        Task<DescopeUser> Update(string loginId, UserRequest? request = null);

        /// <summary>
        /// Activate an existing user.
        /// </summary>
        /// <param name="loginId">The login ID of the user to activate</param>
        /// <returns>The activated user</returns>
        Task<DescopeUser> Activate(string loginId);

        /// <summary>
        /// Deactivate an existing user.
        /// </summary>
        /// <param name="loginId">The login ID of the user to deactivate</param>
        /// <returns>The deactivated user</returns>
        Task<DescopeUser> Deactivate(string loginId);

        /// <summary>
        /// Change current loginID to new one.
        /// <para>
        /// Leave <c>null</c> to remove the current login ID.
        /// Pay attention that if this is the only login ID, it cannot be removed
        /// </para>
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        /// <param name="newLoginId">The new login ID</param>
        /// <returns>The updated user</returns>
        Task<DescopeUser> UpdateLoginId(string loginId, string? newLoginId = null);

        /// <summary>
        /// Update the email address for an existing user.
        /// <para>
        /// The email parameter can be <c>null</c> in which case the email will be removed.
        /// </para>
        /// <para>
        /// The <c>verified</c> flag must be true for the user to be able to login with
        /// the email address.
        /// </para>
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        /// <param name="email">The email to update</param>
        /// <param name="verified">Whether this email is verified</param>
        /// <returns>The updated user</returns>
        Task<DescopeUser> UpdateEmail(string loginId, string? email = null, bool verified = false);

        /// <summary>
        /// Update the phone number for an existing user.
        /// <para>
        /// The phone parameter can be <c>null</c> in which case the phone will be removed.
        /// </para>
        /// <para>
        /// The <c>verified</c> flag must be true for the user to be able to login with
        /// the phone number.
        /// </para>
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        /// <param name="phone">The phone to update</param>
        /// <param name="verified">Whether this phone is verified</param>
        /// <returns>The updated user</returns>
        Task<DescopeUser> UpdatePhone(string loginId, string? phone = null, bool verified = false);

        /// <summary>
        /// Update an existing user's display name (i.e., their full name).
        /// <para>
        /// The displayName parameter can be <c>null</c> in which case the name will be removed.
        /// </para>
        /// </summary>
        /// <param name="loginId">The login ID of the user to update<</param>
        /// <param name="displayName">The display name to update</param>
        /// <returns>The updated user</returns>
        Task<DescopeUser> UpdateDisplayName(string loginId, string? displayName = null);

        /// <summary>
        /// Update an existing user's first/last/middle name.
        /// <para>
        /// A <c>null</c> parameter, means that this value will be removed.
        /// </para>
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        /// <param name="givenName">The given name to update</param>
        /// <param name="middleName">The middle name to update</param>
        /// <param name="familyName">The family name to update</param>
        /// <returns>The updated user</returns>
        Task<DescopeUser> UpdateUserNames(string loginId, string? givenName = null, string? middleName = null, string? familyName = null);

        /// <summary>
        /// Update an existing user's picture (i.e., url to the avatar).
        /// <para>
        /// The picture parameter can be <c>null</c> in which case the picture will be removed.
        /// </para>
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        /// <param name="picture">The updated picture URL</param>
        /// <returns>The updated user</returns>
        Task<DescopeUser> UpdatePicture(string loginId, string? picture);

        /// <summary>
        /// Update an existing user's custom attribute.
        /// <para>
        /// key should be a custom attribute that was already declared in the Descope console app.
        /// value should match the type of the declared attribute
        /// </para>
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        /// <param name="key">Existing attribute key</param>
        /// <param name="value">Value matching the given key</param>
        /// <returns>The updated user</returns>
        Task<DescopeUser> UpdateCustomAttributes(string loginId, string key, object value);

        /// <summary>
        /// Set roles for a user. If the intended roles are associated with a tenant, provide
        /// a tenantId.
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        /// <param name="roleNames">The roles to set</param>
        /// <param name="tenantId">Optional tenant association</param>
        /// <returns>The updated user</returns>
        Task<DescopeUser> SetRoles(string loginId, List<string> roleNames, string? tenantId = null);

        /// <summary>
        /// Add roles for a user. If the intended roles are associated with a tenant, provide
        /// a tenantId.
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        /// <param name="roleNames">The roles to add</param>
        /// <param name="tenantId">Optional tenant association</param>
        /// <returns>The updated user</returns>
        Task<DescopeUser> AddRoles(string loginId, List<string> roleNames, string? tenantId = null);

        /// <summary>
        /// Remove roles from a user. If the intended roles are associated with a tenant, provide
        /// a tenantId.
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        /// <param name="roleNames">The roles to remove</param>
        /// <param name="tenantId">Optional tenant association</param>
        /// <returns>The updated user</returns>
        Task<DescopeUser> RemoveRoles(string loginId, List<string> roleNames, string? tenantId = null);

        /// <summary>
        /// Set (associate) SSO applications for a user.
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        /// <param name="ssoAppIds">The SSO app IDs to set</param>
        /// <returns>The updated user</returns>
        Task<DescopeUser> SetSsoApps(string loginId, List<string> ssoAppIds);

        /// <summary>
        /// Associate SSO application for a user.
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        /// <param name="ssoAppIds">The SSO app IDs to add</param>
        /// <returns>The updated user</returns>
        Task<DescopeUser> AddSsoApps(string loginId, List<string> ssoAppIds);

        /// <summary>
        /// Remove SSO application association from a user.
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        /// <param name="ssoAppIds">The SSO app IDs to remove</param>
        /// <returns>The updated user</returns>
        Task<DescopeUser> RemoveSsoApps(string loginId, List<string> ssoAppIds);

        /// <summary>
        /// Add a tenant association for an existing user.
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        /// <param name="tenantId">The tenant ID to add</param>
        /// <returns>The updated user</returns>
        Task<DescopeUser> AddTenant(string loginId, string tenantId);

        /// <summary>
        /// Remove a tenant association from an existing user.
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        /// <param name="tenantId">The tenant ID to remove</param>
        /// <returns>The updated user</returns>
        Task<DescopeUser> RemoveTenant(string loginId, string tenantId);

        /// <summary>
        /// Set a temporary password for the given login ID.
        /// <para>
        /// Note: The password will automatically be set as expired.
        /// The user will not be able to log-in with this password, and will be required to replace it on next login.
        /// See also: ExpirePassword
        /// </para>
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        /// <param name="password">The temporary password to set</param>
        Task SetTemporaryPassword(string loginId, string password);

        /// <summary>
        /// Set a password for the given login ID.
        /// <para>
        /// The password will not be expired on the next login.
        /// </para>
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        /// <param name="password">The active password to set</param>
        Task SetActivePassword(string loginId, string password);

        /// <summary>
        /// Expire the password for the given login ID.
        /// <para>
        /// Note: user sign-in with an expired password, the user will get `errors.ErrPasswordExpired` error.
        /// Use the `SendPasswordReset` or `ReplaceUserPassword` methods to reset/replace the password.
        /// </para>
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        Task ExpirePassword(string loginId);

        /// <summary>
        /// Removes all registered passkeys (WebAuthn devices) for the user with the given login ID.
        /// <para>
        /// Note: The user might not be able to login anymore if they have no other authentication
        /// methods or a verified email/phone.
        /// </para>
        /// </summary>
        /// <param name="loginId">The login ID of the user to update</param>
        Task RemoveAllPasskeys(string loginId);

        /// <summary>
        /// Get the provider token for the given login ID.
        /// <para>
        /// Only users that sign-in using social providers will have token.
        /// Note: The 'Manage tokens from provider' setting must be enabled.
        /// </para>
        /// </summary>
        /// <param name="loginId">The login ID of the user to fetch tokens for</param>
        /// <param name="provider">The provider to fetch from</param>
        /// <returns>The provider token for the given user</returns>
        Task<ProviderTokenResponse> GetProviderToken(string loginId, string provider);

        /// <summary>
        /// Logout given user from all their devices, by loginId or userId
        /// </summary>
        /// <param name="loginId">The login ID of the user to logout. Alternatively, provide the userId</param>
        /// <param name="userId">The user ID of the user to logout. Alternatively, provide the loginId</param>
        Task Logout(string? loginId = null, string? userId = null);

        /// <summary>
        /// Delete an existing user.
        /// <para>
        /// IMPORTANT: This action is irreversible. Use carefully.
        /// </para>
        /// </summary>
        /// <param name="loginId">The login ID of the user to delete</param>
        Task Delete(string loginId);

        /// <summary>
        /// Delete all test users in the project.
        /// <para>
        /// IMPORTANT: This action is irreversible. Use carefully.
        /// </para>
        /// </summary>
        Task DeleteAllTestUsers();

        /// <summary>
        /// Load an existing user.
        /// </summary>
        /// <param name="loginId">The login ID of the user to load</param>
        /// <returns>The loaded user</returns>
        Task<DescopeUser> Load(string loginId);

        /// <summary>
        /// Search all users according to given filters
        /// <para>
        /// The options optional parameter allows to fine-tune the search filters
        /// and results. Using nil will result in a filter-less query with a set amount of
        /// results.
        /// </para>
        /// </summary>
        /// <param name="options">Parameter to fine tune the search by</param>
        /// <returns>A list of found users</returns>
        Task<List<DescopeUser>> SearchAll(SearchUserOptions? options = null);

        /// <summary>
        /// Generate OTP for the given login ID of a test user.
        /// <para>
        /// Choose the selected delivery method for verification. (see auth/DeliveryMethod)
        /// It returns the code for the login (exactly as it sent via Email or SMS)
        /// This is useful when running tests and don't want to use 3rd party messaging services
        /// The redirect URI is optional. If provided however, it will be used instead of any global configuration.
        /// </para>
        /// </summary>
        /// <param name="deliveryMethod">The intended delivery method</param>
        /// <param name="loginId">The login ID of the test user</param>
        /// <param name="loginOptions">Optional login options</param>
        /// <returns>The generated otp response</returns>
        Task<UserTestOTPResponse> GenerateOtpForTestUser(DeliveryMethod deliveryMethod, string loginId, LoginOptions? loginOptions = null);

        /// <summary>
        /// Generate Magic Link for the given login ID of a test user.
        /// <para>
        /// Choose the selected delivery method for verification. (see auth/DeliveryMethod)
        /// It returns the link for the login (exactly as it sent via Email)
        /// This is useful when running tests and don't want to use 3rd party messaging services
        /// The redirect URI is optional. If provided however, it will be used instead of any global configuration.
        /// </para>
        /// </summary>
        /// <param name="deliveryMethod">The intended delivery method</param>
        /// <param name="loginId">The login ID of the test user</param>
        /// <param name="redirectUrl"></param>
        /// <param name="loginOptions">Optional login options</param>
        /// <returns>The generated magic link response</returns>
        Task<UserTestMagicLinkResponse> GenerateMagicLinkForTestUser(DeliveryMethod deliveryMethod, string loginId, string? redirectUrl = null, LoginOptions? loginOptions = null);

        /// <summary>
        /// Generate Enchanted Link for the given login ID of a test user.
        /// <para>
        /// It returns the link for the login (exactly as it sent via Email) and pendingRef which is used to poll for a valid session
        /// This is useful when running tests and don't want to use 3rd party messaging services
        /// The redirect URI is optional. If provided however, it will be used instead of any global configuration.
        /// </para>
        /// </summary>
        /// <param name="loginId">The login ID of the test user</param>
        /// <param name="redirectUrl"></param>
        /// <param name="loginOptions">Optional login options</param>
        /// <returns>The generated enchanted link response</returns>
        Task<UserTestEnchantedLinkResponse> GenerateEnchantedLinkForTestUser(string loginId, string? redirectUrl = null, LoginOptions? loginOptions = null);

        /// <summary>
        /// Generate an embedded link token, later can be used to authenticate via magiclink verify method
        /// or via flow verify step
        /// </summary>
        /// <param name="loginId">The login ID of the test user</param>
        /// <param name="customClaims">Optional custom claims to be placed on the generated JWT after login</param>
        /// <returns>The generated embedded link response</returns>
        Task<string> GenerateEmbeddedLink(string loginId, Dictionary<string, object>? customClaims = null);
    }

    /// <summary>
    /// Provides functions for managing access keys in a project.
    /// </summary>
    public interface IAccessKey
    {
        /// <summary>
        /// Create a new access key.
        /// <para>
        /// IMPORTANT: The access key <c>cleartext</c> will be returned only when first created.
        /// Make sure to save it in a secure manner.
        /// </para>
        /// </summary>
        /// <param name="name">The access key's name. It doesn't have to be unique</param>
        /// <param name="expireTime">Optional expiration time, leave null to make indefinite</param>
        /// <param name="roleNames">An optional list of the access key's roles for access keys that aren't associated with a tenant</param>
        /// <param name="keyTenants">An optional list of tenants to associate the access key with and what roles the access key has in each one</param>
        /// <param name="userId">If userID is supplied, then authorization would be ignored, and access key will be bound to the users authorization</param>
        /// <returns>A newly created access key along with its cleartext</returns>
        Task<AccessKeyCreateResponse> Create(string name, int? expireTime = null, List<string>? roleNames = null, List<AssociatedTenant>? keyTenants = null, string? userId = null);

        /// <summary>
        /// Load an existing access key.
        /// </summary>
        /// <param name="id">The ID of the access key to update</param>
        /// <param name="name">The key's updated name</param>
        /// <returns>The updated access key</returns>
        Task<AccessKeyResponse> Update(string id, string name);

        /// <summary>
        /// Activate an existing access key.
        /// </summary>
        /// <param name="id">The ID of the access key to activate</param>
        Task Activate(string id);

        /// <summary>
        /// Deactivate an existing access key.
        /// </summary>
        /// <param name="id">The ID of the access key to deactivate</param>
        Task Deactivate(string id);

        /// <summary>
        /// Delete an existing access key.
        /// </summary>
        /// <param name="id">The ID of the access key to delete</param>
        Task Delete(string id);

        /// <summary>
        /// Load an existing access key.
        /// </summary>
        /// <param name="id">The ID of the access key to load</param>
        /// <returns>The loaded access key</returns>
        Task<AccessKeyResponse> Load(string id);

        /// <summary>
        /// Search all access keys according to given filters
        /// </summary>
        /// <param name="tenantIds">Optional list of tenant IDs to filter by.</param>
        /// <returns>A list of found access keys</returns>
        Task<List<AccessKeyResponse>> SearchAll(List<string>? tenantIds = null);
    }

    /// <summary>
    /// Provide functions for manipulating valid JWT
    /// </summary>
    public interface IJwt
    {
        /// <summary>
        /// Update a valid JWT with the custom claims provided
        /// </summary>
        /// <param name="jwt">A valid JWT to update</param>
        /// <param name="customClaims">The custom claims to be added to the JWT</param>
        /// <returns>An updated JWT</returns>
        Task<string> UpdateJwtWithCustomClaims(string jwt, Dictionary<string, object> customClaims);
    }

    /// <summary>
    /// Provides functions for exporting and importing project settings, flows, styles, etc.
    /// </summary>
    public interface IProject
    {
        /// <summary>
        /// Exports all settings and configurations for a project and returns the raw JSON
        /// files response as a map.
        /// <para>
        /// It's advised to use <c>descopeCLI</c> for easier importing and exporting
        /// </para>
        /// </summary>
        /// <returns>The exported project</returns>
        Task<object> Export();

        /// <summary>
        /// Imports all settings and configurations for a project overriding any current configuration.
        /// </summary>
        /// <param name="files">The result of an exported project</param>
        Task Import(object files);

        /// <summary>
        /// Update the current project name.
        /// </summary>
        /// <param name="name">The project's new name</param>
        Task Rename(string name);

        /// <summary>
        /// Clone the current project, including its settings and configurations.
        /// <para>
        /// - This action is supported only with a pro license or above.
        /// </para>
        /// <para>
        /// - Users, tenants and access keys are not cloned.
        /// </para>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="tag"></param>
        /// <returns>The new project details (name, id, and tag)</returns>
        Task<ProjectCloneResponse> Clone(string name, string tag);

        /// <summary>
        /// Delete a project.
        /// </summary>
        /// <param name="projectId">The ID of the project to be deleted</param>
        Task Delete(string projectId);
    }

    /// <summary>
    /// Provides various APIs for managing a Descope project programmatically. A management key must
    /// be provided in the DescopeClient configuration. Management keys can be generated in the Descope console.
    /// </summary>
    public interface IManagement
    {
        /// <summary>
        /// Provides functions for managing tenants in a project.
        /// </summary>
        public ITenant Tenant { get; }

        /// <summary>
        /// Provides functions for managing users in a project.
        /// </summary>
        public IUser User { get; }

        /// <summary>
        /// Provides functions for managing access keys in a project.
        /// </summary>
        public IAccessKey AccessKey { get; }

        /// <summary>
        /// Provides functions for exporting and importing project settings, flows, styles, etc.
        /// </summary>
        public IProject Project { get; }
    }
}
