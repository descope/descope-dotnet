using System.Text.Json.Serialization;

namespace Descope.Internal.Auth
{
    internal class MaskedAddressResponse
    {
        [JsonPropertyName("maskedEmail")]
        public string? MaskedEmail { get; set; }

        [JsonPropertyName("maskedPhone")]
        public string? MaskedPhone { get; set; }
    }
}
