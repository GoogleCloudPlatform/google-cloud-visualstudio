using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources.Models
{
    public class WindowsCredentials
    {
        public string User { get; }

        public string Password { get; }

        public WindowsCredentials(string user, string password)
        {
            User = user;
            Password = password;
        }
    }
}
