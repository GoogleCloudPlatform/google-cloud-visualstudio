// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.AppEngine
{
    internal class AppEngineRootViewModel : TreeHierarchy
    {
        private const string AppEngineIconResourcePath = "CloudExplorerSources/AppEngine/Resources/app_engine.png";
        private const string ModuleIconResourcePath = "CloudExplorerSources/AppEngine/Resources/ic_view_module.png";

        private static readonly Lazy<ImageSource> s_appEngineIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(AppEngineIconResourcePath));
        private static readonly Lazy<ImageSource> s_moduleIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(ModuleIconResourcePath));
        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Content = "Loading modules...",
            IsLoading = true
        };

        private bool _loading;
        private bool _loaded;

        public AppEngineRootViewModel()
        {
            Content = "AppEngine";
            Icon = s_appEngineIcon.Value;
            Children.Add(s_loadingPlaceholder);

            // Add a weak event handler to receive notifications of the deployment of app engine instances.
            // We also need to invalidate the list if the account or project changed.
            var handler = new WeakAction<object, EventArgs>(this.InvalidateAppEngineAppList);
            ExtensionEvents.AppEngineDeployed += handler.Invoke;
            GCloudWrapper.Instance.AccountOrProjectChanged += handler.Invoke;
        }

        protected override void OnIsExpandedChanged(bool newValue)
        {
            if (_loading)
            {
                return;
            }

            if (!_loaded && newValue)
            {
                LoadAppEngineAppListAsync();
            }
        }

        /// <summary>
        /// Loads the list of app engine apps, changing the state of the properties
        /// as the process advances.
        /// </summary>
        internal async void LoadAppEngineAppListAsync()
        {
            if (!GCloudWrapper.Instance.ValidateGCloudInstallation())
            {
                Debug.WriteLine("Cannot find GCloud, disabling the AppEngine tool window.");
                Children.Clear();
                Children.Add(new TreeLeaf { Content = "Please install gcloud..." });
                return;
            }

            try
            {
                _loading = true;
                var apps = await AppEngineClient.GetAppEngineAppListAsync();
                var nodes = apps
                    .GroupBy(x => x.Module)
                    .OrderBy(x => x.Key)
                    .Select(x => MakeModuleHierarchy(x))
                    .ToList();

                Children.Clear();
                foreach (var node in nodes)
                {
                    Children.Add(node);
                }

                _loaded = true;
            }
            catch (GCloudException ex)
            {
                GcpOutputWindow.OutputLine("Failed to load the list of AppEngine apps.");
                GcpOutputWindow.OutputLine(ex.Message);
                GcpOutputWindow.Activate();
            }
            finally
            {
                _loading = false;
            }
        }

        private TreeHierarchy MakeModuleHierarchy(IGrouping<string, ModuleAndVersion> src)
        {
            var versions = src
                .OrderBy(x => x, new VersionComparer())
                .Select(x => new VersionViewModel(x));
            return new TreeHierarchy(versions) { Content = src.Key, Icon = s_moduleIcon.Value };
        }

        private void InvalidateAppEngineAppList(object src, EventArgs args)
        {
            Refresh();
        }

        internal void Refresh()
        {
            if (!_loaded)
            {
                return;
            }

            ResetChildren();
            LoadAppEngineAppListAsync();
        }

        private void ResetChildren()
        {
            _loaded = false;
            Children.Clear();
            Children.Add(s_loadingPlaceholder);
        }
    }
}
