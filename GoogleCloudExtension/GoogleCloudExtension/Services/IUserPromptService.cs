// Copyright 2018 Google Inc. All Rights Reserved.
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
using GoogleCloudExtension.Theming;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.Services
{
    public interface IUserPromptService
    {
        /// <summary>
        /// Show a message dialog with a Yes and No button to the user.
        /// </summary>
        /// <param name="prompt">The message for the dialog.</param>
        /// <param name="title">The title for the dialog.</param>
        /// <param name="message">The message to show under the prompt.</param>
        /// <param name="actionCaption">The caption for the action button, it will be "Yes" by default.</param>
        /// <param name="cancelCaption">The caption for the cancel button, it will be "Cancel" by default.</param>
        /// <param name="isWarning">If true, the prompt will include a warning icon.</param>
        /// <returns>Returns true if the user pressed the action button, false if the user pressed the cancel button or closed the dialog.</returns>
        bool ActionPrompt(string prompt, string title, string message = null, string actionCaption = null, string cancelCaption = null, bool isWarning = false);

        /// <summary>
        /// Shows an error message dialog to the user, with an Ok and a Cancel button.
        /// </summary>
        /// <param name="message">The message for the dialog.</param>
        /// <param name="title">The title for the dialog.</param>
        /// <param name="errorDetails">The error details for the dialog, optional.</param>
        /// <returns>Returns true if the user pressed the yes button, false if the user pressed the no button or closed the dialog.</returns>
        bool ErrorActionPrompt(string message, string title, string errorDetails = null);

        /// <summary>
        /// Shows an error message dialog to the user, with an Ok button.
        /// </summary>
        /// <param name="message">The message for the dialog.</param>
        /// <param name="title">The title for the dialog.</param>
        /// <param name="errorDetails">The error details for the dialog, optional.</param>
        void ErrorPrompt(string message, string title, string errorDetails = null);

        /// <summary>
        /// Shows an error message for the given exception.
        /// </summary>
        /// <param name="ex">The exception to show.</param>
        void ExceptionPrompt(Exception ex);

        /// <summary>
        /// Shows a message dialog to the user with an Ok button.
        /// </summary>
        /// <param name="message">The message for the dialog.</param>
        /// <param name="title">The title for the dialog.</param>
        void OkPrompt(string message, string title);

        /// <summary>
        /// Prompts the user with the given content, and returns the result.
        /// </summary>
        /// <typeparam name="TResult">The type of the result from the dialog.</typeparam>
        /// <param name="content">The content to display to the user.</param>
        /// <returns>The result of the user prompt.</returns>
        TResult PromptUser<TResult>(ICommonWindowContent<IViewModelBase<TResult>> content);

        /// <summary>
        /// Prompts the user with the given content.
        /// </summary>
        /// <param name="content">The content to display to the user.</param>
        void PromptUser(ICommonWindowContent<ICloseSource> content);
    }
}