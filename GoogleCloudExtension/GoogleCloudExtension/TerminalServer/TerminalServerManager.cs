using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.DataSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GoogleCloudExtension.TerminalServer
{
    internal class TerminalServerManager
    {
        public static async void OpenSession(Instance instance, WindowsInstanceCredentials credentials)
        {
            var properties = new RdpProperties
            {
                Address = instance.GetPublicIpAddress(),
                Credentials = credentials,
            };

            var path = Path.Combine(Path.GetTempPath(), $"{Path.GetRandomFileName()}.rdp");
            properties.Serialize(path);
            Debug.WriteLine($"Saved session file to {path}");

            var info = Process.Start("mstsc", path);
            await Task.Run(() => info.WaitForExit()).ConfigureAwait(false);

            try
            {
                Debug.WriteLine($"Deleting session file {path}");
                File.Delete(path);
            }
            catch (Exception)
            { }
        }
    }
}
