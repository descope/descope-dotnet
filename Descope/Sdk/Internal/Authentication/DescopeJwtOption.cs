using Microsoft.Kiota.Abstractions;
using System.Collections.Generic;

namespace Descope;

/// <summary>
/// Use to allow passing JWT tokens in Kiota requests if they need to be included in the Authorization header.
/// </summary>
internal class DescopeJwtOption : IRequestOption
{
    private readonly Dictionary<string, object> context;

    public DescopeJwtOption(Dictionary<string, object> context)
    {
        this.context = context ?? new Dictionary<string, object>();
    }

    public Dictionary<string, object> GetContext()
    {
        return context;
    }

    // Creates a new instance with a single JWT token in the context.
    public static DescopeJwtOption WithJwt(string jwt)
    {
        return new DescopeJwtOption(new Dictionary<string, object>
        {
            { "jwt", jwt }
        });
    }

}
