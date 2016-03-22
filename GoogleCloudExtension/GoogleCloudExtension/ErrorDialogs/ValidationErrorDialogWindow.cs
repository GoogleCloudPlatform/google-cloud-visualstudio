// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.PlatformUI;

namespace GoogleCloudExtension.ErrorDialogs
{
    public class ValidationErrorDialogWindow : DialogWindow
    {
        /// <summary>
        /// Initializes the error dialog.
        /// </summary>
        /// <param name="gcloudValidationResult">The validation result to display.</param>
        /// <param name="level">What level of validation errors to display.</param>
        public ValidationErrorDialogWindow(
            GCloudValidationResult gcloudValidationResult = null,
            DnxValidationResult dnxValidationResult = null)
        {
            this.ResizeMode = System.Windows.ResizeMode.NoResize;
            this.Width = 420;
            this.Height = 350;
            this.Title = "Google Cloud Extension Error";

            var viewModel = new ValidationErrorDialogViewModel(this, gcloudValidationResult, dnxValidationResult);
            var content = new ValidationErrorDialogContent() { DataContext = viewModel };
            this.Content = content;
        }
    }
}
