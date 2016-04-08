using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Accounts.Models
{
    internal class StoredUserAccount
    {
        public string FileName { get; }

        public UserAccount UserAccount { get; }

        public StoredUserAccount(string fileName, UserAccount userAccount)
        {
            FileName = fileName;
            UserAccount = userAccount;
        }
    }
}
