// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtension.DnxSupport
{
    /// <summary>
    /// This class contains all of the metadata for a runtime enumeration value.
    /// </summary>
    public class DnxRuntimeInfo
    {
        // The names of the supported runtimes.
        // Clr will be substituted by Mono in the container.
        // CoreClr will be itself.
        public const string Dnx451FrameworkName = "dnx451";
        public const string DnxCore50FrameworkName = "dnxcore50";

        // The display strings for the frameworks
        public const string Dnx451DisplayString = ".NET Desktop CLR";
        public const string DnxCore50DisplayString = ".NET Core CLR";

        /// <summary>
        /// When a new runtime is supported it's metadata should be added to this table so
        /// it can be used in the rest of the code.
        /// </summary>
        private static readonly IList<DnxRuntimeInfo> s_KnownRuntimeInfos = new List<DnxRuntimeInfo>
        {
            new DnxRuntimeInfo(
                runtime: DnxRuntime.Dnx451,
                displayName: Dnx451DisplayString,
                frameworkName: Dnx451FrameworkName),
            new DnxRuntimeInfo(
                runtime: DnxRuntime.DnxCore50,
                displayName: DnxCore50DisplayString,
                frameworkName: DnxCore50FrameworkName),
        };

        /// <summary>
        /// This info is to be returned when referring to runtimes that we don't *yet* know.
        /// </summary>
        private static readonly DnxRuntimeInfo s_UnknownRuntimeInfo = new DnxRuntimeInfo(
            runtime: DnxRuntime.None,
            displayName: "",
            frameworkName: "");

        public DnxRuntime Runtime { get; }

        public string DisplayName { get; }

        public string FrameworkName { get; }

        private DnxRuntimeInfo(DnxRuntime runtime, string displayName, string frameworkName)
        {
            Runtime = runtime;
            DisplayName = displayName;
            FrameworkName = frameworkName;
        }

        public static DnxRuntimeInfo GetRuntimeInfo(DnxRuntime runtime)
        {
            return s_KnownRuntimeInfos.FirstOrDefault(x => x.Runtime == runtime) ?? s_UnknownRuntimeInfo;
        }

        public static DnxRuntimeInfo GetRuntimeInfo(string name)
        {
            return s_KnownRuntimeInfos.FirstOrDefault(x => x.FrameworkName == name) ?? s_UnknownRuntimeInfo;
        }
    }
}
