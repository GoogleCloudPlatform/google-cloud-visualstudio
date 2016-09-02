using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
