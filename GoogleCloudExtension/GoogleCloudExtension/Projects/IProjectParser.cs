using EnvDTE;
using GoogleCloudExtension.Deployment;

namespace GoogleCloudExtension.Projects {
    internal interface IProjectParser {
        /// <summary>
        /// Parses the given <seealso cref="Project"/> instance and resturns a friendlier and more usable type to use for
        /// deployment and other operations.
        /// </summary>
        /// <param name="project">The <seealso cref="Project"/> instance to parse.</param>
        /// <returns>The resulting <seealso cref="IParsedProject"/> or null if the project is not supported.</returns>
        IParsedProject ParseProject(Project project);
    }
}