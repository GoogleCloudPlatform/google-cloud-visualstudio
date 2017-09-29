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

namespace GoogleCloudExtension.TemplateWizards
{
    /// <summary>
    /// A collection of keys used by the replacement dictionarys of template wizards.
    /// </summary>
    public static class ReplacementsKeys
    {
        public const string GcpProjectIdKey = "$gcpprojectid$";
        public const string DestinationDirectoryKey = "$destinationdirectory$";
        public const string ExclusiveProjectKey = "$exclusiveproject$";
        public const string SolutionDirectoryKey = "$solutiondirectory$";
        public const string PackagesPathKey = "$packagespath$";
        public const string ProjectNameKey = "$projectname$";
        public const string TemplateChooserResultKey = "$templateChooserResult$";
        public const string SafeProjectNameKey = "$safeprojectname$";
        public const string EmbeddableSafeProjectNameKey = "_safe_project_name_";
    }
}