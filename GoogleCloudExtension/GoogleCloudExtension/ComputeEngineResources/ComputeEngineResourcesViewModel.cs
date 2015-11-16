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
        /// <summary>
        /// The list of instances in the current project.
        /// </summary>
        private IList<ComputeInstance> _Instances;
        public IList<ComputeInstance> Instances
        {
            get { return _Instances; }
            set
            {
                SetValueAndRaise(ref _Instances, value);
                RaisePropertyChanged(nameof(HaveInstances));
                this.CurrentInstance = value?.FirstOrDefault();
            }
        }

        /// <summary>
        /// The currently selected instance.
        /// </summary>
        private ComputeInstance _CurrentInstance;
        public ComputeInstance CurrentInstance
        {
            get { return _CurrentInstance; }
            set
            {
                SetValueAndRaise(ref _CurrentInstance, value);
                RaisePropertyChanged(nameof(CurrentInstanceIsRunning));
                RaisePropertyChanged(nameof(CurrentInstanceIsTerminated));
            }
        }

        /// <summary>
        /// Helper property to determine whether the currently selected instance is in the
        /// TERMINATED state.
        /// </summary>
        public bool CurrentInstanceIsTerminated => CurrentInstance?.Status == "TERMINATED";

        /// <summary>
        /// Herlper property to determine whether the currently selected instance is in the
        /// RUNNING state.
        /// </summary>
        public bool CurrentInstanceIsRunning => CurrentInstance?.Status == "RUNNING";

        /// <summary>
        /// The command to execute to referesh the list of instances.
        /// </summary>
        public ICommand RefreshCommand { get; private set; }

        /// <summary>
        /// The command to execute to start the current instance.
        /// </summary>
        public ICommand StartCommand { get; private set; }

        /// <summary>
        /// The command to execute to stop the current instance.
        /// </summary>
        public ICommand StopCommand { get; private set; }

        /// <summary>
        /// Whether there are instances or not in the list.
        /// </summary>
        public bool HaveInstances => Instances?.Count != 0;

        public ComputeEngineResourcesViewModel()
        {
            RefreshCommand = new WeakCommand(this.OnRefresh);
            StartCommand = new WeakCommand(this.OnStartInstance);
            StopCommand = new WeakCommand(this.OnStopInstance);

            var handler = new WeakHandler(this.InvalidateInstancesListAsync);
            GCloudWrapper.Instance.AccountOrProjectChanged += handler.OnEvent;
        }

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

        private void OnRefresh(object param)
        {
            LoadComputeInstancesListAsync();
        }

        private async void OnStartInstance(object param)
        {
            var instance = param as ComputeInstance;
            if (instance == null)
            {
                Debug.WriteLine($"Expected ComputeEngine got: {instance}");
                return;
            }
            if (instance.Status != "TERMINATED")
            {
                Debug.WriteLine($"Expected status TERMINATED got: {instance.Status}");
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
            var instance = param as ComputeInstance;
            if (instance == null)
            {
                Debug.WriteLine($"Expected ComputeEngine got: {instance}");
                return;
            }
            if (instance.Status != "RUNNING")
            {
                Debug.WriteLine($"Expected status RUNNING, got: {instance.Status}");
                return;
            }
            Debug.WriteLine($"Stoping instance {instance.Name} in zone {instance.Zone}");

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
    }
}
