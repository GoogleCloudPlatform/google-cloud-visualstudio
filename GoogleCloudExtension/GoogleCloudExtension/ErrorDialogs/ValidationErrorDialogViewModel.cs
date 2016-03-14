// Copyright 2015 Google Inc. All Rights Reserved.
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
        private readonly GCloudValidationResult _gcloudValidationResult;
        private readonly DnxValidationResult _dnxValidationResult;

        /// <summary>
        /// Whether there are missing components to display.
        /// </summary>
        public bool HasMissingComponents => (_gcloudValidationResult?.MissingComponents.Count ?? 0) != 0;

        // Whether to show the error message about missing the gcloud SDK.
        public bool ShowMissingGCloud => !(_gcloudValidationResult?.IsValidGCloudInstallation() ?? true);

        // Whether to show the error message about missing the DNX runtime.
        public bool ShowMissingDnxRuntime => !(_dnxValidationResult?.IsValidDnxInstallation() ?? true);

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
            _gcloudValidationResult = gcloudValidationResult;
            _dnxValidationResult = dnxValidationResult;
            var missingComponentsList = String.Join(" ", _gcloudValidationResult?.MissingComponents?.Select(x => x.Id) ?? Enumerable.Empty<string>());
            InstallComponentsCommandLine = $"gcloud components update {missingComponentsList}";
            OnOkCommand = new WeakCommand(() => _owner.Close());
        }
    }
}
