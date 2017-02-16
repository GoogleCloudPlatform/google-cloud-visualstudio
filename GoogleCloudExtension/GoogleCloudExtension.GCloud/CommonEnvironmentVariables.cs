namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// This class contains defintions for the environment variables used the various wrappers.
    /// </summary>
    internal static class CommonEnvironmentVariables
    {
        // This variables is used to force gcloud to use application default credentials when generating
        // the cluster information for the cluster.
        public const string GCloudContainerUseApplicationDefaultCredentialsVariable = "CLOUDSDK_CONTAINER_USE_APPLICATION_DEFAULT_CREDENTIALS";
        public const string TrueValue = "true";

        // This variable is used to override the location of the application default credentials
        // with the current user's credentials.
        public const string GoogleApplicationCredentialsVariable = "GOOGLE_APPLICATION_CREDENTIALS";

        // This variable contains the path to the configuration to be used for kubernetes operations.
        public const string GCloudKubeConfigVariable = "KUBECONFIG";
    }
}
