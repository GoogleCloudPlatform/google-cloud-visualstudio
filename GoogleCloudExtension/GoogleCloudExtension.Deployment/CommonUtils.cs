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

using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// This class contains common utils methods shared between the deployments.
    /// </summary>
    public static class CommonUtils
    {
        // This pattern is to be used to find all of  the .deps.json in the stage directory.
        private const string DepsFilePattern = "*.deps.json";

        // This is the extension for the .deps.json file that determines the name of the entrypoint assembly.
        private const string DepsFileExtension = ".deps.json";

        /// <summary>
        /// Returns the name of the entrypoint assembly for the .NET Core project given it's stage directory. The name
        /// is determined by looking for the .deps.json that defines the app's structure.
        /// </summary>
        /// <param name="stageDirectory">The directory where the app is being staged.</param>
        public static string GetEntrypointName(string stageDirectory)
        {
            var depsFile = Directory.GetFiles(stageDirectory, DepsFilePattern).FirstOrDefault();
            if (depsFile == null)
            {
                Debug.WriteLine($"Cannot find .deps.file in {stageDirectory}");
                return null;
            }

            var name = Path.GetFileName(depsFile);
            return name.Substring(0, name.Length - DepsFileExtension.Length);
        }

        /// <summary>
        /// Deletes the given directory in a safe way.
        /// </summary>
        /// <param name="dir">The path to the directory to delete.</param>
        public static void Cleanup(string dir)
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
