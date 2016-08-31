using EnvDTE;
using System.Linq;
using System.Xml.Linq;

namespace GoogleCloudExtension.Utils
{
    internal enum KnownProjectTypes
    {
        None,
        WebApplication,
    }

    internal static class ProjectUtils
    {
        private const string MsbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";
        private const string WebApplicationGuid = "{349c5851-65df-11da-9384-00065b846f21}";

        public static KnownProjectTypes GetProjectType(this Project project)
        {
            var dom = XDocument.Load(project.FullName);
            var projectGuids = dom.Root
                .Elements(XName.Get("PropertyGroup", MsbuildNamespace))
                .Descendants(XName.Get("ProjectTypeGuids", MsbuildNamespace))
                .Select(x => x.Value)
                .FirstOrDefault();

            if (projectGuids == null)
            {
                return KnownProjectTypes.None;
            }

            var guids = projectGuids.Split(';');
            if (guids.Contains(WebApplicationGuid))
            {
                return KnownProjectTypes.WebApplication;
            }
            return KnownProjectTypes.None;
        }
    }
}
