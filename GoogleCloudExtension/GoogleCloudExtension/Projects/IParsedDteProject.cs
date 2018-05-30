using EnvDTE;
using GoogleCloudExtension.Deployment;

namespace GoogleCloudExtension.Projects
{
    public interface IParsedDteProject : IParsedProject
    {
        Project Project { get; }
    }
}