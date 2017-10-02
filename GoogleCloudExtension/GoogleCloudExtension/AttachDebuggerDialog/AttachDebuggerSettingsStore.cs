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

using GoogleCloudExtension.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace GoogleCloudExtension.AttachDebuggerDialog
{
    /// <summary>
    /// Save settings for attach debugger dialog.
    /// </summary>
    internal static class AttachDebuggerSettingsStore
    {
        private const string SettingsPath = @"googlecloudvsextension\attach_debugger_dialog";
        private const string DefaultUsersSettingFile = @"gceinstace_default_user.cfg";
        private static readonly string s_defaultUsersSettingsFullPath
            = Path.Combine(GetStoragePath(), DefaultUsersSettingFile);

        /// <summary>
        /// Read GCE instance default user settings.
        /// </summary>
        public static List<InstanceDefaultUser> ReadGceInstanceDefaultUsers()
        {
            List<InstanceDefaultUser> results = null;
            if (File.Exists(s_defaultUsersSettingsFullPath))
            {
                string jsonText = File.ReadAllText(s_defaultUsersSettingsFullPath);
                if (!string.IsNullOrWhiteSpace(jsonText))
                {
                    try
                    {
                        results = JsonConvert.DeserializeObject<List<InstanceDefaultUser>>(jsonText);
                    }
                    catch (JsonSerializationException)
                    { }
                }
            }
            return results ?? new List<InstanceDefaultUser>();
        }

        /// <summary>
        /// Save GCE instance default user settings.
        /// </summary>
        public static void PersistGceInstanceDefaultUsers(List<InstanceDefaultUser> defaultUsers)
        {
            defaultUsers.ThrowIfNull(nameof(defaultUsers));
            if (!Directory.Exists(GetStoragePath()))
            {
                Directory.CreateDirectory(GetStoragePath());
            }
            File.WriteAllText(s_defaultUsersSettingsFullPath, JsonConvert.SerializeObject(defaultUsers));
        }

        private static string GetStoragePath()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, SettingsPath);
        }
    }
}