// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Credentials;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.DataSources.Models;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.AppEngine
{
    internal class GaeVersionViewModel : TreeLeaf, ICloudExplorerItemSource
    {
        private const string IconResourcePath = "CloudExplorerSources/AppEngine/Resources/ic_web.png";
        private static readonly Lazy<ImageSource> s_versionIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));

        private AppEngineRootViewModel _owner;
        private readonly string _serviceId;
        private readonly GaeVersion _version;
        private readonly double _trafficSplit;

        private readonly WeakCommand _openAppCommand;
        private readonly WeakCommand _deleteVersionCommand;
        private readonly WeakCommand _setDefaultVersionCommand;

        public object Item { get; }

        public GaeVersionViewModel(AppEngineRootViewModel owner, string serviceId, GaeVersion version, double trafficSplit)
        {
            _owner = owner;
            _serviceId = serviceId;
            _version = version;
            _trafficSplit = trafficSplit;
            _openAppCommand = new WeakCommand(OnOpenApp);
            _deleteVersionCommand = new WeakCommand(OnDeleteVersion);
            _setDefaultVersionCommand = new WeakCommand(OnSetDefaultVersion, canExecuteCommand: trafficSplit != 1.0);

            Item = new VersionItem(_version, _trafficSplit);

            // Initialize the TreeLeaf properties.
            Content = version.Id;
            var menuItems = new List<MenuItem>
            {
                new MenuItem {Header="Open App in Browser", Command = _openAppCommand },
                new MenuItem {Header="Set as Default", Command = _setDefaultVersionCommand },
                new MenuItem {Header="Delete", Command = _deleteVersionCommand },
            };
            ContextMenu = new ContextMenu { ItemsSource = menuItems };
            Icon = s_versionIcon.Value;
        }

        #region Command handlers

        private async void OnOpenApp()
        {
            try
            {
                _openAppCommand.CanExecuteCommand = false;
                var url = $"https://{_version.Id}-dot-{_serviceId}-dot-{_owner.Owner.CurrentProject.Id}.appspot.com/";
                Debug.WriteLine($"Opening URL: {url}");
                Process.Start(url);
            }
            finally
            {
                _openAppCommand.CanExecuteCommand = true;
            }
        }

        private async void OnDeleteVersion()
        {
            try
            {
                if (!UserPromptUtils.YesNoPrompt(
                    $"Are you sure you want to delete version {_version.Id} in service {_serviceId}?",
                    $"Deleting {_version.Id}"))
                {
                    Debug.WriteLine("The user cancelled the operation.");
                    return;
                }

                _deleteVersionCommand.CanExecuteCommand = false;
                Content = $"{_version.Id} (Deleting...)";
                IsLoading = true;

                var oauthToken = await CredentialsManager.GetAccessTokenAsync();
                await GaeDataSource.DeleteVersionAsync(
                    projectId: _owner.Owner.CurrentProject.Id,
                    serviceId: _serviceId,
                    versionId: _version.Id,
                    oauthToken: oauthToken);
                _owner.Refresh();
            }
            catch (DataSourceException ex)
            {
                Content = _version.Id;
                IsLoading = false;

                _deleteVersionCommand.CanExecuteCommand = true;
                GcpOutputWindow.OutputLine($"Failed to delete version {_version.Id} in service {_serviceId}");
                GcpOutputWindow.OutputLine(ex.Message);
                GcpOutputWindow.Activate();
            }
        }

        private async void OnSetDefaultVersion()
        {
            try
            {
                _setDefaultVersionCommand.CanExecuteCommand = false;
                Content = $"{_version.Id} (Setting as default...)";
                IsLoading = true;
                var oauthToken = await CredentialsManager.GetAccessTokenAsync();
                await GaeDataSource.SetServiceTrafficAllocationAsync(
                    _owner.Owner.CurrentProject.Id,
                    _serviceId,
                    new Dictionary<string, double> { { _version.Id, 1.0 } },
                    oauthToken);
                _owner.Refresh();
            }
            catch (DataSourceException ex)
            {
                Content = _version.Id;
                IsLoading = false;

                _setDefaultVersionCommand.CanExecuteCommand = true;
                GcpOutputWindow.OutputLine("Failed to set default version.");
                GcpOutputWindow.OutputLine(ex.Message);
                GcpOutputWindow.Activate();
            }
        }

        #endregion
    }
}
