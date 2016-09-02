using System.IO;

namespace GoogleCloudExtension.SolutionUtils
{
    /// <summary>
    /// This class represetns a .NET Core project.
    /// </summary>
    internal class NetCoreProject : ISolutionProject
    {
        private readonly string _projectJsonPath;

        public string DirectoryPath => Path.GetDirectoryName(_projectJsonPath);

        public string FullPath => _projectJsonPath;

        public string Name => Path.GetFileName(Path.GetDirectoryName(_projectJsonPath));

        public KnownProjectTypes ProjectType => KnownProjectTypes.NetCoreWebApplication;

        public NetCoreProject(string projectJsonPath)
        {
            _projectJsonPath = projectJsonPath;
        }
    }
}
