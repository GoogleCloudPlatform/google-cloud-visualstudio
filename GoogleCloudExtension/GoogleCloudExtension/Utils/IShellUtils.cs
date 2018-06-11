// Copyright 2016 Google Inc. All Rights Reserved.
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

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;

namespace GoogleCloudExtension.Utils
{
    public interface IShellUtils
    {
        /// <summary>
        /// Returns true if the shell is in a busy state.
        /// </summary>
        bool IsBusy();

        /// <summary>
        /// Changes the UI state to a busy state. The pattern to use this method is to assign the result value
        /// to a variable in a using statement.
        /// </summary>
        /// <returns>An implementation of <seealso cref="IDisposable"/> that will cleanup the state change on dispose.</returns>
        IDisposable SetShellUIBusy();

        /// <summary>
        /// Updates the UI state of all commands in the VS shell. This is useful when the state that determines
        /// if a command is enabled/disabled (or visible/invisiable) changes and the commands in the menus need
        /// to be updated.
        /// In essence this method will cause the <seealso cref="OleMenuCommand.BeforeQueryStatus"/> event in all commands to be
        /// triggered again.
        /// </summary>
        void InvalidateCommandsState();

        /// <summary>
        /// Executes the "File.OpenProject" command in the shell.
        /// </summary>
        void OpenProject();

        /// <summary>
        /// Opens a source file in Visual Studio.
        /// </summary>
        /// <param name="sourceFile">Source file path</param>
        /// <returns>The Window that displays the project item.</returns>
        Window Open(string sourceFile);

        /// <summary>
        /// Get Visual Studio <seealso cref="IServiceProvider"/>.
        /// </summary>
        ServiceProvider GetGloblalServiceProvider();

        /// <summary>
        /// Executes the "File.SaveAll" command in the shell, which will save all currently dirty files.
        /// </summary>
        void SaveAllFiles();

        /// <summary>
        /// Register a Visual Studio Window close event handler.
        /// </summary>
        /// <param name="onWindowCloseEventHandler">The event handler.</param>
        void RegisterWindowCloseEventHandler(Action<Window> onWindowCloseEventHandler);

        /// <summary>
        /// Create empty solution at the <paramref name="localPath"/>
        /// </summary>
        /// <param name="localPath">Create the solution at the path.</param>
        /// <param name="name">The solution name.</param>
        void CreateEmptySolution(string localPath, string name);

        /// <summary>
        /// Open a create solution dialog on the given path.
        /// </summary>
        /// <param name="path">The initial path in the create solution dialog.</param>
        void LaunchCreateSolutionDialog(string path);
    }
}