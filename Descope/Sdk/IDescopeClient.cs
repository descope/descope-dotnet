using Descope.Mgmt;
using Descope.Auth;

namespace Descope;

/// <summary>
/// Interface for the Descope Client.
/// This wrapper allows for dependency injection and testability.
/// </summary>
public interface IDescopeClient
{
    /// <summary>
    /// Access to Management API endpoints.
    /// Use Mgmt.V1 or Mgmt.V2 to access specific API versions.
    /// </summary>
    DescopeClient.DescopeMgmtClient Mgmt { get; }

    /// <summary>
    /// Access to Authentication API endpoints.
    /// Use Auth.V1 to access the authentication API.
    /// </summary>
    DescopeClient.DescopeAuthClient Auth { get; }
}
