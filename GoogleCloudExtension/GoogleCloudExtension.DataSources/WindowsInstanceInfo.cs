namespace GoogleCloudExtension.DataSources
{
    public class WindowsInstanceInfo
    {
        public string DisplayName { get; }

        public string Version { get; }

        public string SubVersion { get; }

        public WindowsInstanceInfo(string displayName, string version, string subVersion)
        {
            DisplayName = displayName;
            Version = version;
            SubVersion = subVersion;
        }
    }
}
