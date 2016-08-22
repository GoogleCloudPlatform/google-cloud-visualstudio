using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.GCloud;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GoogleCloudExtension.TerminalServer
{
    internal class TerminalServerManager
    {
        public static void OpenSession(Instance instance, WindowsInstanceCredentials credentials)
        {
            var rdpPath = CreateRdpFile(instance, credentials);
            Debug.WriteLine($"Saved .rdp file at {rdpPath}");
            Process.Start("mstsc", rdpPath);
        }

        private static string CreateRdpFile(Instance instance, WindowsInstanceCredentials credentials)
        {
            var instanceRootPath = WindowsCredentialsStore.Default.GetStoragePathForInstance(instance);
            var rdpPath = Path.Combine(instanceRootPath, GetRdpFileName(credentials));

            WriteRdpFile(rdpPath, instance, credentials);
            return rdpPath;
        }

        public static void WriteRdpFile(string path, Instance instance, WindowsInstanceCredentials credentials)
        {
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine($"full address:s:{instance.GetPublicIpAddress()}");
                writer.WriteLine($"username:s:{credentials.User}");
                writer.WriteLine($"password 51:b:{SerializePassword(credentials.Password)}");
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

        private static string GetRdpFileName(WindowsInstanceCredentials credentials) => $"{credentials.User}.rdp";
    }
}
