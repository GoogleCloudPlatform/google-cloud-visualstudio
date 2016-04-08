using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Accounts.Models
{
    public class UserAccount
    {
        [JsonProperty("account")]
        public string AccountName { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        internal static UserAccount FromFile(string path)
        {
            var contents = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<UserAccount>(contents);
        }

        internal void Save(string path)
        {
            var serialized = JsonConvert.SerializeObject(this);
            var name = GetName(serialized) + ".json";
            File.WriteAllText(Path.Combine(path, name), serialized);
        }

        /// <summary>
        /// Generates a unique name for the user credentials based on the hash of the contents
        /// which guarantees a safe and unique name for the credentials file.
        /// </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        private static string GetName(string serialized)
        {
            var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(serialized));

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.AppendFormat("{0:2x}", b);
            }
            return sb.ToString();
        }
    }
}
