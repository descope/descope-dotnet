using Microsoft.Kiota.Abstractions;
using System.Collections.Generic;

namespace Descope;

/// <summary>
/// Use to allow passing access keys in Kiota requests if they need to be included in the Authorization header.
/// Unlike JWT tokens, access keys should not trigger appending the auth management key.
/// </summary>
internal class DescopeKeyOption : IRequestOption
{
    private readonly Dictionary<string, object> context;

    public DescopeKeyOption(Dictionary<string, object> context)
    {
        this.context = context ?? new Dictionary<string, object>();
    }

    public Dictionary<string, object> GetContext()
    {
        return context;
    }

    // Creates a new instance with a single access key in the context.
    public static DescopeKeyOption WithKey(string key)
    {
        return new DescopeKeyOption(new Dictionary<string, object>
        {
            { "key", key }
        });
    }
}
