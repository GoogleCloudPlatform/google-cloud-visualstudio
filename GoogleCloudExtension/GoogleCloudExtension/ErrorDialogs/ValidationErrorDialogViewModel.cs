// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.Utils;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace GoogleCloudExtension.ErrorDialogs
{
    /// <summary>
    /// The view model for the ValidationErrorDialog.
    /// </summary>
    public class ValidationErrorDialogViewModel : ViewModelBase
    {
        private readonly ValidationErrorDialogWindow _owner;
        private bool _showMissingComponents;
        private bool _showMissingGCloud;
        private bool _showMissingDnxRuntime;
        private string _installComponentsCommandLine;
        private bool _isRefreshing;

        /// <summary>
        /// Whether there are missing components to display.
        /// </summary>
        public bool ShowMissingComponents
        {
            get { return _showMissingComponents; }
            set { SetValueAndRaise(ref _showMissingComponents, value); }
        }

        // Whether to show the error message about missing the gcloud SDK.
        public bool ShowMissingGCloud
        {
            get { return _showMissingGCloud; }
            set { SetValueAndRaise(ref _showMissingGCloud, value); }
        }

        // Whether to show the error message about missing the DNX runtime.
        public bool ShowMissingDnxRuntime
        {
            get { return _showMissingDnxRuntime; }
            set { SetValueAndRaise(ref _showMissingDnxRuntime, value); }
        }

        // The command line to use to isntall the missing components.
        public string InstallComponentsCommandLine
        {
            get { return _installComponentsCommandLine; }
            set { SetValueAndRaise(ref _installComponentsCommandLine, value); }
        }

        public bool IsRefreshing
        {
            get { return _isRefreshing; }
            set { SetValueAndRaise(ref _isRefreshing, value);  }
        }

        public bool IsValidInstallation => !ShowMissingComponents && !ShowMissingGCloud && !ShowMissingDnxRuntime;

        /// <summary>
        /// The command to execute when pressing the OK button.
        /// </summary>
        public WeakCommand OnOkCommand { get; }

        /// <summary>
        /// The command to execute when pressing the Refresh button.
        /// </summary>
        public WeakCommand OnRefreshCommand { get; }

        /// <summary>
        /// The command to execute when the copy button is pressed.
        /// </summary>
        public WeakCommand OnCopyCommand { get; }

        public ValidationErrorDialogViewModel(
            ValidationErrorDialogWindow owner,
            GCloudValidationResult gcloudValidationResult,
            DnxValidationResult dnxValidationResult)
        {
            _owner = owner;

            OnOkCommand = new WeakCommand(() => _owner.Close());
            OnRefreshCommand = new WeakCommand(OnRefreshHandler);
            OnCopyCommand = new WeakCommand(OnCopyHandler);

            SetPublicProperties(gcloudValidationResult, dnxValidationResult);
        }

        private void SetPublicProperties(GCloudValidationResult gcloudValidationResult, DnxValidationResult dnxValidationResult)
        {
            if (gcloudValidationResult != null && !gcloudValidationResult.IsValidGCloudInstallation)
            {
                ShowMissingGCloud = !gcloudValidationResult.IsGCloudInstalled;
                if (gcloudValidationResult.MissingComponents.Count != 0)
                {
                    var missingComponentsList = String.Join(" ", gcloudValidationResult.MissingComponents);
                    InstallComponentsCommandLine = $"gcloud components install {missingComponentsList}";
                    ShowMissingComponents = true;
                    OnCopyCommand.CanExecuteCommand = true;
                }
            }
            else
            {
                ShowMissingGCloud = false;
                ShowMissingComponents = false;
                OnCopyCommand.CanExecuteCommand = false;
            }

            if (dnxValidationResult != null && !dnxValidationResult.IsDnxInstalled)
            {
                ShowMissingDnxRuntime = true;
            }
            else
            {
                ShowMissingDnxRuntime = false;
            }

            RaisePropertyChanged(nameof(IsValidInstallation));
        }

        private async void OnRefreshHandler()
        {
            GCloudValidationResult gcloudValidationResult = null;
            DnxValidationResult dnxValidationResult = null;

            try
            {
                OnRefreshCommand.CanExecuteCommand = false;
                IsRefreshing = true;

                EnvironmentUtils.Reset();
                if (ShowMissingGCloud || ShowMissingComponents)
                {
                    gcloudValidationResult = await EnvironmentUtils.ValidateGCloudInstallationAsync();
                }
                if (ShowMissingDnxRuntime)
                {
                    dnxValidationResult = EnvironmentUtils.ValidateDnxInstallation();
                }
            }
            finally
            {
                OnRefreshCommand.CanExecuteCommand = true;
                IsRefreshing = false;
            }

            SetPublicProperties(gcloudValidationResult, dnxValidationResult);

            if (IsValidInstallation)
            {
                _owner.Close();
            }
        }

        private void OnCopyHandler()
        {
            Clipboard.SetText(InstallComponentsCommandLine);
        }
    }
}
