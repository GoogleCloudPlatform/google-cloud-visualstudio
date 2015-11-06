using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud.Dnx
{
    /// <summary>
    /// This class contains all of the metadata for a runtime enumeration value.
    /// </summary>
    public class DnxRuntimeInfo
    {
        // Docker images names for the runtime.
        public const string Dnx451ImageName = "mono";
        public const string DnxCore50ImageName = "coreclr";

        // The names of the supported runtimes.
        // Clr will be substituted by Mono in the container.
        // CoreClr will be itself.
        public const string Dnx451FrameworkName = "dnx451";
        public const string DnxCore50FrameworkName = "dnxcore50";

        // The display strings for the frameworks
        public const string Dnx451DisplayString = ".NET Desktop CLR";
        public const string DnxCore50DisplayString = ".NET Core CLR";

        public DnxRuntime Runtime { get; }

        public string DisplayName { get; }

        public string FrameworkName { get; }

        public string ImageName { get; }

        private DnxRuntimeInfo(DnxRuntime runtime, string displayName, string frameworkName, string imageName)
        {
            Runtime = runtime;
            DisplayName = displayName;
            FrameworkName = frameworkName;
            ImageName = imageName;
        }

        /// <summary>
        /// When a new runtime is supported it's metadata should be added to this table so
        /// it can be used in the rest of the code.
        /// </summary>
        private static IList<DnxRuntimeInfo> s_RegisteredInfos = new List<DnxRuntimeInfo>
        {
            new DnxRuntimeInfo(
                runtime: DnxRuntime.Dnx451,
                displayName: Dnx451DisplayString,
                frameworkName: Dnx451FrameworkName,
                imageName: Dnx451ImageName),
            new DnxRuntimeInfo(
                runtime: DnxRuntime.DnxCore50,
                displayName: DnxCore50DisplayString,
                frameworkName: DnxCore50FrameworkName,
                imageName: DnxCore50ImageName),
        };
        private static DnxRuntimeInfo s_DefaultRuntimeInfo = new DnxRuntimeInfo(
            runtime: DnxRuntime.None,
            displayName: "",
            frameworkName: "",
            imageName: "");

        public static DnxRuntimeInfo GetRuntimeInfo(DnxRuntime runtime)
        {
            return s_RegisteredInfos.FirstOrDefault(x => x.Runtime == runtime) ?? s_DefaultRuntimeInfo;
        }

        public static DnxRuntimeInfo GetRuntimeInfo(string name)
        {
            return s_RegisteredInfos.FirstOrDefault(x => x.FrameworkName == name) ?? s_DefaultRuntimeInfo;
        }
    }
}
