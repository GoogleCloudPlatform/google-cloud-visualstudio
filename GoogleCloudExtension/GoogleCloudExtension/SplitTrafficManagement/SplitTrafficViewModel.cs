// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google.Apis.Appengine.v1.Data;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System.Globalization;
using GoogleCloudExtension.DataSources;

namespace GoogleCloudExtension.SplitTrafficManagement
{
    /// <summary>
    /// The changes made by the user in the <seealso cref="SplitTrafficWindow"/> dialog.
    /// </summary>
    public class SplitTrafficChange
    {
        /// <summary>
        /// The map of version Ids to traffic percentages.
        /// </summary>
        public IDictionary<string, double?> Allocations;

        /// <summary>
        /// The mechanism to shard the traffic by.  Either "COOKIE" or "IP"
        /// </summary>
        public string ShardBy;

        public SplitTrafficChange(IDictionary<string, double?> allocations, string shardBy)
        {
            Allocations = allocations;
            ShardBy = shardBy;
        }
    }

    /// <summary>
    /// This class is the view model for the <seealso cref="SplitTrafficWindow"/> dialog.
    /// </summary>
    public class SplitTrafficViewModel : ViewModelBase
    {
        private readonly SplitTrafficWindow _owner;

        private readonly Service _service;

        private string _versionId;
        private int _trafficAllocation;
        private bool _isIpChecked;
        private bool _isCookieChecked;

        /// <summary>
        /// The version id, this is bound to an text box in the UI to allow the 
        /// user to add new versions to the traffic split.
        /// </summary>
        public string VersionId
        {
            get { return _versionId; }
            set { SetValueAndRaise(ref _versionId, value); }
        }

        /// <summary>
        /// The traffic allocation, this is bound to an text box in the UI to allow the 
        /// user to add new versions to the traffic split.
        /// </summary>
        public int TrafficAllocation
        {
            get { return _trafficAllocation; }
            set { SetValueAndRaise(ref _trafficAllocation, value); }
        }

        /// <summary>
        /// Whether the user has checked IP address as the way to shard traffic. This is
        /// bound in the UI to a radio button.
        /// </summary>
        public bool IsIpChecked
        {
            get { return _isIpChecked; }
            set { SetValueAndRaise(ref _isIpChecked, value); }
        }

        /// <summary>
        /// Whether the user has checked cookies as the way to shard traffic. This is
        /// bound in the UI to a radio button.
        /// </summary>
        public bool IsCookieChecked
        {
            get { return _isCookieChecked; }
            set { SetValueAndRaise(ref _isCookieChecked, value); }
        }

        /// <summary>
        /// True if there are versions that do not have traffic allocated to them.  This
        /// makes 'IsEnabled' checks in the UI more simple.
        /// </summary>
        public bool HasAvailableVersions => AvailableVersions.Count != 0;

        /// <summary>
        /// The list of traffic allocations.
        /// </summary>
        public ObservableCollection<SplitTrafficModel> Allocations { get; } = new ObservableCollection<SplitTrafficModel>();

        /// <summary>
        /// The list of versions that are not in the traffic allocations.
        /// </summary>
        public ObservableCollection<string> AvailableVersions { get; } = new ObservableCollection<string>();

        /// <summary>
        /// The changes that were made by the user. This property will be null if the user
        /// cancelled the dialog.
        /// </summary>
        public SplitTrafficChange Result { get; private set; }

        /// <summary>
        /// The command to execute when the Save button is pressed.
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// The command to execute when the Delete button is pressed.
        /// </summary>
        public ICommand DeleteCommand { get; }

        /// <summary>
        /// The command to execute when the Add Traffic Version button is pressed.
        /// </summary>
        public ICommand AddTrafficAllocationCommand { get; }

        public SplitTrafficViewModel(SplitTrafficWindow owner, Service service, IEnumerable<Google.Apis.Appengine.v1.Data.Version> versions)
        {
            _owner = owner;
            _service = service;

            // Set up the radio buttons properly.  Default to "IP" splitting if none is set.
            IsCookieChecked = GaeServiceExtensions.ShardByCookie.Equals(service?.Split?.ShardBy);
            IsIpChecked = !IsCookieChecked;

            // Set up the current allocations and avaible versions.
            foreach (var allocation in GetAllocations(service))
            {
                Allocations.Add(allocation);
            }
            foreach (var version in GetAvailableVersions(service, versions))
            {
                AvailableVersions.Add(version);
            }

            Result = null;
            SaveCommand = new WeakCommand(OnSaveCommand);
            DeleteCommand = new WeakCommand<SplitTrafficModel>(OnDeleteCommand);
            AddTrafficAllocationCommand = new WeakCommand(OnAddTrafficAllocationCommand);
        }

