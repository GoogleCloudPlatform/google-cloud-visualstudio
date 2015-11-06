// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// Supported ASP.NET runtimes.
    /// </summary>
    public enum AspNetRuntime
    {
        None,
        Mono,
        CoreClr
    }

    public static class DnxEnvironment
    {
        public const string DnxVersion = "1.0.0-beta8";

        // Docker images names for the runtime.
        public const string MonoImageName = "mono";
        public const string CoreClrImageName = "coreclr";

        // The names of the supported runtimes.
        // Clr will be substituted by Mono in the container.
        // CoreClr will be itself.
        public const string ClrFrameworkName = "dnx451";
        public const string CoreClrFrameworkName = "dnxcore50";

        // The path where the binaries for the particular runtime live.
        //   {0} the runtime name.
        private const string DnxRuntimesBinPathFormat = @".dnx\runtimes\{0}\bin";

        // Names for the runtime to use depending on the runtime.
        //   {0} is the bitness of the os, x86 or x64.
        //   {1} is the version of the runtime.
        private const string DnxClrRuntimeNameFormat = "dnx-clr-win-{0}.{1}";
        private const string DnxCoreClrRuntimeNameFormat = "dnx-coreclr-win-{0}.{1}";

        private static readonly List<string> s_VSKeysToCheck = new List<string>
        {
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\14.0",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\14.0",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VWDExpress\14.0",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VWDExpress\14.0",
        };

        private const string WebToolsRelativePath = @"Extensions\Microsoft\Web Tools\External";

        private const string InstallDirValue = "InstallDir";

        private static string s_VSInstallPath;

        public static string GetDNXPathForRuntime(AspNetRuntime runtime)
        {
            var userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string bitness = Environment.Is64BitProcess ? "x64" : "x86";

            string runtimeNameFormat = null;
            switch (runtime)
            {
                case AspNetRuntime.Mono:
                    runtimeNameFormat = DnxClrRuntimeNameFormat;
                    break;
                case AspNetRuntime.CoreClr:
                    runtimeNameFormat = DnxCoreClrRuntimeNameFormat;
                    break;
                default:
                    Debug.Assert(false, "Should not get here.");
                    break;
            }

            var runtimeName = String.Format(runtimeNameFormat, bitness, DnxVersion);
            var runtimeRelativePath = String.Format(DnxRuntimesBinPathFormat, runtimeName);
            Debug.WriteLine($"Using runtime path: {runtimeRelativePath}");

            return Path.Combine(userDirectory, runtimeRelativePath);
        }

        public static string GetDnxFrameworkNameFromRuntime(AspNetRuntime runtime)
        {
            switch (runtime)
            {
                case AspNetRuntime.Mono:
                    return ClrFrameworkName;
                case AspNetRuntime.CoreClr:
                    return CoreClrFrameworkName;
                default:
                    return "none";
            }
        }

        public static string GetRuntimeDisplayName(AspNetRuntime runtime)
        {
            switch (runtime)
            {
                case AspNetRuntime.Mono:
                    return ".NET Desktop CLR";
                case AspNetRuntime.CoreClr:
                    return ".NET Core CLR";
                default:
                    return "";
            }
        }

        public static string GetImageNameFromRuntime(AspNetRuntime runtime)
        {
            switch (runtime)
            {
                case AspNetRuntime.Mono:
                    return MonoImageName;
                case AspNetRuntime.CoreClr:
                    return CoreClrImageName;
                default:
                    return "";
            }
        }

        public static AspNetRuntime GetRuntimeFromName(string name)
        {
            switch (name)
            {
                case ClrFrameworkName:
                    return AspNetRuntime.Mono;
                case CoreClrFrameworkName:
                    return AspNetRuntime.CoreClr;
                default:
                    return AspNetRuntime.None;
            }
        }

        public static string GetWebToolsPath()
        {
            var vsInstallPath = GetVSInstallPath();
            return Path.Combine(vsInstallPath, WebToolsRelativePath);
        }

        public static bool ValidateDnxInstallationForRuntime(AspNetRuntime runtime)
        {
            bool result = false;
            Debug.WriteLine("Validating DNX installation.");
            var dnxDirectory = GetDNXPathForRuntime(runtime);
            var dnuPath = Path.Combine(dnxDirectory, "dnu.cmd");

            result = File.Exists(dnuPath);
            Debug.WriteLineIf(!result, $"DNX runtime {runtime} not installed, cannot find {dnuPath}");
            return result;
        }

        private static string GetVSInstallPath()
        {
            if (s_VSInstallPath == null)
            {
                foreach (var key in s_VSKeysToCheck)
                {
                    var value = (string)Registry.GetValue(key, InstallDirValue, null);
                    if (value != null)
                    {
                        s_VSInstallPath = value;
                        break;
                    }
                }
            }
            return s_VSInstallPath;
        }
    }
}
