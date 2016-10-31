using System;

namespace GoogleCloudExtension.AddWindowsCredential
{
    public class CredentialsRequest
    {
        public string Password { get; private set; }

        public string User { get; private set; }

        public bool GeneratePassword => String.IsNullOrEmpty(Password);


        public CredentialsRequest(string user, string password = null)
        {
            Password = password;
            User = user;
        }
    }
}
