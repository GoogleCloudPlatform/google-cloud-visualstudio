// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GCloud;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

namespace GoogleCloudExtension.ComputeEngineResources
{
    public class ComputeEngineResourcesViewModel : ViewModelBase
    {
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

        public bool CurrentInstanceIsTerminated
        {
            get
            {
                if (this.CurrentInstance != null)
                {
                    return this.CurrentInstance.Status == "TERMINATED";
                }
                return false;
            }
        }

        public bool CurrentInstanceIsRunning
        {
            get
            {
                if (this.CurrentInstance != null)
                {
                    return this.CurrentInstance.Status == "RUNNING";
                }
                return false;
            }
        }

        private ICommand _RefreshCommand;
        public ICommand RefreshCommand
        {
            get { return _RefreshCommand; }
            set { SetValueAndRaise(ref _RefreshCommand, value); }
        }

        private ICommand _StartCommand;
        public ICommand StartCommand
        {
            get { return _StartCommand; }
            set { SetValueAndRaise(ref _StartCommand, value); }
        }

        private ICommand _StopCommand;
        public ICommand StopCommand
        {
            get { return _StopCommand; }
            set { SetValueAndRaise(ref _StopCommand, value); }
        }

        public bool HaveInstances
        {
            get { return this.Instances != null && this.Instances.Count != 0; }
        }

        public ComputeEngineResourcesViewModel()
        {
            RefreshCommand = new WeakCommand(this.OnRefresh);
            StartCommand = new WeakCommand(this.OnStartInstance);
            StopCommand = new WeakCommand(this.OnStopInstance);

            var handler = new WeakHandler(this.InvalidateInstancesListAsync);
            GCloudWrapper.DefaultInstance.AccountOrProjectChanged += handler.OnEvent;
        }

        public async void LoadComputeInstancesList()
        {
            if (!GCloudWrapper.DefaultInstance.ValidateGCloudInstallation())
            {
                Debug.WriteLine("GCloud is not installed, disabling GCE tool window.");
                return;
            }

            try
            {
                this.Loading = true;
                this.LoadingMessage = "Loading instances...";
                this.Instances = new List<ComputeInstance>();
                this.Instances = await GCloudWrapper.DefaultInstance.GetComputeInstanceListAsync();
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
            LoadComputeInstancesList();
        }

        private void OnRefresh(object param)
        {
            LoadComputeInstancesList();
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
                await GCloudWrapper.DefaultInstance.StartComputeInstanceAsync(instance.Name, instance.Zone);
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
            LoadComputeInstancesList();
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
                await GCloudWrapper.DefaultInstance.StopComputeInstanceAsync(instance.Name, instance.Zone);
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
            LoadComputeInstancesList();
        }
    }
}
