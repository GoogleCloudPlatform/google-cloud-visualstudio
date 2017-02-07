﻿// Copyright 2017 Google Inc. All Rights Reserved.
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

using System;
using System.Text.RegularExpressions;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Common utils for publishing steps to GCP.
    /// </summary>
    public static class GcpPublishStepsUtils
    {
        private static readonly Regex s_validNamePattern = new Regex(@"^(?!-)[a-z\d\-]{1,100}$");

        /// <summary>
        /// Returns a default version name suitable for publishing to GKE and Flex.
        /// </summary>
        /// <returns>The default name string.</returns>
        public static string GetDefaultVersion()
        {
            var now = DateTime.Now;
            return String.Format(
                "{0:0000}{1:00}{2:00}t{3:00}{4:00}{5:00}",
                now.Year, now.Month, now.Day,
                now.Hour, now.Minute, now.Second);
        }
        
        /// <summary>
        /// Determines if the given name is a valid name.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>True if the name is valid, false otherwise.</returns>
        public static bool IsValidName(string name)
        {
            return !String.IsNullOrEmpty(name) && s_validNamePattern.IsMatch(name);
        }
    }
}
