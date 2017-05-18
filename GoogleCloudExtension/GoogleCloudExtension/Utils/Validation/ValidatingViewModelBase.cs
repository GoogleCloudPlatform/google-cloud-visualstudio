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
    public abstract class ValidatingViewModelBase : ViewModelBase, INotifyDataErrorInfo
    {
        protected int MillisecondsDelay = 500;

        private readonly Dictionary<string, IList<ValidationResult>> _validationsMap =
            new Dictionary<string, IList<ValidationResult>>();

        private readonly Dictionary<string, IList<ValidationResult>> _pendingResultsMap =
            new Dictionary<string, IList<ValidationResult>>();

        public bool HasErrors => _pendingResultsMap.Values.SelectMany(results => results).Any(v => !v.IsValid);

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IList<ValidationResult> GetErrors(string propertyName)
        {
            if (_validationsMap.ContainsKey(propertyName))
            {
                return new ReadOnlyCollection<ValidationResult>(_validationsMap[propertyName]);
            }
            else
            {
                return null;
            }
        }

        protected async void SetValidationResults(
            IEnumerable<ValidationResult> validations,
            [CallerMemberName] string property = "")
        {
            property.ThrowIfNullOrEmpty(nameof(property));
            var validationResults = validations.ToList();
            _pendingResultsMap[property] = validationResults;
            RaisePropertyChanged(nameof(HasErrors));
            if (validationResults.Any(r => !r.IsValid))
            {
                await Task.Delay(MillisecondsDelay);
            }
            if (validationResults.Equals(_pendingResultsMap[property]))
            {
                _validationsMap[property] = _pendingResultsMap[property];
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
            }
        }

        IEnumerable INotifyDataErrorInfo.GetErrors(string propertyName)
        {
            return GetErrors(propertyName);
        }
    }
}