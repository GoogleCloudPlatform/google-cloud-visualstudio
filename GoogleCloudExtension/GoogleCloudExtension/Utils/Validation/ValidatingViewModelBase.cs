// Copyright 2017 Google Inc. All Rights Reserved.
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

using Microsoft.VisualStudio.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.Utils.Validation
{
    /// <summary>
    /// This class helps with validation. It provides a default implementation of the INotifyDataErrorInfo.
    /// </summary>
    public abstract class ValidatingViewModelBase : ViewModelBase, INotifyDataErrorInfo
    {
        /// <summary>
        /// The delay between detecting a validation error and displaying it.
        /// This delay will be reset as the user continues to change the input.
        /// </summary>
        protected internal int MillisecondsDelay { private get; set; } = 500;

        /// <summary>
        /// The map of validation errors that continue to exist after the delay.
        /// </summary>
        private readonly Dictionary<string, IList<ValidationResult>> _validationsMap =
            new Dictionary<string, IList<ValidationResult>>();

        /// <summary>
        /// The map of validation errors.
        /// </summary>
        private readonly Dictionary<string, IList<ValidationResult>> _pendingResultsMap =
            new Dictionary<string, IList<ValidationResult>>();

        /// <summary>
        /// True if any of the pending validations are not valid.
        /// </summary>
        public bool HasErrors => _pendingResultsMap.Values.SelectMany(results => results).Any(v => !v.IsValid);

        /// <summary>
        /// Triggered when validations change after the delay.
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>
        /// Exposes the validation delay task for testing purposes given that the
        /// validation method is (needs to be) async void.
        /// </summary>
        internal Task ValidationDelayTask { get; private set; }

        protected ValidatingViewModelBase()
        {
            ValidationDelayTask = TplExtensions.CompletedTask;
        }

        /// <summary>
        /// Gets the validation errors for the given property that have passed the delay.
        /// </summary>
        /// <param name="propertyName">The name of the property to get the validation errors for.</param>
        /// <returns>A readonly list of validation error for the property.</returns>
        public IEnumerable<ValidationResult> GetErrors(string propertyName)
        {
            if (propertyName == null)
            {
                return _validationsMap.Values.SelectMany(r => r);
            }
            else if (_validationsMap.ContainsKey(propertyName))
            {
                return new ReadOnlyCollection<ValidationResult>(_validationsMap[propertyName]);
            }
            else
            {
                return Enumerable.Empty<ValidationResult>();
            }
        }

        protected void SetAndRaiseWithValidation<T>(
            ref T storage,
            T value,
            IEnumerable<ValidationResult> validations,
            [CallerMemberName] string propertyName = "")
        {
            SetValueAndRaise(ref storage, value, propertyName);
            SetValidationResults(validations, propertyName);
        }

        /// <summary>
        /// Schedules validation results for a given property.
        /// </summary>
        /// <param name="validations">The validation results to set for the property.</param>
        /// <param name="property">The name of the property to set. Defaults to the caller member name.</param>
        protected void SetValidationResults(
            IEnumerable<ValidationResult> validations,
            [CallerMemberName] string property = "")
        {
            property.ThrowIfNullOrEmpty(nameof(property));
            List<ValidationResult> validationResults;
            if (validations == null)
            {
                validationResults = new List<ValidationResult>();
            }
            else
            {
                validationResults = validations.ToList();
            }
            _pendingResultsMap[property] = validationResults;
            HasErrorsChangedInternal();
            ScheduleUpdateErrors(property, validationResults);
        }

        private async void ScheduleUpdateErrors(string property, List<ValidationResult> validationResults)
        {
            if (validationResults.Any(r => !r.IsValid))
            {
                ValidationDelayTask = Task.Delay(MillisecondsDelay);
                await ValidationDelayTask;
            }
            if (validationResults.Equals(_pendingResultsMap[property]))
            {
                _validationsMap[property] = _pendingResultsMap[property];
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
            }
        }

        private void HasErrorsChangedInternal()
        {
            RaisePropertyChanged(nameof(HasErrors));
            HasErrorsChanged();
        }

        /// <summary>
        /// Callback function for when HasErrors may have changed.
        /// </summary>
        protected virtual void HasErrorsChanged() { }

        /// <inheritdoc />
        IEnumerable INotifyDataErrorInfo.GetErrors(string propertyName)
        {
            return GetErrors(propertyName);
        }
    }
}
