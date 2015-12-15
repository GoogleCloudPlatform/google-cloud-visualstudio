// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GoogleCloudExtension.GCloud.Dnx
{
    /// <summary>
    /// This class contains the functionality to manage the DNX environment and get
    /// information about it.
    /// </summary>
    public static class DnxEnvironment
    {
        public const string DnxVersion = "1.0.0-rc1-update1";

        // The path where the binaries for the particular runtime live.
        //   {0} the runtime name.
        private const string DnxRuntimesBinPathFormat = @".dnx\runtimes\{0}\bin";

        // Names for the runtime to use depending on the runtime.
        //   {0} is the bitness of the os, x86 or x64.
        //   {1} is the version of the runtime.
        private const string DnxCore50RuntimeNameFormat = "dnx-coreclr-win-{0}.{1}";

        private static readonly List<string> s_VSKeysToCheck = new List<string>
        {
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\14.0",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\14.0",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VWDExpress\14.0",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VWDExpress\14.0",
        };

        private const string WebToolsRelativePath = @"Extensions\Microsoft\Web Tools\External";

        private const string InstallDirValue = "InstallDir";

        private static readonly Lazy<string> s_VSInstallPath = new Lazy<string>(CalculateVSInstallPath);

        /// <summary>
        /// Determines the path to the given DNX runtime.
        /// </summary>
        /// <param name="runtime"></param>
        /// <returns></returns>
        public static string GetDnxPath()
        {
            var userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string bitness = Environment.Is64BitProcess ? "x64" : "x86";

            var runtimeName = String.Format(DnxCore50RuntimeNameFormat, bitness, DnxVersion);
            var runtimeRelativePath = String.Format(DnxRuntimesBinPathFormat, runtimeName);
            Debug.WriteLine($"Using runtime path: {runtimeRelativePath}");

            return Path.Combine(userDirectory, runtimeRelativePath);
        }

        public static string GetWebToolsPath()
        {
            return Path.Combine(s_VSInstallPath.Value, WebToolsRelativePath);
        }

        public static bool ValidateDnxInstallationForRuntime()
        {
            bool result = false;
            Debug.WriteLine("Validating DNX installation.");
            var dnxDirectory = GetDnxPath();
            var dnuPath = Path.Combine(dnxDirectory, "dnu.cmd");

            result = File.Exists(dnuPath);
            Debug.WriteLineIf(!result, $"DNX runtime not installed, cannot find {dnuPath}");
            return result;
        }

        private static string CalculateVSInstallPath()
        {
            return s_VSKeysToCheck
                .Select(x => Registry.GetValue(x, InstallDirValue, null) as string)
                .FirstOrDefault(x => x != null);
        }
    }
}
