namespace Descope.Internal.Management
{
    internal class Management : IManagement
    {
        public ITenant Tenant => _tenant;
        public IUser User => _user;
        public IAccessKey AccessKey => _accessKey;
        public ISso Sso => _sso;
        public ISsoApplication SsoApplication => _ssoApplication;
        public IPasswordSettings Password => _password;
        public IJwt Jwt => _jwt;
        public IPermission Permission => _permission;
        public IRole Role => _role;
        public IProject Project => _project;

        private readonly Tenant _tenant;
        private readonly User _user;
        private readonly AccessKey _accessKey;
        private readonly Sso _sso;
        private readonly SsoApplication _ssoApplication;
        private readonly Password _password;
        private readonly Jwt _jwt;
        private readonly Permission _permission;
        private readonly Role _role;
        private readonly Project _project;

        public Management(IHttpClient client, string managementKey)
        {
            _tenant = new Tenant(client, managementKey);
            _user = new User(client, managementKey);
            _accessKey = new AccessKey(client, managementKey);
            _sso = new Sso(client, managementKey);
            _ssoApplication = new SsoApplication(client, managementKey);
            _password = new Password(client, managementKey);
            _jwt = new Jwt(client, managementKey);
            _permission = new Permission(client, managementKey);
            _role = new Role(client, managementKey);
            _project = new Project(client, managementKey);
        }
    }
}
