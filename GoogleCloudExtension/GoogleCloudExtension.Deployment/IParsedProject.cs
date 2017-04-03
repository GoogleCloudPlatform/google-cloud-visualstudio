using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    public interface IParsedProject
    {
        /// <summary>
        /// The name of the project.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The full path to the project, including the project file.
        /// </summary>
        string FullPath { get; }

        /// <summary>
        /// The full path to the directory that contains the project file.
        /// </summary>
        string DirectoryPath { get; }

        /// <summary>
        /// The type of the project.
        /// </summary>
        KnownProjectTypes ProjectType { get; }
    }
}
