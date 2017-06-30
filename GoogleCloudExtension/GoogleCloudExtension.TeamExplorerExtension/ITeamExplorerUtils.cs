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

using System.Windows.Input;

namespace GoogleCloudExtension.TeamExplorerExtension
{
    /// <summary>
    /// Define interfaces for methods, properties that VS2015 and VS2017 team explorer exposes.
    /// </summary>
    public interface ITeamExplorerUtils
    {
        /// <summary>
        /// Show a message on top of the Team Explorer 
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="command">The command that can be executed when clicking at the message.</param>
        void ShowMessage(string message, ICommand command);

        /// <summary>
        /// Show an error message on top of the Team Explorer 
        /// </summary>
        /// <param name="message">The error message to display.</param>
        void ShowError(string message);

        /// <summary>
        /// Returns current active repository.
        /// </summary>
        string GetActiveRepository();

        /// <summary>
        /// Navigate to home section of Team Explorer
        /// </summary>
        void ShowHomeSection();
    }
}
