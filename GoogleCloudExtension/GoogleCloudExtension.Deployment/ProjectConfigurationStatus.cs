namespace GoogleCloudExtension.Deployment
{
    public class ProjectConfigurationStatus
    {
        public bool HasAppYaml { get; }

        public bool HasDockerfile { get; }

        public ProjectConfigurationStatus(bool hasAppYaml, bool hasDockerfile)
        {
            HasAppYaml = hasAppYaml;
            HasDockerfile = hasDockerfile;
        }
    }
}
