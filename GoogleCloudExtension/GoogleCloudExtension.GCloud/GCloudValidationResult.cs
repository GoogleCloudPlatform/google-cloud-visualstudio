using System;

namespace GoogleCloudExtension.GCloud
{
    public class GCloudValidationResult
    {
        public Version CloudSdkVersion { get; }

        public bool IsCloudSdkInstalled { get; }

        public bool IsCloudSdkUpdated { get; }

        public bool IsRequiredComponentInstalled { get; }

        public bool IsValid => IsCloudSdkInstalled && IsCloudSdkUpdated && IsRequiredComponentInstalled;

        public GCloudValidationResult(
            bool isCloudSdkInstalled = false,
            bool isCloudSdkUpdated = false,
            bool isRequiredComponentInstalled = false,
            Version cloudSdkVersion = null)
        {
            IsCloudSdkInstalled = isCloudSdkInstalled;
            IsCloudSdkUpdated = isCloudSdkUpdated;
            IsRequiredComponentInstalled = isRequiredComponentInstalled;
            CloudSdkVersion = cloudSdkVersion;
        }
    }
}