﻿// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud.Dnx
{
    public static class DnxEnvironment
    {
        public const string DnxVersion = "1.0.0-beta8";

        // The path where the binaries for the particular runtime live.
        //   {0} the runtime name.
        private const string DnxRuntimesBinPathFormat = @".dnx\runtimes\{0}\bin";

        // Names for the runtime to use depending on the runtime.
        //   {0} is the bitness of the os, x86 or x64.
        //   {1} is the version of the runtime.
        private const string Dnx451RuntimeNameFormat = "dnx-clr-win-{0}.{1}";
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

        private static string s_VSInstallPath;

        public static string GetDNXPathForRuntime(DnxRuntime runtime)
        {
            var userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string bitness = Environment.Is64BitProcess ? "x64" : "x86";

            string runtimeNameFormat = null;
            switch (runtime)
            {
                case DnxRuntime.Dnx451:
                    runtimeNameFormat = Dnx451RuntimeNameFormat;
                    break;
                case DnxRuntime.DnxCore50:
                    runtimeNameFormat = DnxCore50RuntimeNameFormat;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            var runtimeName = String.Format(runtimeNameFormat, bitness, DnxVersion);
            var runtimeRelativePath = String.Format(DnxRuntimesBinPathFormat, runtimeName);
            Debug.WriteLine($"Using runtime path: {runtimeRelativePath}");

            return Path.Combine(userDirectory, runtimeRelativePath);
        }

        public static string GetWebToolsPath()
        {
            var vsInstallPath = GetVSInstallPath();
            return Path.Combine(vsInstallPath, WebToolsRelativePath);
        }

        public static bool ValidateDnxInstallationForRuntime(DnxRuntime runtime)
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
