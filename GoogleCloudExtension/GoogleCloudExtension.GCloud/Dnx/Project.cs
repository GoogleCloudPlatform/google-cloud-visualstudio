// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud.Dnx.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GoogleCloudExtension.GCloud.Dnx
{
    public sealed class Project
    {
        /// <summary>
        /// The full name of the webserver dependency.
        /// </summary>
        private const string KestrelFullName = "Microsoft.AspNet.Server.Kestrel";

        /// <summary>
        /// The file name of the .json file that contains the project definition.
        /// </summary>
        private const string ProjectJsonFileName = "project.json";

        private Lazy<DnxRuntime> _runtime;
        private Lazy<ProjectModel> _parsedProject;
        private Lazy<IEnumerable<DnxRuntime>> _supportedRuntimes;

        public string Root { get; private set; }

        public string Name => Path.GetFileNameWithoutExtension(Root);

        public DnxRuntime Runtime => _runtime.Value;

        public IEnumerable<DnxRuntime> SupportedRuntimes => _supportedRuntimes.Value;

        public bool HasWebServer => _parsedProject.Value.Dependencies.ContainsKey(KestrelFullName);

        public Project(string root)
        {
            Root = root;
            _runtime = new Lazy<DnxRuntime>(GetProjectRuntime);
            _supportedRuntimes = new Lazy<IEnumerable<DnxRuntime>>(GetSupportedRuntimes);
            _parsedProject = new Lazy<ProjectModel>(GetParsedProject);
        }

        private ProjectModel GetParsedProject()
        {
            var jsonPath = Path.Combine(Root, ProjectJsonFileName);
            var jsonContents = File.ReadAllText(jsonPath);
            var result = JsonConvert.DeserializeObject<ProjectModel>(jsonContents);

            // Ensure default empty dictionaries in case the project.json file does not contain the
            // sections of interest.
            if (result.Dependencies == null)
            {
                result.Dependencies = new Dictionary<string, object>();
            }
            if (result.Frameworks == null)
            {
                result.Frameworks = new Dictionary<string, object>();
            }
            return result;
        }

        /// <summary>
        /// This list contains the supported runtimes in order of preference, do not change
        /// the order or it will affect the selection process for the images.
        /// </summary>
        private static IList<DnxRuntimeInfo> s_PreferredRuntimes = new List<DnxRuntimeInfo>
        {
            DnxRuntimeInfo.GetRuntimeInfo(DnxRuntime.DnxCore50),
            DnxRuntimeInfo.GetRuntimeInfo(DnxRuntime.Dnx451)
        };

        private DnxRuntime GetProjectRuntime() => s_PreferredRuntimes
            .Where(x => _parsedProject.Value.Frameworks.ContainsKey(x.FrameworkName)
                && DnxEnvironment.ValidateDnxInstallationForRuntime(x.Runtime))
            .Select(x => x.Runtime)
            .FirstOrDefault();

        public static bool IsDnxProject(string projectRoot)
        {
            var projectJson = Path.Combine(projectRoot, ProjectJsonFileName);
            return File.Exists(projectJson);
        }

        private IEnumerable<DnxRuntime> GetSupportedRuntimes() => _parsedProject.Value.Frameworks
            .Select(x => DnxRuntimeInfo.GetRuntimeInfo(x.Key).Runtime)
            .Where(x => x != DnxRuntime.None)
            .Where(x => DnxEnvironment.ValidateDnxInstallationForRuntime(x));
    }
}
