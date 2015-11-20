// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

namespace GoogleCloudExtension.ComputeEngineResources
{
    /// <summary>
    /// This class is the viemodel for the Compute Engine resources tool window, containing
    /// the model data for the tool window as well as the controller for the window.
    /// </summary>
    public class ComputeEngineResourcesViewModel : ViewModelBase
    {
        private IList<ComputeInstance> _instances;
        private ComputeInstance _currentInstance;

        /// <summary>
        /// The list of instances in the current project.
        /// </summary>
        public IList<ComputeInstance> Instances
        {
            get { return _instances; }
            private set
            {
                SetValueAndRaise(ref _instances, value);
                RaisePropertyChanged(nameof(HaveInstances));
                this.CurrentInstance = value?.FirstOrDefault();
            }
        }

        /// <summary>
        /// The currently selected instance.
        /// </summary>
        public ComputeInstance CurrentInstance
        {
            get { return _currentInstance; }
            set
            {
                SetValueAndRaise(ref _currentInstance, value);
                RaisePropertyChanged(nameof(CurrentInstanceIsRunning));
                RaisePropertyChanged(nameof(CurrentInstanceIsTerminated));
            }
        }

        /// <summary>
        /// Helper property to determine whether the currently selected instance is in the
        /// TERMINATED state.
        /// </summary>
        public bool CurrentInstanceIsTerminated => CurrentInstance?.IsTerminated ?? false;

        /// <summary>
        /// Herlper property to determine whether the currently selected instance is in the
        /// RUNNING state.
        /// </summary>
        public bool CurrentInstanceIsRunning => CurrentInstance?.IsRunning ?? false;

        /// <summary>
        /// The command to execute to referesh the list of instances.
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// The command to execute to start the current instance.
        /// </summary>
        public ICommand StartCommand { get; }

        /// <summary>
        /// The command to execute to stop the current instance.
        /// </summary>
        public ICommand StopCommand { get; }

        /// <summary>
        /// Whether there are instances or not in the list.
        /// </summary>
        public bool HaveInstances => (Instances?.Count ?? 0) != 0;

        public ComputeEngineResourcesViewModel()
        {
            RefreshCommand = new WeakCommand(this.OnRefresh);
            StartCommand = new WeakCommand(this.OnStartInstance);
            StopCommand = new WeakCommand(this.OnStopInstance);

            var handler = new WeakHandler(this.InvalidateInstancesListAsync);
            GCloudWrapper.Instance.AccountOrProjectChanged += handler.OnEvent;
        }

        /// <summary>
        /// Loads the list of compute instances for the view.
        /// </summary>
        public async void LoadComputeInstancesListAsync()
        {
            if (!GCloudWrapper.Instance.ValidateGCloudInstallation())
            {
                Debug.WriteLine("GCloud is not installed, disabling GCE tool window.");
                return;
            }

            try
            {
                this.Loading = true;
                this.LoadingMessage = "Loading instances...";
                this.Instances = new List<ComputeInstance>();
                this.Instances = await ComputeEngineClient.GetComputeInstanceListAsync();
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.OutputLine("Failed to load instances list.");
                AppEngineOutputWindow.OutputLine(ex.Message);
                AppEngineOutputWindow.Activate();
            }
            finally
            {
                this.Loading = false;
            }
        }

        private void InvalidateInstancesListAsync(object sender, EventArgs args)
        {
            Debug.WriteLine("Invalidating GCE list.");
            LoadComputeInstancesListAsync();
        }

        #region Command handlers

        private void OnRefresh(object param)
        {
            LoadComputeInstancesListAsync();
        }

        private async void OnStartInstance(object param)
        {
            var instance = (ComputeInstance)param;
            if (instance == null)
            {
                return;
            }
            if (!instance.IsTerminated)
            {
                Debug.WriteLine($"Expected status TERMINATED got: {instance.Status}");
                return;
            }
            Debug.WriteLine($"Starting instance {instance.Name} in zone {instance.Zone}");

            try
            {
                this.Loading = true;
                this.LoadingMessage = "Starting Instance...";
                await ComputeEngineClient.StartComputeInstanceAsync(instance.Name, instance.Zone);
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.OutputLine($"Failed to start instance {instance.Name} in zone {instance.Zone}");
                AppEngineOutputWindow.OutputLine(ex.Message);
                AppEngineOutputWindow.Activate();
            }
            finally
            {
                this.Loading = false;
            }
            LoadComputeInstancesListAsync();
        }

        private async void OnStopInstance(object param)
        {
            var instance = (ComputeInstance)param;
            if (instance == null)
            {
                return;
            }
            if (!instance.IsRunning)
            {
                Debug.WriteLine($"Expected status RUNNING, got: {instance.Status}");
                return;
            }
            Debug.WriteLine($"Stopping instance {instance.Name} in zone {instance.Zone}");

            try
            {
                this.Loading = true;
                this.LoadingMessage = "Stopping Instance...";
                await ComputeEngineClient.StopComputeInstanceAsync(instance.Name, instance.Zone);
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.OutputLine($"Failed to stop instance {instance.Name} in zone {instance.Zone}.");
                AppEngineOutputWindow.OutputLine(ex.Message);
                AppEngineOutputWindow.Activate();
            }
            finally
            {
                this.Loading = false;
            }
            LoadComputeInstancesListAsync();
        }

        #endregion
    }
}
