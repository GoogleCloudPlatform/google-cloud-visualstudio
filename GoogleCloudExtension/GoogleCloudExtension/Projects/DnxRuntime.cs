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

        public static string GetRuntimeName(AspNetRuntime runtime)
        {
            switch (runtime)
            {
                case AspNetRuntime.Mono:
                    return "dnx451";
                case AspNetRuntime.CoreCLR:
                    return "dnxcore50";
                default:
                    return "";
            }
        }

        public static string GetRuntimeDisplayName(AspNetRuntime runtime)
        {
            switch (runtime)
            {
                case AspNetRuntime.Mono:
                    return ".NET Desktop CLR";
                case AspNetRuntime.CoreCLR:
                    return ".NET Core CLR";
                default:
                    return "";
            }
        }

        public static AspNetRuntime GetRuntimeFromName(string name)
        {
            switch (name)
            {
                case "dnx451":
                    return AspNetRuntime.Mono;
                case "dnxcore50":
                    return AspNetRuntime.CoreCLR;
                default:
                    return AspNetRuntime.None;
            }
        }
    }
}
