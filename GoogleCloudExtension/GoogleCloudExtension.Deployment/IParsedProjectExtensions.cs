using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// Useful checks for projects.
    /// </summary>
    public static class IParsedProjectExtensions
    {
        /// <summary>
        /// Determines if the given project is a .NET Core project or not.
        /// </summary>
        /// <param name="project">The project to check.</param>
        /// <returns>Returns true if the project is a .NET Core project, false otherwise.</returns>
        public static bool IsAspNetCoreProject(this IParsedProject project)
            => project.ProjectType == KnownProjectTypes.NetCoreWebApplication1_0 ||
               project.ProjectType == KnownProjectTypes.NetCoreWebApplication1_1 ||
               project.ProjectType == KnownProjectTypes.NetCoreWebApplication2_0;
    }
}
