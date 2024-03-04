using RestSharp;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Descope.Internal
{
    public interface IHttpClient
    {
        DescopeConfig DescopeConfig { get; set; }

        Task<TResponse> Get<TResponse>(string resource, string? pswd = null);

        Task<TResponse> Post<TResponse>(string resource, string? pswd = null, object? body = null);

        Task<TResponse> Delete<TResponse>(string resource, string pswd);
    }

    public class HttpClient : IHttpClient
    {
        DescopeConfig IHttpClient.DescopeConfig { get => _descopeConfig; set => _descopeConfig = value; }

        private DescopeConfig _descopeConfig;
        private readonly RestClient _client;

        public HttpClient(DescopeConfig descopeConfig)
        {
            _descopeConfig = descopeConfig;
            var baseUrl = descopeConfig.BaseURL ?? "https://api.descope.com";

            // init rest client
            var options = new RestClientOptions(baseUrl);
            if (_descopeConfig.Unsafe) options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            _client = new RestClient(options);
            _client.AddDefaultHeader("Accept", "application/json");
            _client.AddDefaultHeader("Content-Type", "application/json");
            _client.AddDefaultHeader("x-descope-sdk-name", SdkInfo.Name);
            _client.AddDefaultHeader("x-descope-sdk-version", SdkInfo.Version);
            _client.AddDefaultHeader("x-descope-sdk-dotnet-version", Environment.Version.ToString());
        }

        public async Task<TResponse> Get<TResponse>(string resource, string? pswd = null)
        {
            return await Call<TResponse>(resource, Method.Get, pswd);
        }

        public async Task<TResponse> Post<TResponse>(string resource, string? pswd = null, object? body = null)
        {
            return await Call<TResponse>(resource, Method.Post, pswd, body);
        }

        public async Task<TResponse> Delete<TResponse>(string resource, string? pswd = null)
        {
            return await Call<TResponse>(resource, Method.Delete, pswd);
        }

        private async Task<TResponse> Call<TResponse>(string resource, Method method, string? pswd, object? body = null)
        {
            var request = new RestRequest(resource, method);

            // Add authorization header
            var bearer = _descopeConfig.ProjectId;
            if (!string.IsNullOrEmpty(pswd)) bearer = $"{bearer}:{pswd}";
            request.AddHeader("Authorization", "Bearer " + bearer);

            if (body != null)
            {
                var jsonBody = JsonSerializer.Serialize(body);
                request.AddJsonBody(jsonBody);
            }

            var response = await _client.ExecuteAsync<TResponse>(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string cnt = response.Content ?? "{}";
                return JsonSerializer.Deserialize<TResponse>(cnt) ?? throw new DescopeException("Unable to parse response");
            }
            else
            {
                var ed = JsonSerializer.Deserialize<ErrorDetails>(response.Content ?? "{}");
                throw (ed != null) ? new DescopeException(ed) : new DescopeException("Unexpected server error");
            }
        }
    }
}