        /// <summary>
        /// Get a list of traffic allocations of a service's versions as <seealso cref="SplitTrafficModel"/>
        /// </summary>
        private ObservableCollection<SplitTrafficModel> GetAllocations(Service service)
        {
            IDictionary<string, double?> allocations = service?.Split?.Allocations ??
                new Dictionary<string, double?>();
            IEnumerable<SplitTrafficModel> models = allocations
                .Where(x => x.Value != null)
                .Select((x) => new SplitTrafficModel(x.Key, Convert.ToInt32(x.Value * 100)));
            return new ObservableCollection<SplitTrafficModel>(models);
        }

        /// <summary>
        /// Get a list of versions that are not allocated traffic in a service.
        /// </summary>
        private ObservableCollection<string> GetAvailableVersions(
            Service service, IEnumerable<Google.Apis.Appengine.v1.Data.Version> versions)
        {
            ICollection<string> keys = service?.Split?.Allocations?.Keys;
            IEnumerable<string> versionIds = versions
                .Where(x => !keys.Contains(x.Id))
                .Select(x => x.Id);
            return new ObservableCollection<string>(versionIds);
        }

        /// <summary>
        /// Called when the user clicks the Add Traffic Version button.  This will add the
        /// currently selected version (<seealso cref="VersionId"/>>) and allocation
        /// <seealso cref="TrafficAllocation"/> to the list of <seealso cref="Allocations"/> and
        /// removes the version from the the list of <seealso cref="AvailableVersions"/>.
        /// </summary>
        private void OnAddTrafficAllocationCommand()
        {
            SplitTrafficModel split = new SplitTrafficModel(VersionId, TrafficAllocation);
            Allocations.Add(split);
            AvailableVersions.Remove(VersionId);

            // Reset the version and traffic
            VersionId = null;
            TrafficAllocation = 0;

            // Ensure the 'HasAvailableVersions' is up to date so the view state is up to date.
            RaisePropertyChanged(nameof(HasAvailableVersions));
        }

        /// <summary>
        /// Called when the user clicks the Dlete button.  This will remove the
        /// given version (<seealso cref="VersionId"/>>) from the list of
        /// <seealso cref="Allocations"/> and adds the version to the the list
        /// of <seealso cref="AvailableVersions"/>.
        /// </summary>
        private void OnDeleteCommand(SplitTrafficModel sender)
        {
            Allocations.Remove(sender);
            AvailableVersions.Add(sender.VersionId);

            // Ensure the 'HasAvailableVersions' is up to date so the view state is up to date.
            RaisePropertyChanged(nameof(HasAvailableVersions));
        }

        /// <summary>
        /// Called when the user clicks the Save button.  This handles error checks, 
        /// populates the <seealso cref="Result"/> field and closes the dialog.
        /// </summary>
        private void OnSaveCommand()
        {
            // Build the set of allocations.
            IDictionary<string, double?> allocations = new Dictionary<string, double?>();
            int sum = 0;
            foreach (var allocation in Allocations)
            {
                // Ensure each allocation have a valid traffic allocation percent.
                int percent = allocation.TrafficAllocation;
                if (percent <= 0 || percent > 100)
                {
                    // TODO(talarico): Use form handler to detect errors before the user clicks.
                    UserPromptUtils.ErrorPrompt(
                        Resources.SplitTrafficWindowInvalidPercentRangeErrorMessage,
                        Resources.SplitTrafficWindowInvalidPercentTitle);
                    return;
                }
                sum += percent;

                double doublePercent = Math.Round((double)allocation.TrafficAllocation / 100, 2);
                allocations.Add(allocation.VersionId, doublePercent);
            }

            // Ensure that 100% of traffic is allocated.
            if (sum != 100)
            {
                // TODO(talarico): Use form handler to detect errors before the user clicks.
                UserPromptUtils.ErrorPrompt(
                    Resources.SplitTrafficWindowInvalidPercentSumErrorMessage,
                    Resources.SplitTrafficWindowInvalidPercentTitle);
                return;
            }

            string shardBy = IsIpChecked ? GaeServiceExtensions.ShardByIp : GaeServiceExtensions.ShardByCookie;
            Result = new SplitTrafficChange(allocations, shardBy);
            _owner.Close();
        }
    }
}
