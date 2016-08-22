using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.GCloud;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.TerminalServer
{
    /// <summary>
    /// This class contains the settings used to generate an .rdp file to start a
    /// Terminal Server session.
    /// </summary>
    internal class RdpProperties
    {
        public Instance Instance { get; set; }

        public WindowsInstanceCredentials Credentials { get; set; }

        public void Serialize(string path)
        {
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine($"full address:s:{Instance.GetPublicIpAddress()}");
                writer.WriteLine($"username:s:{Credentials.User}");
                writer.WriteLine($"password 51:b:{SerializePassword(Credentials.Password)}");
            }
        }

        private static string SerializePassword(string password)
        {
            var output = ProtectedData.Protect(
                Encoding.Unicode.GetBytes(password),
                null,
                DataProtectionScope.CurrentUser);
            return AsHexadecimalString(output);
        }

        private static string AsHexadecimalString(byte[] src)
        {
            StringBuilder result = new StringBuilder();
            foreach (var b in src)
            {
                result.AppendFormat("{0:x2}", b);
            }
            return result.ToString();
        }
    }
}
