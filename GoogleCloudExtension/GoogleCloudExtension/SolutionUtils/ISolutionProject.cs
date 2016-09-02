namespace GoogleCloudExtension.SolutionUtils
{
    public interface ISolutionProject
    {
        string Name { get; }

        string FullPath { get; }

        string DirectoryPath { get; }

        KnownProjectTypes ProjectType { get; }
    }
}
