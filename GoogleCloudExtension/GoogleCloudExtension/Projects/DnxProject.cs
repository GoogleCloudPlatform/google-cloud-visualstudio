// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GoogleCloudExtension.Projects
{
    public class DnxProject
    {
        internal DnxProject(string path)
        {
            _path = path;
        }

        private readonly string _path;

        public string Root
        {
            get { return _path; }
        }

        public string Name
        {
            get { return Path.GetFileNameWithoutExtension(_path); }
        }

        private AspNETRuntime _runtime = AspNETRuntime.None;
        private ParsedProjectJson _parsedProject;

        public AspNETRuntime Runtime
        {
            get
            {
                if (_runtime == AspNETRuntime.None)
                {
                    _runtime = GetProjectRuntime();
                }
                return _runtime;
            }
        }

        private IList<AspNETRuntime> _supportedRuntimes;

        public IList<AspNETRuntime> SupportedRuntimes
        {
            get
            {
                if (_supportedRuntimes == null)
                {
                    var parsed = GetParsedProject();
                    _supportedRuntimes = parsed.Frameworks
                        .Select(x => DnxRuntime.GetRuntimeFromName(x.Key))
                        .Where(x => GCloudWrapper.DefaultInstance.ValidateDNXInstallationForRuntime(x))
                        .ToList();
                }
                return _supportedRuntimes;
            }
        }

        // The full name of the webserver dependency.
        private const string KestrelFullName = "Microsoft.AspNet.Server.Kestrel";

        // The file name of the .json file that contains the project definition.
        private const string ProjectJsonFileName = "project.json";

        public bool HasWebServer
        {
            get
            {
                var parsed = GetParsedProject();
                return parsed.Dependencies.ContainsKey(KestrelFullName);
            }
        }

        private ParsedProjectJson GetParsedProject()
        {
            if (_parsedProject == null)
            {
                var jsonPath = Path.Combine(Root, ProjectJsonFileName);
                var jsonContents = File.ReadAllText(jsonPath);
                _parsedProject = JsonConvert.DeserializeObject<ParsedProjectJson>(jsonContents);

                // Ensure default empty dictionaries in case the project.json file does not contain the
                // sections of interest.
                if (_parsedProject.Dependencies == null)
                {
                    _parsedProject.Dependencies = new Dictionary<string, object>();
                }
                if (_parsedProject.Frameworks == null)
                {
                    _parsedProject.Frameworks = new Dictionary<string, object>();
                }
            }
            return _parsedProject;
        }

        private AspNETRuntime GetProjectRuntime()
        {
            var parsed = GetParsedProject();
            bool clrRuntimeTargeted = parsed.Frameworks.ContainsKey(DnxRuntime.ClrFrameworkName);
            bool coreClrRuntimeTargeted = parsed.Frameworks.ContainsKey(DnxRuntime.CoreClrFrameworkName);

            bool hasCoreClrRuntimeInstalled = GCloudWrapper.DefaultInstance.ValidateDNXInstallationForRuntime(AspNETRuntime.CoreCLR);
            bool hasClrRuntimeInstalled = GCloudWrapper.DefaultInstance.ValidateDNXInstallationForRuntime(AspNETRuntime.Mono);

            if (coreClrRuntimeTargeted && hasCoreClrRuntimeInstalled)
            {
                return AspNETRuntime.CoreCLR;
            }
            else if (clrRuntimeTargeted && hasClrRuntimeInstalled)
            {
                return AspNETRuntime.Mono;
            }
            else
            {
                Debug.WriteLine("No known runtime is being targeted.");
                return AspNETRuntime.None;
            }
        }

        internal static bool IsDnxProject(string projectRoot)
        {
            var projectJson = Path.Combine(projectRoot, ProjectJsonFileName);
            return File.Exists(projectJson);
        }
    }
}
