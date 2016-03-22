// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.Utils;
using System;
using System.Linq;
using System.Windows.Input;

namespace GoogleCloudExtension.ErrorDialogs
{
    /// <summary>
    /// The view model for the ValidationErrorDialog.
    /// </summary>
    public class ValidationErrorDialogViewModel : ViewModelBase
    {
        private readonly ValidationErrorDialogWindow _owner;

        /// <summary>
        /// Whether there are missing components to display.
        /// </summary>
        public bool ShowMissingComponents { get; }

        // Whether to show the error message about missing the gcloud SDK.
        public bool ShowMissingGCloud { get; }

        // Whether to show the error message about missing the DNX runtime.
        public bool ShowMissingDnxRuntime { get; }

        // The command line to use to isntall the missing components.
        public string InstallComponentsCommandLine { get; }

        /// <summary>
        /// The command to execute when pressing the OK button.
        /// </summary>
        public ICommand OnOkCommand { get; }

        public ValidationErrorDialogViewModel(
            ValidationErrorDialogWindow owner,
            GCloudValidationResult gcloudValidationResult,
            DnxValidationResult dnxValidationResult)
        {
            _owner = owner;

            if (gcloudValidationResult != null && !gcloudValidationResult.IsValidGCloudInstallation)
            {
                ShowMissingGCloud = !gcloudValidationResult.IsGCloudInstalled;
                if (gcloudValidationResult.MissingComponents.Count != 0)
                {
                    var missingComponentsList = String.Join(" ", gcloudValidationResult.MissingComponents);
                    InstallComponentsCommandLine = $"gcloud components install {missingComponentsList}";
                    ShowMissingComponents = true;
                }
            }

            if (dnxValidationResult != null && !dnxValidationResult.IsDnxInstalled)
            {
                ShowMissingDnxRuntime = true;
            }

            OnOkCommand = new WeakCommand(() => _owner.Close());
        }
    }
}
