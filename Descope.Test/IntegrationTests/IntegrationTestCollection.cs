using Xunit;

namespace Descope.Test.Integration
{
    /// <summary>
    /// Collection definitions for integration tests that are partitioned to allow controlled
    /// parallel execution. Each collection groups tests that don't conflict with each other,
    /// while tests within the same collection run sequentially.
    /// 
    /// Collections can run in parallel, but tests within a collection run sequentially.
    /// This balances test speed with avoiding resource conflicts and rate limits.
    /// </summary>

    /// <summary>
    /// Authentication flow tests (OTP, MagicLink, Password, etc.)
    /// These tests are stateless and don't modify shared resources.
    /// </summary>
    [CollectionDefinition("Authentication Tests")]
    public class AuthenticationTestCollection
    {
    }

    /// <summary>
    /// User management tests (CRUD operations on users)
    /// Sequential execution within this collection to manage rate limits (100 creates/60s, 200 updates/60s).
    /// </summary>
    [CollectionDefinition("User Management Tests")]
    public class UserManagementTestCollection
    {
    }

    /// <summary>
    /// Tenant management tests (CRUD operations on tenants)
    /// Sequential execution to avoid conflicts when listing/verifying tenants.
    /// </summary>
    [CollectionDefinition("Tenant Management Tests")]
    public class TenantManagementTestCollection
    {
    }

    /// <summary>
    /// Authorization tests (Roles, Permissions, FGA)
    /// Sequential execution to avoid conflicts with shared authorization resources.
    /// </summary>
    [CollectionDefinition("Authorization Tests")]
    public class AuthorizationTestCollection
    {
    }

    /// <summary>
    /// Project and settings tests (SSO, Password Settings, Third Party Apps, JWT, Access Keys)
    /// Sequential execution required as these modify project-level configuration.
    /// </summary>
    [CollectionDefinition("Project & Settings Tests")]
    public class ProjectSettingsTestCollection
    {
    }
}
