namespace Descope
{
    public class DescopeConfig
    {
        public string ProjectId { get; set; }
        public string? ManagementKey { get; set; }
        public string? BaseURL { get; set; } = null;
        public bool Unsafe { get; set; } = false;

        public DescopeConfig(string projectId)
        {
            ProjectId = projectId;
        }

        public DescopeConfig(DescopeConfig other)
        {
            ProjectId = other.ProjectId;
            ManagementKey = other.ManagementKey;
            BaseURL = other.BaseURL;
            Unsafe = other.Unsafe;
        }
    }
}
