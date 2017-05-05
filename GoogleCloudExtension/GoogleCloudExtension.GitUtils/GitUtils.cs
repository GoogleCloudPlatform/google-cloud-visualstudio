using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GitUtils
{
    public static class GitUtils
    {
        private const string VisualStudio2015Version = "14.0";
        private const string VisualStudio2017Version = "15.0";

        public static IEnumerable<string> GetLocalRepositories(string vsVersion)
        {
            switch (vsVersion)
            {
                case VisualStudio2015Version:
                    return VS14.RegistryHelper.PokeTheRegistryForRepositoryList();
                case VisualStudio2017Version:
                    return VS15.RegistryHelper.PokeTheRegistryForRepositoryList();
                default:
                    throw new NotSupportedException($"Version {vsVersion} is not supported.");
            }
        }
    }
}
