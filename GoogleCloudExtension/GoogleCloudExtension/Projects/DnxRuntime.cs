// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;

namespace GoogleCloudExtension.Projects
{
    public static class DnxRuntime
    {
        // The names of the supported runtimes.
        // Clr will be substituted by Mono in the container.
        // CoreClr will be itself.
        public const string ClrFrameworkName = "dnx451";
        public const string CoreClrFrameworkName = "dnxcore50";

        public static string GetRuntimeName(AspNETRuntime runtime)
        {
            switch (runtime)
            {
                case AspNETRuntime.Mono:
                    return "dnx451";
                case AspNETRuntime.CoreCLR:
                    return "dnxcore50";
                default:
                    return "";
            }
        }

        public static string GetRuntimeDisplayName(AspNETRuntime runtime)
        {
            switch (runtime)
            {
                case AspNETRuntime.Mono:
                    return ".NET Desktop CLR";
                case AspNETRuntime.CoreCLR:
                    return ".NET Core CLR";
                default:
                    return "";
            }
        }

        public static AspNETRuntime GetRuntimeFromName(string name)
        {
            switch (name)
            {
                case "dnx451":
                    return AspNETRuntime.Mono;
                case "dnxcore50":
                    return AspNETRuntime.CoreCLR;
                default:
                    return AspNETRuntime.None;
            }
        }
    }
}
