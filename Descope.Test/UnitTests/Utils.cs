using Descope.Internal;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Descope.Test.Unit
{
    internal class Utils
    {
        public static string Serialize(object o)
        {
            return JsonSerializer.Serialize(o);
        }

        public static T Convert<T>(object? o)
        {
            var s = JsonSerializer.Serialize(o ?? "{}");
            var d = JsonSerializer.Deserialize<T>(s);
            return d ?? throw new Exception("Conversion error");
        }
    }

    internal class MockHttpClient : IHttpClient
    {

        // Delete
        public bool DeleteFailure { get; set; }
        public Exception? DeleteError { get; set; }
        public int DeleteCount { get; set; }
        public Func<string, string?, Dictionary<string, string?>?, object?>? DeleteAssert { get; set; }
        public object? DeleteResponse { get; set; }

        // Get
        public bool GetFailure { get; set; }
        public Exception? GetError { get; set; }
        public int GetCount { get; set; }
        public Func<string, string?, Dictionary<string, string?>?, object?>? GetAssert { get; set; }
        public object? GetResponse { get; set; }

        // Post
        public bool PostFailure { get; set; }
        public Exception? PostError { get; set; }
        public int PostCount { get; set; }
        public Func<string, string?, object?, Dictionary<string, string?>?, object?>? PostAssert { get; set; }
        public object? PostResponse { get; set; }

        // Patch
        public bool PatchFailure { get; set; }
        public Exception? PatchError { get; set; }
        public int PatchCount { get; set; }
        public Func<string, string?, object?, Dictionary<string, string?>?, object?>? PatchAssert { get; set; }
        public object? PatchResponse { get; set; }

        // IHttpClient Properties
        public DescopeConfig DescopeConfig { get; set; }

        public MockHttpClient()
        {
            DescopeConfig = new DescopeConfig(projectId: "test");
        }

        // IHttpClient Implementation

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        public async Task<TResponse> Delete<TResponse>(string resource, string pswd, Dictionary<string, string?>? queryParams = null)
        {
            DeleteCount++;
            DeleteAssert?.Invoke(resource, pswd, queryParams);
            if (DeleteError != null) throw DeleteError;
            if (DeleteFailure) throw new Exception();
            return Utils.Convert<TResponse>(DeleteResponse);
        }

        public async Task<TResponse> Get<TResponse>(string resource, string? pswd = null, Dictionary<string, string?>? queryParams = null)
        {
            GetCount++;
            GetAssert?.Invoke(resource, pswd, queryParams);
            if (GetError != null) throw GetError;
            if (GetFailure) throw new Exception();
            return Utils.Convert<TResponse>(GetResponse);
        }


        public async Task<TResponse> Post<TResponse>(string resource, string? pswd = null, object? body = null, Dictionary<string, string?>? queryParams = null)
        {
            PostCount++;
            PostAssert?.Invoke(resource, pswd, body, queryParams);
            if (PostError != null) throw PostError;
            if (PostFailure) throw new Exception();
            return Utils.Convert<TResponse>(PostResponse);
        }

        public async Task<TResponse> Patch<TResponse>(string resource, string? pswd = null, object? body = null, Dictionary<string, string?>? queryParams = null)
        {
            PatchCount++;
            PatchAssert?.Invoke(resource, pswd, body, queryParams);
            if (PatchError != null) throw PatchError;
            if (PatchFailure) throw new Exception();
            return Utils.Convert<TResponse>(PatchResponse);
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    }
}
