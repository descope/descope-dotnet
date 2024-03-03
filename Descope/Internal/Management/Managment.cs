namespace Descope.Internal.Management
{
    internal class Management : IManagement
    {
        public ITenant Tenant => _tenant;
        public IUser User => _user;
        public IAccessKey AccessKey => _accessKey;
        public IProject Project => _project;

        private readonly Tenant _tenant;
        private readonly User _user;
        private readonly AccessKey _accessKey;
        private readonly Project _project;

        public Management(IHttpClient client, string managementKey)
        {
            _tenant = new Tenant(client, managementKey);
            _user = new User(client, managementKey);
            _accessKey = new AccessKey(client, managementKey);
            _project = new Project(client, managementKey);
        }
    }
}
