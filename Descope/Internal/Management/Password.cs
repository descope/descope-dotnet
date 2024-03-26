using System.Text.Json.Serialization;

namespace Descope.Internal.Management
{
    internal class Password : IPasswordSettings
    {
        private readonly IHttpClient _httpClient;
        private readonly string _managementKey;

        internal Password(IHttpClient httpClient, string managementKey)
        {
            _httpClient = httpClient;
            _managementKey = managementKey;
        }

        public async Task<PasswordSettings> GetSettings(string? tenantId = null)
        {
            return await _httpClient.Get<PasswordSettings>(Routes.PasswordSettings, _managementKey, queryParams: new Dictionary<string, string?> { { "tenantId", tenantId } });
        }

        public async Task ConfigureSettings(PasswordSettings settings, string? tenantId = null)
        {
            var body = new WrappedSettings
            {
                TenantId = tenantId,
                Enabled = settings.Enabled,
                MinLength = settings.MinLength,
                Lowercase = settings.Lowercase,
                Uppercase = settings.Uppercase,
                Number = settings.Number,
                NonAlphanumeric = settings.NonAlphanumeric,
                Expiration = settings.Expiration,
                ExpirationWeeks = settings.ExpirationWeeks,
                Reuse = settings.Reuse,
                ReuseAmount = settings.ReuseAmount,
                Lock = settings.Lock,
                LockAttempts = settings.LockAttempts,
            };
            await _httpClient.Post<object>(Routes.PasswordSettings, _managementKey, body);
        }

    }

    internal class WrappedSettings : PasswordSettings
    {
        [JsonPropertyName("tenantId")]
        public string? TenantId { get; set; }
    }

}
