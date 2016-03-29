// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.DataSources.Models;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.AppEngine
{
    internal class VersionViewModel : TreeLeaf, ICloudExplorerItemSource
    {
        private const string IconResourcePath = "CloudExplorerSources/AppEngine/Resources/ic_web.png";
        private static readonly Lazy<ImageSource> s_versionIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));

        private readonly string _serviceId;
        private readonly GaeVersion _version;
        private readonly double _trafficSplit;

        private readonly WeakCommand _openAppCommand;
        private readonly WeakCommand _deleteVersionCommand;
        private readonly WeakCommand _setDefaultVersionCommand;

        public object Item { get; }

        public VersionViewModel(string serviceId, GaeVersion version, double trafficSplit)
        {
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
                var accountAndProject = await GCloudWrapper.Instance.GetCurrentCredentialsAsync();
                var url = $"https://{_version.Id}-dot-{_serviceId}-dot-{accountAndProject.ProjectId}.appspot.com/";
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
                _deleteVersionCommand.CanExecuteCommand = false;
                Content = $"{_version.Id} (Deleting...)";
                await AppEngineClient.DeleteAppVersion(_serviceId, _version.Id);
            }
            catch (GCloudException ex)
            {
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
                var credentials = await GCloudWrapper.Instance.GetCurrentCredentialsAsync();
                var oauthToken = await GCloudWrapper.Instance.GetAccessTokenAsync();
                await GaeDataSource.SetServiceTrafficAllocationAsync(
                    credentials.ProjectId,
                    _serviceId,
                    new Dictionary<string, double> { { _version.Id, 1.0 } },
                    oauthToken);
            }
            catch (DataSourceException ex)
            {
                _setDefaultVersionCommand.CanExecuteCommand = true;
                GcpOutputWindow.OutputLine("Failed to set default version.");
                GcpOutputWindow.OutputLine(ex.Message);
                GcpOutputWindow.Activate();
            }
        }

        #endregion
    }
}
