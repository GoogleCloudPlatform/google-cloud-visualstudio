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

using GoogleCloudExtension.Theming;
using GoogleCloudExtension.UserPrompt;
using GoogleCloudExtension.Utils;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;

namespace GoogleCloudExtension.Services
{
    /// <summary>
    /// This class provides helpers to show messages to the user in a uniform way.
    /// </summary>
    [Export(typeof(IUserPromptService))]
    internal class UserPromptService : IUserPromptService
    {
        public const string WarningIconPath = "Utils/Resources/ic_warning_yellow_24px.png";
        public const string ErrorIconPath = "Utils/Resources/ic_error_red_24px.png";

        private static readonly Lazy<ImageSource> s_warningIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(WarningIconPath));
        private static readonly Lazy<ImageSource> s_errorIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(ErrorIconPath));

        /// <summary>
        /// The default <see cref="IUserPromptService"/>.
        /// </summary>
        public static IUserPromptService Default => GoogleCloudExtensionPackage.Instance.UserPromptService;

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
        public bool ActionPrompt(
            string prompt,
            string title,
            string message = null,
            string actionCaption = null,
            string cancelCaption = null,
            bool isWarning = false)
        {
            return UserPromptWindow.PromptUser(
                new UserPromptWindow.Options
                {
                    Title = title,
                    Prompt = prompt,
                    Message = message,
                    ActionButtonCaption = actionCaption ?? Resources.UiYesButtonCaption,
                    CancelButtonCaption = cancelCaption ?? Resources.UiCancelButtonCaption,
                    Icon = isWarning ? s_warningIcon.Value : null
                });
        }

        /// <summary>
        /// Shows a message dialog to the user with an Ok button.
        /// </summary>
        /// <param name="message">The message for the dialog.</param>
        /// <param name="title">The title for the dialog.</param>
        public void OkPrompt(string message, string title)
        {
            UserPromptWindow.PromptUser(
                new UserPromptWindow.Options
                {
                    Title = title,
                    Prompt = message,
                    CancelButtonCaption = Resources.UiOkButtonCaption
                });
        }

        /// <summary>
        /// Shows an error message dialog to the user, with an Ok button.
        /// </summary>
        /// <param name="message">The message for the dialog.</param>
        /// <param name="title">The title for the dialog.</param>
        /// <param name="errorDetails">The error details for the dialog, optional.</param>
        public void ErrorPrompt(string message, string title, string errorDetails = null)
        {
            UserPromptWindow.PromptUser(
                new UserPromptWindow.Options
                {
                    Title = title,
                    Prompt = message,
                    ErrorDetails = errorDetails,
                    CancelButtonCaption = Resources.UiOkButtonCaption,
                    Icon = s_errorIcon.Value
                });
        }

        /// <summary>
        /// Shows an error message dialog to the user, with an Ok and a Cancel button.
        /// </summary>
        /// <param name="message">The message for the dialog.</param>
        /// <param name="title">The title for the dialog.</param>
        /// <param name="errorDetails">The error details for the dialog, optional.</param>
        /// <returns>Returns true if the user pressed the yes button, false if the user pressed the no button or closed the dialog.</returns>
        public bool ErrorActionPrompt(string message, string title, string errorDetails = null) =>
            UserPromptWindow.PromptUser(
                new UserPromptWindow.Options
                {
                    Title = title,
                    Prompt = message,
                    ErrorDetails = errorDetails,
                    ActionButtonCaption = Resources.UiYesButtonCaption,
                    CancelButtonCaption = Resources.UiNoButtonCaption,
                    Icon = s_errorIcon.Value
                });

        /// <summary>
        /// Shows an error message for the given exception.
        /// </summary>
        /// <param name="ex">The exception to show.</param>
        public void ExceptionPrompt(Exception ex)
        {
            ErrorPrompt(
                title: Resources.ExceptionPromptTitle,
                message: string.Format(Resources.ExceptionPromptMessage, GetExceptionMessage(ex)),
                errorDetails: ex.StackTrace);
        }

        private static string GetExceptionMessage(Exception ex)
        {
            if (ex is AggregateException aggregateException)
            {
                return GetExceptionMessage(ex.InnerException) ??
                    aggregateException.InnerExceptions?.Select(GetExceptionMessage)
                        .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ??
                    aggregateException.Message;
            }
            else
            {
                return ex?.Message;
            }
        }

        public TResult PromptUser<TResult>(ICommonWindowContent<IViewModelBase<TResult>> content)
        {
            var dialog = new CommonDialogWindow<IViewModelBase<TResult>>(content);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }

        public void PromptUser(ICommonWindowContent<ICloseSource> content)
        {
            var dialog = new CommonDialogWindow<ICloseSource>(content);
            dialog.ShowModal();
        }
    }
}
