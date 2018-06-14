using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Projects;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    public interface IWindowsVmDeployment
    {
        /// <summary>
        /// Publishes an ASP.NET 4.x project to the given GCE <seealso cref="Instance"/>.
        /// </summary>
        /// <param name="project">The project to deploy.</param>
        /// <param name="targetInstance">The instance to deploy.</param>
        /// <param name="credentials">The Windows credentials to use to deploy to the <paramref name="targetInstance"/>.</param>
        /// <param name="targetDeployPath"></param>
        Task<bool> PublishProjectAsync(
            IParsedDteProject project,
            Instance targetInstance,
            WindowsInstanceCredentials credentials,
            string targetDeployPath);
    }
}