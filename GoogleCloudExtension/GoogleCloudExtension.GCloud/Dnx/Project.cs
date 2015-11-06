// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Dnx.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GoogleCloudExtension.GCloud.Dnx
{
    public sealed class Project
    {
        public Project(string path)
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

        private DnxRuntime _runtime = DnxRuntime.None;
        private ProjectModel _parsedProject;

        public DnxRuntime Runtime
        {
            get
            {
                if (_runtime == DnxRuntime.None)
                {
                    _runtime = GetProjectRuntime();
                }
                return _runtime;
            }
        }

        private IList<DnxRuntime> _supportedRuntimes;

        public IList<DnxRuntime> SupportedRuntimes
        {
            get
            {
                if (_supportedRuntimes == null)
                {
                    var parsed = GetParsedProject();
                    _supportedRuntimes = parsed.Frameworks
                        .Select(x => DnxRuntimeInfo.GetRuntimeInfo(x.Key).Runtime)
                        .Where(x => DnxEnvironment.ValidateDnxInstallationForRuntime(x))
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

        private ProjectModel GetParsedProject()
        {
            if (_parsedProject == null)
            {
                var jsonPath = Path.Combine(Root, ProjectJsonFileName);
                var jsonContents = File.ReadAllText(jsonPath);
                _parsedProject = JsonConvert.DeserializeObject<ProjectModel>(jsonContents);

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

        private DnxRuntime GetProjectRuntime()
        {
            var parsed = GetParsedProject();
            bool clrRuntimeTargeted = parsed.Frameworks.ContainsKey(DnxRuntimeInfo.Dnx451FrameworkName);
            bool coreClrRuntimeTargeted = parsed.Frameworks.ContainsKey(DnxRuntimeInfo.DnxCore50FrameworkName);

            bool hasCoreClrRuntimeInstalled = DnxEnvironment.ValidateDnxInstallationForRuntime(DnxRuntime.DnxCore50);
            bool hasClrRuntimeInstalled = DnxEnvironment.ValidateDnxInstallationForRuntime(DnxRuntime.Dnx451);

            if (coreClrRuntimeTargeted && hasCoreClrRuntimeInstalled)
            {
                return DnxRuntime.DnxCore50;
            }
            else if (clrRuntimeTargeted && hasClrRuntimeInstalled)
            {
                return DnxRuntime.Dnx451;
            }
            else
            {
                Debug.WriteLine("No known runtime is being targeted.");
                return DnxRuntime.None;
            }
        }

        public static bool IsDnxProject(string projectRoot)
        {
            var projectJson = Path.Combine(projectRoot, ProjectJsonFileName);
            return File.Exists(projectJson);
        }
    }
}
