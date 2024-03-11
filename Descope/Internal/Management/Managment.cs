namespace Descope.Internal.Management
{
    internal class Management : IManagement
    {
        public ITenant Tenant => _tenant;
        public IUser User => _user;
        public IAccessKey AccessKey => _accessKey;
        public IPermission Permission => _permission;
        public IRole Role => _role;
        public IProject Project => _project;

        private readonly Tenant _tenant;
        private readonly User _user;
        private readonly AccessKey _accessKey;
        private readonly Permission _permission;
        private readonly Role _role;
        private readonly Project _project;

        public Management(IHttpClient client, string managementKey)
        {
            _tenant = new Tenant(client, managementKey);
            _user = new User(client, managementKey);
            _accessKey = new AccessKey(client, managementKey);
            _permission = new Permission(client, managementKey);
            _role = new Role(client, managementKey);
            _project = new Project(client, managementKey);
        }
    }
}
