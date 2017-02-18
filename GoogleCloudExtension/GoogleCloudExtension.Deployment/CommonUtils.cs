// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// This class contains common utils methods shared between the deployments.
    /// </summary>
    internal static class CommonUtils
    {
        /// <summary>
        /// Returns the project name given the path to the project.json. If the project.json file defines a
        /// "name" property then it is used as the name for the final assembly, otherwise the name of the directory
        /// is used as the name of the final assembly.
        /// </summary>
        /// <param name="projectPath">The full path to the project.json of the project.</param>
        internal static string GetProjectName(string projectPath)
        {
            try
            {
                var contents = File.ReadAllText(projectPath);
                var parsed = JsonConvert.DeserializeObject<Dictionary<string, object>>(contents);
                object name = null;
                if (parsed.TryGetValue("name", out name))
                {
                    return (string)name;
                }
                else
                {
                    var directory = Path.GetDirectoryName(projectPath);
                    return Path.GetFileName(directory);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException)
            {
                throw new DeploymentException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Deletes the given directory in a safe way.
        /// </summary>
        /// <param name="dir">The path to the directory to delete.</param>
        internal static void Cleanup(string dir)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Failed to cleanup: {ex.Message}");
            }
        }
    }
}
