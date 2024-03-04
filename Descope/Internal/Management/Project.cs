namespace Descope.Internal.Management
{
    internal class Project : IProject
    {
        private readonly IHttpClient _httpClient;
        private readonly string _managementKey;

        internal Project(IHttpClient httpClient, string managementKey)
        {
            _httpClient = httpClient;
            _managementKey = managementKey;
        }


        public async Task<object> Export()
        {
            return await _httpClient.Post<object>(Routes.ProjectExport, _managementKey, null!);
        }

        public async Task Import(object files)
        {
            if (files == null) throw new DescopeException("files missing");
            await _httpClient.Post<object>(Routes.ProjectImport, _managementKey, files);
        }

        public async Task Rename(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new DescopeException("name missing");
            var request = new { name };
            await _httpClient.Post<object>(Routes.ProjectRename, _managementKey, request);
        }

        public async Task<ProjectCloneResponse> Clone(string name, string tag)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new DescopeException("name missing");
            var request = new { name, tag };
            return await _httpClient.Post<ProjectCloneResponse>(Routes.ProjectClone, _managementKey, request);
        }

        public async Task Delete(string projectId)
        {
            if (string.IsNullOrWhiteSpace(projectId)) throw new DescopeException("projectId missing");
            var config = new DescopeConfig(_httpClient.DescopeConfig)
            {
                ProjectId = projectId
            };
            await new HttpClient(config).Post<object>(Routes.ProjectDelete, _managementKey);
        }
    }
}
