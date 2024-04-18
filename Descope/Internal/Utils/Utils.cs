namespace Descope.Internal
{
    internal class Utils
    {
        internal static void EnforceRequiredArgs(params (string, object?)[] args)
        {
            foreach (var arg in args)
            {
                if (arg.Item2 == null || (arg.Item2 is string s && string.IsNullOrEmpty(s)))
                {
                    throw new DescopeException($"The {arg.Item1} argument is required");
                }
            }
        }
    }
}
