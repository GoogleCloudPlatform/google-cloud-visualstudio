using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.ManageWindowsCredentials
{
    internal class WindowsCredentialsStore
    {
        private const string WindowsCredentialsPath = @"googlecloudvsextension\windows_credentials";

        private static readonly Lazy<WindowsCredentialsStore> s_defaultStore = new Lazy<WindowsCredentialsStore>();
        private static readonly string s_credentialsStoreRoot = GetCredentialsStoreRoot();

        /// <summary>
        /// In memory cache of the credentials for the current credentials (account and project pair).
        /// </summary>
        private readonly Dictionary<string, IEnumerable<WindowsCredentials>> _credentialsForInstance = new Dictionary<string, IEnumerable<WindowsCredentials>>();

        public static WindowsCredentialsStore Default => s_defaultStore.Value;

        public IEnumerable<WindowsCredentials> GetCredentialsForInstance(Instance instance)
        {
            var instancePath = GetInstancePath(instance);
            IEnumerable<WindowsCredentials> result;
            if (_credentialsForInstance.TryGetValue(instancePath, out result))
            {
                return result;
            }

            var fullInstancePath = Path.Combine(s_credentialsStoreRoot, instancePath);
            if (!Directory.Exists(fullInstancePath))
            {
                result = Enumerable.Empty<WindowsCredentials>();
            }
            else
            {
                result = Directory.EnumerateFiles(fullInstancePath)
                    .Where(x => Path.GetExtension(x) == ".json")
                    .Select(x => LoadCredentials(x))
                    .OrderBy(x => x.UserName);
            }
            _credentialsForInstance[instancePath] = result;

            return result;
        }

        public void AddCredentialsToInstance(Instance instance, WindowsCredentials credentials)
        {
            var instancePath = GetInstancePath(instance);
            var fullInstancePath = Path.Combine(s_credentialsStoreRoot, instancePath);

            SaveCredentials(fullInstancePath, credentials);
            _credentialsForInstance.Remove(instancePath);
        }

        public void DeleteCredentialsForInstance(Instance instance, WindowsCredentials credentials)
        {
            var instancePath = GetInstancePath(instance);
            var fullInstancePath = Path.Combine(s_credentialsStoreRoot, instancePath);
            var credentialsPath = Path.Combine(fullInstancePath, GetFileName(credentials));

            if (File.Exists(credentialsPath))
            {
                File.Delete(credentialsPath);
                _credentialsForInstance.Remove(instancePath);
            }
        }

        private WindowsCredentials LoadCredentials(string path)
        {
            var content = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<WindowsCredentials>(content);
        }

        private void SaveCredentials(string path, WindowsCredentials credentials)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var filePath = Path.Combine(path, GetFileName(credentials));
            var content = JsonConvert.SerializeObject(credentials);
            File.WriteAllText(filePath, content);
        }

        private static string GetCredentialsStoreRoot()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, WindowsCredentialsPath);
        }

        private static string GetInstancePath(Instance instance)
        {
            var credentials = CredentialsStore.Default;
            return $@"{credentials.CurrentProjectId}\{instance.GetZoneName()}\{instance.Name}";
        }

        private static string GetFileName(WindowsCredentials credentials) => $"{credentials.UserName}.json";
    }
}
