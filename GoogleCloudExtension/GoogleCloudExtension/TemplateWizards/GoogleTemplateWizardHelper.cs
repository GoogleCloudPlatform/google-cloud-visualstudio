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

using System.Collections.Generic;
using System.IO;

namespace GoogleCloudExtension.TemplateWizards
{
    /// <summary>
    /// Helper methods for all template wizards.
    /// </summary>
    public static class GoogleTemplateWizardHelper
    {
        /// <summary>
        /// Deletes the project and, if is exclusive, the solution folders.
        /// </summary>
        /// <param name="replacements">The Wizard replacements <see cref="Dictionary{TKey,TValue}"/>.</param>
        public static void CleanupDirectories(Dictionary<string, string> replacements)
        {
            if (Directory.Exists(replacements[ReplacementsKeys.DestinationDirectoryKey]))
            {
                Directory.Delete(replacements[ReplacementsKeys.DestinationDirectoryKey], true);
            }
            bool isExclusive;
            if (Directory.Exists(replacements[ReplacementsKeys.SolutionDirectoryKey]) &&
                bool.TryParse(replacements[ReplacementsKeys.ExclusiveProjectKey], out isExclusive) &&
                isExclusive)
            {
                Directory.Delete(replacements[ReplacementsKeys.SolutionDirectoryKey], true);
            }
        }
    }
}