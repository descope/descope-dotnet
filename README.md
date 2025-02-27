# Descope SDK for .NET

The Descope SDK for .NET provides convenient access to the Descope user management and authentication API for a backend written in .NET. You can read more on the [Descope Website](https://descope.com).

## Requirements

The SDK supports `.NET 6` and `.NET 8`.

## Setup

A Descope `Project ID` is required to initialize the SDK. Find it on the
[project page in the Descope Console](https://app.descope.com/settings/project).

```cs
using Descope;

// ... In your setup code

var config = new DescopeConfig(projectId: "projectId");
var descopeClient = new DescopeClient(config);
```

## Authentication Functions

These sections show how to use the SDK to perform various authentication/authorization functions:

1. [OTP](#otp-authentication)
2. [OAuth](#oauth)
3. [SSO](#sso-saml--oidc)

## Management Functions

These sections show how to use the SDK to perform API management functions. Before using any of them, you will need to create a Management Key. The instructions for this can be found under [Setup](#setup-1).

1. [Manage Tenants](#manage-tenants)
2. [Manage Users](#manage-users)
3. [Manage Access Keys](#manage-access-keys)
4. [Manage SSO Settings](#manage-sso-setting)
5. [Manage SSO Applications](#manage-sso-applications)
6. [Manage Password Settings](#manage-password-settings)
7. [Manage and Manipulate JWTs](#manage-and-manipulate-jwts)
8. [Manage Permissions](#manage-permissions)
9. [Manage Roles](#manage-roles)
10. [Manage Project](#manage-project)

---

### OTP Authentication

Send a user a one-time password (OTP) using your preferred delivery method (_email / SMS_). An email address or phone number must be provided accordingly.

The user can either `sign up`, `sign in` or `sign up or in`

```cs
// Every user must have a loginID. All other user information is optional
var loginId = "desmond@descope.com";
var signUpDetails = new SignUpDetails
{
    Name = "Desmond Copeland",
    GivenName = "Desmond",
    FamilyName = "Copeland",
    Phone = "212-555-1234",
    Email = loginId,
};
try
{
    var maskedAddress = await descopeClient.Auth.Otp.SignUp(DeliveryMethod.Email, loginId, signUpDetails);
}
catch (DescopeException e)
{
    // handle errors
}
```

The user will receive a code using the selected delivery method. Verify that code using:

```cs
try
{
    var authInfo = await descopeClient.Auth.Otp.VerifyCode(DeliveryMethod.Email, loginId, code);
}
catch
{
    // handle error
}
```

The session and refresh JWTs should be returned to the caller, and passed with every request in the session. Read more on [session validation](#session-validation)

### OAuth

Users can authenticate using their social logins, via the OAuth protocol. Configure your OAuth settings on the [Descope console](https://app.descope.com/settings/authentication/social).
To start an OAuth redirect chain call:

```cs
// Choose an oauth provider out of the supported providers, or create a custom one.
// If configured globally, the redirect URL is optional. If provided however, it will be used
// instead of any global configuration.
// Redirect the user to the returned URL to start the OAuth redirect chain
try
{
    var url = await descopeClient.Auth.OAuth.SignUpOrIn(OAuthProvider.Google, "https://my-app.com/handle-oauth");
}
catch
{
    // handle error
}
```

The user will authenticate with the authentication provider, and will be redirected back to the redirect URL, with an appended `code` HTTP URL parameter. Exchange it to validate the user:

```cs
try
{
    var authInfo = await descopeClient.Auth.OAuth.Exchange(code);
}
catch
{
    // handle error
}
```

Users can also connect the social login account to their existing user:

```cs
// A valid Refresh Token of the existing user is required.
// If allowAllMerge is 'true' the users will be merged also if there is no common identifier between the social provider and the existing user (like email).
try
{
    var url = await descopeClient.Auth.OAuth.UpdateUser(OAuthProvider.Google, "<refresh-jwt>", "https://my-app.com/handle-oauth", allowAllMerge: true);
}
catch
{
    // handle error
}
```

The session and refresh JWTs should be returned to the caller, and passed with every request in the session. Read more on [session validation](#session-validation)

### SSO (SAML / OIDC)

Users can authenticate to a specific tenant using SAML or OIDC. Configure your SSO (SAML / OIDC) settings on the [Descope console](https://app.descope.com/settings/authentication/sso). To start a flow call:

These are the parameters you can send in addition to `tenant` and `redirectUrl`:

| Parameter    | Protocol | Description |
|-------------|----------|-------------|
| `prompt`    | OIDC     | An OIDC parameter that lets you control whether a user has to sign in, give consent, etc. For more information, visit our docs [here](https://docs.descope.com/auth-methods/oauth#prompt). *(Doesn't work with SAML)* |
| `forceAuthn` | SAML     | A SAML parameter that lets you control whether a user has to sign in again, regardless of a previously established SSO session. *(Doesn't work with OIDC)* |

```cs
// Choose which tenant to log into
// If configured globally, the redirect URL is optional. If provided however, it will be used
// instead of any global configuration.
// Redirect the user to the returned URL to start the SSO SAML/OIDC redirect chain
try
{
    var redirectUrl = await descopeClient.Auth.Sso.Start(tenant: "my-tenant-ID", redirectUrl: "https://my-app.com/handle-saml")
}
catch
{
    // handle error
}
```

The user will authenticate with the authentication provider configured for that tenant, and will be redirected back to the redirect URL, with an appended `code` HTTP URL parameter. Exchange it to validate the user:

```cs
try
{
    var authInfo = await descopeClient.Auth.Sso.Exchange(code);
}
catch
{
    // handle error
}
```

The session and refresh JWTs should be returned to the caller, and passed with every request in the session. Read more on [session validation](#session-validation)

### Session Validation

Every secure request performed between your client and server needs to be validated. The client sends
the session and refresh tokens with every request, and they are validated using one of the following:

```cs
// Validate the session. Will return an error if expired
try
{
    var sessionToken = await descopeClient.Auth.ValidateSession(sessionJwt);
}
catch (DescopeException e)
{
    // unauthorized error
}

// If ValidateSession throws an exception, you will need to refresh the session using
try
{
    var sessionToken = await descopeClient.Auth.RefreshSession(refreshJwt);
}
catch (DescopeException e)
{
    // unauthorized error
}

// Alternatively, you could combine the two and
// have the session validated and automatically refreshed when expired
try
{
    var sessionToken = descopeClient.Auth.ValidateAndRefreshSession(sessionJwt, refreshJwt);
}
catch  (DescopeException e)
{
    // unauthorized error
}
```

Choose the right session validation and refresh combination that suits your needs.

Refreshed sessions return the same response as is returned when users first sign up / log in,
Make sure to return the session token from the response to the client if tokens are validated directly.

Usually, the tokens can be passed in and out via HTTP headers or via a cookie.
The implementation can defer according to your implementation.

If Roles & Permissions are used, validate them immediately after validating the session. See the [next section](#roles--permission-validation)
for more information.

### Roles & Permission Validation

When using Roles & Permission, it's important to validate the user has the required
authorization immediately after making sure the session is valid. Taking the `sessionToken`
received by the [session validation](#session-validation), call the following functions, while for multi-tenant use cases
make sure to pass in the tenant ID, otherwise leave as `null`:

```cs
// You can validate specific permissions
if (!descopeClient.Auth.ValidatePermissions(sessionToken, new List<string> { "Permission to validate" }, "optional-tenant-ID"))
{
    // Deny access
}

// Or validate roles directly
if (!descopeClient.Auth.ValidateRoles(sessionToken, new List<string> { "Role to validate" }, "optional-tenant-ID"))
{
    // Deny access
}

var matchedRoles = descopeClient.Auth.GetMatchedRoles(sessionToken, new List<string> { "role-name1", "role-name2" }, "optional-tenant-ID");
var matchedPermissions = descopeClient.Auth.GetMatchedPermissions(sessionToken, new List<string> { "permission-name1", "permission-name2" }, "optional-tenant-ID");
```

### Tenant selection

For a user that has permissions to multiple tenants, you can set a specific tenant as the current selected one
This will add an extra attribute to the refresh JWT and the session JWT with the selected tenant ID

```cs
var tenantId = "t1";
try
{
    var session = await descopeClient.Auth.SelectTenant(tenantId, refreshJwt);
}
catch  (DescopeException e)
{
    // failed to select a tenant
}
```

### Logging Out

You can log out a user from an active session by providing their `refreshJwt` for that session.
After calling this function, you must invalidate or remove any cookies you have created.

```cs
await descopeClient.Auth.Logout(refreshJwt);
```

It is also possible to sign the user out of all the devices they are currently signed-in with. Calling `LogoutAll` will
invalidate all user's refresh tokens. After calling this function, you must invalidate or remove any cookies you have created.

```cs
await descopeClient.Auth.LogoutAll(refreshJwt);
```

## Management Functions

It is very common for some form of management or automation to be required. These can be performed
using the management functions. Please note that these actions are more sensitive as they are administrative
in nature. Please use responsibly.

### Setup

To use the management API you'll need a `Management Key` along with your `Project ID`.
Create one in the [Descope Console](https://app.descope.com/settings/company/managementkeys).

```cs
using Descope;

// ... In your setup code
var config = new DescopeConfig(projectId: "projectId");
var descopeClient = new DescopeClient(config)
{
    ManagementKey = "management-key",
};
```

### Manage Tenants

You can create, update, delete or load tenants:

```cs
// The self provisioning domains or optional. If given they'll be used to associate
// Users logging in to this tenant

// Creating and updating tenants takes a TenantOptions. For example:
var options = new TenantOptions("name")
{
    SelfProvisioningDomains = new List<string> { "domain" },
    CustomAttributes = new Dictionary<string, object> { { "mycustomattribute", "test" } },
};
try
{
    // Create tenant
    var tenantId = await descopeClient.Management.Tenant.Create(options: options);

    // You can optionally set your own ID when creating a tenant
    var tenantId = await descopeClient.Management.Tenant.Create(options: options, id: "my-tenant-id");

    // Update will override all fields as is. Use carefully.
    await descopeClient.Management.Tenant.Update(tenantId, updatedTenantOptions);

    // Tenant deletion cannot be undone. Use carefully.
    await descopeClient.Management.Tenant.Delete(tenantId);

    // Load tenant by id
    var tenant = await descopeClient.Management.Tenant.Load("my-custom-id");

    // Load all tenants
    var tenants = await descopeClient.Management.Tenant.LoadAll();
    foreach (var tenant in tenants)
    {
        // do something
    }

    // Search tenants - takes the &descope.TenantSearchOptions type. This is an example of a &descope.TenantSearchOptions
    var searchOptions = new TenantSearchOptions
    {
        Ids = new List<string> {"my-custom-id"},
        Names = new List<string> { "My Tenant" },
        SelfProvisioningDomains = new List<string> {"domain.com", "company.com"},
        CustomAttributes = new Dictionary<string, object> {{ "mycustomattribute": "Test" }},
    };
    var tenants = await descopeClient.Management.Tenant.SearchAll(searchOptions);
    foreach (var tenant in tenants)
    {
        // do something
    }
}
catch  (DescopeException e)
{
    // handle errors
}
```

### Manage Users

You can create, update, delete, logout, get user history and load users, as well as search according to filters:

```cs
try
{
    // A user must have a loginId, other fields are optional.
    // Roles should be set directly if no tenants exist, otherwise set
    // on a per-tenant basis.
    await descopeClient.Management.User.Create(loginId: "desmond@descope.com", new UserRequest()
    {
        Email = "desmond@descope.com",
        Name = "Desmond Copeland",
        GivenName = "Desmond",
        FamilyName = "Copeland",
        UserTenants = new List<AssociatedTenant>
        {
            new(tenantId:"tenant-ID1") { RoleNames = new List<string> { "role-name1" }},
            new(tenantId:"tenant-ID2"),
        },
        SsoAppIds = new List<string> { "appId1", "appId2" },
    });

    // Alternatively, a user can be created and invited via an email or text message.
    // Make sure to configure the invite URL in the Descope console prior to using this function,
    // or provide the necessary invite options for sending invitation, and that an email address
    // or phone number is provided in the information.
    var inviteOptions = new InviteOptions()
    {
        // options can be null, and in this case, value will be taken from project settings page
        // otherwise provide them here
    };
    await descopeClient.Management.User.Create(loginId: "desmond@descope.com", new UserRequest()
    {
        Email = "desmond@descope.com",
        SsoAppIds = new List<string> { "appId1", "appId2" },
    }, sendInvite: true, inviteOptions: new InviteOptions());

    // User creation and invitation can also be performed in a similar fashion but as a batch operation
    var batchUsers = new List<BatchUser>()
    {
        new(loginId: "user1@something.com")
        {
            Email = "user1@something.com",
            VerifiedEmail = true,
        },
        new(loginId: "user2@something.com")
        {
            Email = "user2@something.com",
            VerifiedEmail = false,
        }
    };
    var result = await descopeClient.Management.User.CreateBatch(batchUsers);

    // Import users from another service by calling CreateBatch with each user's password hash
    var user = new BatchUser("desmond@descope.com")
    {
        Password = new BatchUserPassword
        {
            Hashed = new BatchUserPasswordHashed
            {
                Bcrypt = new BatchUserPasswordBcrypt(hash: "$2a$...")
            },
        },
    };
    var users = await descopeClient.Management.User.CreateBatch(new List<BatchUser> { user });

    // Update will override all fields as is. Use carefully.
    await descopeClient.Management.User.Update(loginId: "desmond@descope.com", new UserRequest()
    {
        Email = "desmond@descope.com",
        Name = "Desmond Copeland",
        GivenName = "Desmond",
        FamilyName = "Copeland",
        UserTenants = new List<AssociatedTenant>
        {
            new(tenantId:"tenant-ID2"),
        },
        SsoAppIds = new List<string> { "appId3" },
    });

    // Update loginId of a user, or remove a login ID (last login ID cannot be removed)
    await descopeClient.Management.User.UpdateLoginIs("desmond@descope.com", "bane@descope.com");

    // Associate SSO application for a user.
    var user = await descopeClient.Management.User.AddSsoApps("desmond@descope.com", new List<string> { "appId1" });

    // Set (associate) SSO application for a user.
    var user = await descopeClient.Management.User.SetSsoApps("desmond@descope.com", new List<string> { "appId1" });

    // Remove SSO application association from a user.
    var user = await descopeClient.Management.User.RemoveSsoApps("desmond@descope.com", new List<string> { "appId1" });

    // User deletion cannot be undone. Use carefully.
    await descopeClient.Management.User.Delete("desmond@descope.com");

    // Load specific user
    var userRes = descopeClient.Management.User.Load("desmond@descope.com");

    // Search all users, optionally according to tenant and/or role filter
    // Results can be paginated using the limit and page parameters
    var usersResp = descopeClient.Management.User.SearchAll(new SearchUserOptions
    {
        TenantIds = new List<string> { "my-tenant-id" },
    });

    // Logout given user from all its devices, by loginId or by userId
    await descopeClient.Management.User.Logout(loginId: "<login id>", userId: "<optionally a user id>");
}
catch  (DescopeException e)
{
    // handle any errors
}
```

#### Set or Expire User Password

You can set a new active password for a user, which they can then use to sign in. You can also set a temporary
password that the user will be forced to change on the next login.

```cs
// Set a temporary password for the user which they'll need to replace it on next login
await descopeClient.Management.User.SetTemporaryPassword("<login-id>", "<some-password>");

// Set an active password for the user which they can use to login
await descopeClient.Management.User.SetActivePassword("<login-id>", "<some-password>")
```

For a user that already has a password, you can expire it to require them to change it on the next login.

```cs
// Expire the user's active password
await descopeClient.Management.User.ExpirePassword("<login-id>");

// Later, if the user is signing in with an expired password, the returned error will be ErrPasswordExpired
```

### Manage Access Keys

You can create, update, delete or load access keys, as well as search according to filters:

```cs
try
{
    // An access key must have a name and expireTime, other fields are optional.
    // Roles should be set directly if no tenants exist, otherwise set
    // on a per-tenant basis.
    // If userID is supplied, then authorization would be ignored, and access key would be bound to the users authorization
    var accessKey = await descopeClient.Management.AccessKey.Create(name: "access-key-1", expireTime: 0, keyTenants: new List<AssociatedTenant> {
        new("tenant-ID1"){RoleNames= new List<string>{"role-name1"}},
        new("tenant-ID2"),
    });

    // Load specific access key
    var accessKey = await descopeClient.Management.AccessKey.Load("access-key-id");

    // Search all access keys, optionally according to tenant and/or role filter
    var accessKeys = await descopeClient.Management.AccessKey.SearchAll(new List<string>{ "my-tenant-id" });

    // Update will override all fields as is. Use carefully.
    var accessKey = await descopeClient.Management.AccessKey.Update("access-key-id", "updated-name");

    // Access keys can be deactivated to prevent usage. This can be undone using "activate".
    await descopeClient.Management.AccessKey.Deactivate("access-key-id");

    // Disabled access keys can be activated once again.
    await descopeClient.Management.AccessKey.Activate("access-key-id");

    // Access key deletion cannot be undone. Use carefully.
    await descopeClient.Management.AccessKey.Delete("access-key-id");
}
catch (DescopeException e)
{
    // handle errors
}
```

Exchange the access key and provide optional access key login options:

```cs
var loginOptions = new AccessKeyLoginOptions
{
	CustomClaims = new Dictionary<string, object> { {"k1": "v1"} },
}
var token = await descopeClient.Auth.ExchangeAccessKey("accessKey", loginOptions);
```

### Manage SSO Setting

You can manage SSO (SAML or OIDC) settings for a specific tenant.

```cs
try
{
    // Load all tenant SSO settings
    var ssoSettings = await descopeClient.Management.Sso.LoadSettings("tenant-id");

    // Configure tenant SSO by OIDC settings
    var oidcSettings = new SsoOidcSettings{...};
    await descopeClient.Management.Sso.ConfigureOidcSettings("tenant-id", oidcSettings);
    // OR
    // Load all tenant SSO settings and use them to configure OIDC settings
    ssoSettings = await descopeClient.Management.Sso.LoadSettings("tenant-id");
    ssoSettings.Oidc.Name = "my prOvider";
    ssoSettings.Oidc.AuthURL = authorizeEndpoint;
    ...
    ssoSettings.Oidc.Scope = new List<string>{"openid", "profile", "email"};
    await descopeClient.Management.Sso.ConfigureOidcSettings("tenant-id", ssoSettings.Oidc);

    // Configure tenant SSO by SAML settings
    var tenantId = "tenant-id"; // Which tenant this configuration is for
    var idpURL = "https://idp.com";
    var entityID = "my-idp-entity-id";
    var idpCert = "<your-cert-here>";
    var redirectURL = "https://my-app.com/handle-saml"; // Global redirect URL for SSO/SAML
    var domain = "domain.com"; // Users logging in from this domain will be logged in to this tenant
    var samlSettings = SsoSamlSettings(idpURL, entityID, idpCert) {
        AttributeMapping: new AttributeMapping{Email: "myEmail", ..},
        RoleMappings: new List<RoleMapping>{...},
    };
    await descopeClient.Management.Sso.ConfigureSAMLSettings(tenantId, samlSettings, redirectURL, domain);

    // Alternatively, configure using an SSO SAML metadata URL
    var samlSettings = new SsoSamlSettingsByMetadata ("https://idp.com/my-idp-metadata") {
        AttributeMapping: new AttributeMapping{Email: "myEmail", ..},
        RoleMappings: new List<RoleMapping>{...},
    };
    await descopeClient.Management.Sso.ConfigureSAMLSettingsByMetadata(tenantId, samlSettings, redirectURL, domain);

    // To delete SSO settings, call the following method
    await descopeClient.Management.Sso.DeleteSettings("tenant-id");
}
catch (DescopeException e)
{
    // handle errors
}
```

Note: Certificates should have a similar structure to:

```
-----BEGIN CERTIFICATE-----
Certifcate contents
-----END CERTIFICATE-----
```

### Manage SSO Applications

You can create, update, delete or load sso applications:

```cs
try
{
    // Create OIDC SSO application
    var oidcOptions = new OidcApplicationOptions("My OIDC App", "http://loginurl.com") { Enabled = true };
    var appId = await descopeClient.Management.SsoApplication.CreateOidcApplication(oidcOptions);

    // Create SAML SSO application
    var samlOptions = new SamlApplicationOptions("samlApp", "http://loginurl.com")
    {
        Id = samlAppID,
        Enabled = true,
        EntityID = "EntityID",
        AcsURL = "http://dummy.com/acs",
        Certificate: "cert",
        AttributeMapping = new List<SamlIdpAttributeMappingInfo> { new ("attrName1", "attrType1", "attrValue1") },
        GroupsMapping = new List<SamlIdpGroupsMappingInfo>
        {
            new("grpName1", "grpType1", "grpFilterType1", "grpValue1", new List<SamlIdpRoleGroupMappingInfo> { new("rl1", "rlName1") })
        },
    };
    appId = await descopeClient.Management.SsoApplication.CreateSamlApplication(samlOptions);

    // Update OIDC SSO application
    // Update will override all fields as is. Use carefully.
    var oidcOptions = new OidcApplicationOptions("My OIDC App", "http://updated-loginurl.com") { Id = oidcAppId, Enabled = true };
    await descopeClient.Management.SsoApplication.UpdateOidcApplication(options);

    // Update SAML SSO application
    // Update will override all fields as is. Use carefully.
    var samlOptions = new SamlApplicationOptions("samlApp", "http://loginurl.com")
    {
        Id = samlAppID,
        Enabled = true,
        UseMetadataInfo = true,
        MetadataURL = "https://metadata.com",
    };
    await descopeClient.Management.SsoApplication.UpdateSamlApplication(samlOptions);

    // Load SSO application by id
    var app = await descopeClient.Management.SsoApplication.Load( "appId");

    // Load all SSO applications
    var apps = await descopeClient.Management.SsoApplication.LoadAll();

    // SSO application deletion cannot be undone. Use carefully.
    await descopeClient.Management.SsoApplication.Delete("appId");
}
catch
{
    // handle errors
}
```

### Manage Password Settings

You can manage password settings for your project or tenants.

```cs
try
{
    // You can get password settings for the project or for a specific tenant ID.
    var settings = await _descopeClient.Management.Password.GetSettings("optional-tenant-id");

    // You can configure the project level settings, by leaving the optional tenant ID empty,
    // or tenant level password settings by providing a tenant ID.
    // The update is performed as-is in an overriding manner - use carefully.
    var updatedSettings = new PasswordSettings
    {
        Enabled = true,
        MinLength = 8,
        Lowercase = true,
        Uppercase = true,
        Number = true,
        NonAlphanumeric = true,
        Expiration = true,
        ExpirationWeeks = 3,
        Reuse = true,
        ReuseAmount = 3,
        Lock = true,
        LockAttempts = 5,
    };
    await _descopeClient.Management.Password.ConfigureSettings(updatedSettings, "optional-tenant-id");

}
catch (DescopeException e)
{
    // handle errors
}
```

### Manage and Manipulate JWTs

You can update custom claims on a valid JWT or even impersonate a different user - as long as the
impersonating user has the permission to do so.

```cs
try
{
    // add custom claims to a valid JWT
    var updatedJwt = await _descopeClient.Management.Jwt.UpdateJwtWithCustomClaims(jwt, myCustomClaims);

    // impersonate a user, assuming the user identified by `impersonatorId` has the `Impersonation` permission.
    // the resulting JWT will have the actor (`act`) custom claim identifying the impersonator
     var impersonatedJwt = await _descopeClient.Management.Jwt.Impersonate(impersonatorId, impersonatedLoginId);
}
catch (DescopeException e)
{
    // handle errors
}
```

### Manage Permissions

You can create, update, delete or load permissions:

```cs
try
{
    // You can optionally set a description for a permission.
    var name = "My Permission";
    var description = "Optional description to briefly explain what this permission allows.";
    await descopeClient.Management.Permission.Create(name, description);

    // Update will override all fields as is. Use carefully.
    var newName = "My Updated Permission";
    description = "A revised description";
    await descopeClient.Management.Permission.Update(name, newName, description);

    // Permission deletion cannot be undone. Use carefully.
    await descopeClient.Management.Permission.Delete(newName);

    // Load all permissions
    var permissions = await descopeClient.Management.Permission.LoadAll();
    foreach(var permission in permissions)
    {
        // Do something
    }
}
catch (DescopeException e)
{
    // handle errors
}
```

### Manage Roles

You can create, update, delete or load roles:

```cs
try
{
    // You can optionally set a description and associated permission for a roles.
    var name = "My Role";
    var description = "Optional description to briefly explain what this role allows.";
    var permissionNames = new List<string>{"My Updated Permission"};
    var tenantId = "" // set here tenant ID value in order to create a role for a specific tenant
    await descopeClient.Management.Role.Create(name, description, permissionNames, tenantId);

    // Update will override all fields as is. Use carefully.
    var newName = "My Updated Role";
    description = "A revised description";
    permissionNames.Add("Another Permission");
    await descopeClient.Management.Role.Update(name, newName, description, permissionNames, tenantId);

    // Role deletion cannot be undone. Use carefully.
    await descopeClient.Management.Role.Delete(newName, tenantId);

    // Load all roles
    var roles = await descopeClient.Management.Role().LoadAll(context.Background());
    foreach(var role in roles)
    {
        // Do something
    }

    // Search roles
    roles = await descopeClient.Management.Role.Search(new RoleSearchOptions
    {
        TenantIds: new List<string>{"tenant1", "tenant2"},
        RoleNames: new List<string>{"name1"},
    });
    foreach(var role in roles)
    {
        // Do something
    }
}
catch (DescopeException e)
{
    // handle errors
}
```

### Manage Project

You can update project name, as well as to clone the current project to a new one:

```cs
try
{
    // Update project name
    await descopeClient.Management.Project.Rename("new-project-name");

    // Clone the current project to a new one
    // Note that this action is supported only with a pro license or above.
    var res = await descopeClient.Management.Project.Clone("new-project-name", "");

    // Delete the current project. Kindly note that following calls on the `descopeClient` are most likely to fail because the current project has been deleted
    await descopeClient.Management.Project.Delete("projectId");
}
catch (DescopeException e)
{
    // handle errors
}
```

## Learn More

To learn more please see the [Descope Documentation and API reference page](https://docs.descope.com/).

## Contact Us

If you need help you can email [Descope Support](mailto:support@descope.com)

## License

The Descope SDK for .NET is licensed for use under the terms and conditions of the [MIT license Agreement](https://github.com/descope/descope-dotnet/blob/main/LICENSE).
