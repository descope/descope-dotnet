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

    internal class UrlResponse
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        public UrlResponse(string url)
        {
            Url = url;
        }
    }
}
