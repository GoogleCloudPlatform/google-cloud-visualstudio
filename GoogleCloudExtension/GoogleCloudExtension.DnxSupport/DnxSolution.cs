// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud.Dnx.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GoogleCloudExtension.DnxSupport
{
    /// <summary>
    /// This class represents a Dnx solution and understands the conventions used to be able
    /// to locate the projects within it.
    /// </summary>
    public class DnxSolution
    {
        private const string GlobalJsonName = "global.json";

        public string Root { get; private set; }

        public DnxSolution(string root)
        {
            Root = root;
        }

        public DnxProject GetProjectFromName(string name)
        {
            var solutionDirectory = Path.GetDirectoryName(Root);
            var projectDirectory = Path.GetDirectoryName(name);
            var projectPath = Path.Combine(solutionDirectory, projectDirectory);

            // Only return a DnxProject if the project is indeed a DnxProject otherwise just return
            // null. This is for the case when a Non-Dnx project is opened in VS.
            if (DnxProject.IsDnxProject(projectPath))
            {
                return new DnxProject(projectPath);
            }
            else
            {
                return null;
            }
        }

        public IList<DnxProject> GetProjects()
        {
            List<DnxProject> result = new List<DnxProject>();

            var solutionDirectory = Path.GetDirectoryName(Root);
            var globalJsonPath = Path.Combine(solutionDirectory, GlobalJsonName);
            if (File.Exists(globalJsonPath))
            {
                try
                {
                    var globalJsonContents = File.ReadAllText(globalJsonPath);
                    var globalJson = JsonConvert.DeserializeObject<GlobalModel>(globalJsonContents);
                    foreach (var dir in globalJson.Projects)
                    {
                        FindProjects(Path.Combine(solutionDirectory, dir), result);
                    }
                }
                catch (JsonException)
                {
                    return result;
                }
            }
            else
            {
                // If no global.json file is found then we attempt at looking for projects in the
                // solution directory, this can happen if a solution was created from loose project.json files.
                Debug.WriteLine("No global.json found.");
                FindProjects(solutionDirectory, result);
            }
            return result;
        }

        private void FindProjects(string projectContainer, List<DnxProject> result)
        {
            if (!Directory.Exists(projectContainer))
            {
                Debug.WriteLine($"The project container {projectContainer} doesn't exist.");
                return;
            }

            var possibleProjects = Directory.GetDirectories(projectContainer);
            foreach (var project in possibleProjects)
            {
                if (!DnxProject.IsDnxProject(project))
                {
                    continue;
                }
                Debug.WriteLine($"Found project: {project}");
                result.Add(new DnxProject(project));
            }
        }
    }
}
