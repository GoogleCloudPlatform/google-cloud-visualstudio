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

using Microsoft.VisualStudio;
using System;
using System.Diagnostics;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class implements an event handler for the exceptions that escape the given <seealso cref="Action"/>.
    /// </summary>
    public static class ErrorHandlerUtils
    {
        /// <summary>
        /// Runs the given <seealso cref="Action"/> and handles all non-critical exceptions by showing an
        /// error dialog to the user. If the exception is critical, as determiend by <seealso cref="ErrorHandler.IsCriticalException(Exception)"/>
        /// then it is re-thrown as this could be that the process is not in a good state to continue executing.
        /// </summary>
        /// <param name="action"></param>
        public static void HandleExceptions(Action action)
        {
            try
            {
                action();
            }
            catch (AggregateException ex)
            {
                Debug.WriteLine($"Uncaught aggregate exception: {ex.Message}");
                if (ErrorHandler.ContainsCriticalException(ex))
                {
                    throw;
                }
                UserPromptUtils.ExceptionPrompt(ex);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Uncaught exception: {ex.Message}");
                if (ErrorHandler.IsCriticalException(ex))
                {
                    throw;
                }
                UserPromptUtils.ExceptionPrompt(ex);
            }
        }
    }
}
