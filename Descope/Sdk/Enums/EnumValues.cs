namespace Descope;

/// <summary>
/// Constant values for various enums used in Descope SDK API Requests and responses.
/// </summary>
public static class EnumValues
{

    public static class DeliveryMethod
    {
        public const string Email = "email";
        public const string Sms = "sms";
        public const string Whatsapp = "whatsapp";
    }

    public static class UserStatus
    {
        public const string Enabled = "enabled";
        public const string Disabled = "disabled";
        public const string Invited = "invited";
    }
}
