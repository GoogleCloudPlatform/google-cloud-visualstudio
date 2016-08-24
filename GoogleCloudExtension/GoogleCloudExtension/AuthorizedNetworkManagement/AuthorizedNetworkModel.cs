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

using Google.Apis.SQLAdmin.v1beta4.Data;
using GoogleCloudExtension.Utils;
using System.Windows.Input;

namespace GoogleCloudExtension.AuthorizedNetworkManagement
{
    /// <summary>
    /// This class is the model for the an authorized network (<seealso cref="AclEntry"/>>)
    /// for a Cloud SQL instance.
    /// </summary>
    public class AuthorizedNetworkModel : Model
    {
        private readonly AclEntry _acl;
        private readonly bool _newNetwork;
        private bool _toBeDeleted;

        /// <summary>
        /// The name of the authorized network.  This is optional.
        /// </summary>
        public string Name => _acl.Name;

        /// <summary>
        /// The value of the authorized network.
        /// This should be an IPv4 address in CIDR notation.
        /// </summary>
        public string Value => _acl.Value;

        /// <summary>
        /// A command to mark this instance as deleted.
        /// </summary>
        public ICommand DeleteCommand { get; }

        /// <summary>
        /// A command to unmark this instance as deleted.
        /// </summary>
        public ICommand UndoDeleteCommand { get; }

        /// <summary>
        /// True if this instance is to be deleted.
        /// </summary>
        public bool Deleted
        {
            get { return _toBeDeleted; }
            set
            {
                SetValueAndRaise(ref _toBeDeleted, value);
                RaisePropertyChanged(nameof(NotDeleted));
                RaisePropertyChanged(nameof(NewOrUpdated));
            }
        }

        /// <summary>
        /// Negation of <seealso cref="Deleted"/> to ease WPF files.
        /// </summary>
        public bool NotDeleted => !_toBeDeleted;

        public string DisplayString => ToString();

        /// <summary>
        /// True if this network is new or scheduled to be deleted, meaning the state
        /// of it is not saved.
        /// </summary>
        public bool NewOrUpdated => _newNetwork || Deleted;

        public AuthorizedNetworkModel(AclEntry acl, bool newNetwork = false)
        {
            _acl = acl;
            _newNetwork = newNetwork;
            _toBeDeleted = false;

            DeleteCommand = new WeakCommand(OnDeleteCommand);
            UndoDeleteCommand = new WeakCommand(OnUndoDeleteCommand);
        }

        /// <summary>
        /// Sets the <seealso cref="Deleted"/> state to true.
        /// </summary>
        private void OnDeleteCommand()
        {
            Deleted = true;
        }

        /// <summary>
        /// Sets the <seealso cref="Deleted"/> state to false.
        /// </summary>
        private void OnUndoDeleteCommand()
        {
            Deleted = false;
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Name) ? Value : $"{Name} ({Value})";
        }
    }
}
