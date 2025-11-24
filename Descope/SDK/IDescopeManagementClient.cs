using Descope.Mgmt.Scim;
using Descope.Mgmt.V1;
using Descope.Mgmt.V2;

namespace Descope;

/// <summary>
/// Interface for the Descope Management Client.
/// This wrapper allows for dependency injection and testability.
/// </summary>
public interface IDescopeManagementClient
{
    /// <summary>
    /// Access to SCIM API endpoints.
    /// </summary>
    ScimRequestBuilder Scim { get; }

    /// <summary>
    /// Access to V1 API endpoints.
    /// </summary>
    V1RequestBuilder V1 { get; }

    /// <summary>
    /// Access to V2 API endpoints.
    /// </summary>
    V2RequestBuilder V2 { get; }
}
