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

using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogleCloudExtension.AddTrafficSplit
{
    /// <summary>
    /// The view model for the <seealso cref="AddTrafficSplitWindow"/> dialog.
    /// </summary>
    public class AddTrafficSplitViewModel : ValidatingViewModelBase
    {
        private readonly AddTrafficSplitWindow _owner;
        private string _selectedVersion;
        private string _allocation = "0";

        /// <summary>
        /// The currently selected version.
        /// </summary>
        public string SelectedVersion
        {
            get { return _selectedVersion; }
            set { SetValueAndRaise(ref _selectedVersion, value); }
        }

        /// <summary>
        /// The list of versions that can be selected.
        /// </summary>
        public IEnumerable<string> Versions { get; }

        /// <summary>
        /// The allocation chosen for the version.
        /// </summary>
        public string Allocation
        {
            get { return _allocation; }
            set { SetAndRaiseWithValidation(ref _allocation, value, ValidateAllocation(value)); }
        }

        /// <summary>
        /// The command to execute to commit the changes.
        /// </summary>
        public ICommand AddSplitCommand { get; }

        /// <summary>
        /// The result from the dialog.
        /// </summary>
        public AddTrafficSplitResult Result { get; private set; }

        public AddTrafficSplitViewModel(AddTrafficSplitWindow owner, IEnumerable<string> versions)
        {
            _owner = owner;

            Versions = versions;
            SelectedVersion = Versions.FirstOrDefault();
            AddSplitCommand = new ProtectedCommand(OnAddSplitCommand);
        }

        private void OnAddSplitCommand()
        {
            if (!Validate())
            {
                return;
            }

            Result = new AddTrafficSplitResult(
                version: SelectedVersion,
                allocation: Int32.Parse(Allocation));
            _owner.Close();
        }

        private IEnumerable<ValidationResult> ValidateAllocation(string value)
        {
            int allocationValue;
            if (!int.TryParse(value, out allocationValue))
            {
                yield return StringValidationResult.FromResource(nameof(Resources.AddGaeTrafficSplitInvalidValueMessage), value);
            }
            else if (allocationValue > 100 || allocationValue < 0)
            {
                yield return StringValidationResult.FromResource(nameof(Resources.AddGaeTrafficSplitValueOutOfRangeMessage), value);
            }
        }

        private bool Validate()
        {
            int allocationValue;
            if (!Int32.TryParse(Allocation, out allocationValue))
            {
                UserPromptUtils.ErrorPrompt(
                    message: String.Format(Resources.AddGaeTrafficSplitInvalidValueMessage, Allocation),
                    title: Resources.AddGaeTrafficSplitInvalidValueTitle);
                return false;
            }

            if (allocationValue > 100 || allocationValue < 0)
            {
                UserPromptUtils.ErrorPrompt(
                    message: String.Format(Resources.AddGaeTrafficSplitValueOutOfRangeMessage, Allocation),
                    title: Resources.AddGaeTrafficSplitInvalidValueTitle);
                return false;
            }

            return true;
        }
    }
}
