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

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// INotifyDataErrorInfo implementation is needed to support data validation in the UI
    /// </summary>
    public partial class ViewModelBase : INotifyDataErrorInfo
    {
        private readonly ConcurrentDictionary<string, List<string>> _errors =
            new ConcurrentDictionary<string, List<string>>();
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        public event EventHandler<EventArgs> ValidationFinished;

        protected bool ValidateOnChanges { get; set; }

        public bool HasErrors => _errors.Any(x => x.Value != null && x.Value.Any());

        public bool IsValid => !HasErrors;

        protected override void SetValueAndRaise<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
        {
            base.SetValueAndRaise(ref storage, value, propertyName);

            if (ValidateOnChanges)
            {
                ValidateProperty(value, propertyName);
            }
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                List<string> errors;
                _errors.TryGetValue(propertyName, out errors);
                return errors;
            }

            return _errors.SelectMany(error => error.Value);
        }

        protected void ValidateProperty<T>(T value, [CallerMemberName] string propertyName = null)
        {
            var validationContext = new ValidationContext(this, null, null)
            {
                MemberName = propertyName
            };

            var validationResults = new List<ValidationResult>();
            Validator.TryValidateProperty(value, validationContext, validationResults);

            List<string> outputList;
            _errors.TryRemove(propertyName, out outputList);
            RaiseErrorsChanged(propertyName);

            HandleValidationResults(validationResults);
            OnValidationFinished();
        }

        protected void Validate()
        {
            var validationContext = new ValidationContext(this, null, null);
            var validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(this, validationContext, validationResults, true);

            foreach (var error in _errors)
            {
                if (validationResults.All(res => res.MemberNames.All(name => name != error.Key)))
                {
                    List<string> outputList;
                    _errors.TryRemove(error.Key, out outputList);
                    RaiseErrorsChanged(error.Key);
                }
            }

            HandleValidationResults(validationResults);
            OnValidationFinished();
        }

        private void HandleValidationResults(IEnumerable<ValidationResult> validationResults)
        {
            var results = from res in validationResults
                          from memberNames in res.MemberNames
                          group res by memberNames into x
                          select x;

            foreach (var group in results)
            {
                var messages = group.Select(r => r.ErrorMessage).ToList();

                if (_errors.ContainsKey(group.Key))
                {
                    List<string> outLi;
                    _errors.TryRemove(group.Key, out outLi);
                }

                _errors.TryAdd(group.Key, messages);
                RaiseErrorsChanged(group.Key);
            }
        }

        private void OnValidationFinished()
        {
            RaisePropertyChanged(nameof(HasErrors));
            RaisePropertyChanged(nameof(IsValid));

            ValidationFinished?.Invoke(this, new EventArgs());
        }

        protected void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
    }
}
