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

using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// View model for stackdriver log viewer <seealso cref="DateTimePicker"/>
    /// </summary>
    public class DateTimePickerViewModel : ViewModelBase
    {
        private TimeSpan _uiElementTime;

        /// <summary>
        /// The current selected time zone. 
        /// </summary>
        private TimeZoneInfo _timeZone;

        /// <summary>
        /// The date that binds to the calendar control and the date textbox.
        /// </summary>
        private DateTime _uiElementDate;

        /// <summary>
        /// The selected item index of the "before/after" combo box.
        /// </summary>
        private int _uiSelectedOrderIndex;

        /// <summary>
        /// The boolean data that binds to the IsDropDown property of the ComboBox. 
        /// </summary>
        private bool _isDropDownOpen = false;

        /// <summary>   
        /// Gets or sets if using descending order by timestamp. 
        /// true: descending order,  false: ascending order.
        /// When the Go button is clicked, the value is set to the UI selection.
        /// While the cancel button click event won't change this value.
        /// </summary>
        public bool IsDescendingOrder { get; set; }

        /// <summary>
        /// Gets or sets the datetime in UTC.  
        /// The current active date time.
        /// When Go button is clicked, the value is set to the UI selection.
        /// While cancel button click event won't change this value.
        /// </summary>
        public DateTime DateTimeUtc { get; set; }

        /// <summary>
        /// The command to execute to accept the changes, binding to Go button.
        /// </summary>
        public ProtectedCommand GoCommand { get; }

        /// <summary>
        /// The command binding to Cancel button.
        /// </summary>
        public ProtectedCommand CancelCommand { get; }

        public TimeSpan UiElementTime
        {
            get { return _uiElementTime; }
            set { SetValueAndRaise(ref _uiElementTime, value); }
        }

        /// <summary>
        /// Gets or sets the UI control date box value of <seealso cref="_timeZone"/>
        /// </summary>
        public DateTime UiElementDate
        {
            get { return _uiElementDate; }
            set
            {
                _uiElementDate = value > Now ? Now : value;
                RaisePropertyChanged(nameof(UiElementDate));
            }
        }

        /// <summary>
        /// Gets or sets the selected order index of the combo box.
        /// </summary>
        public int SelectedOrderIndex
        {
            get { return _uiSelectedOrderIndex; }
            set { SetValueAndRaise(ref _uiSelectedOrderIndex, value); }
        }

        /// <summary>
        /// The event handler that notifies if the date time or the time order is changed.
        /// </summary>
        public event EventHandler DateTimeFilterChange;

        /// <summary>
        /// Gets or sets if the combo box drop down is open.
        /// </summary>
        public bool IsDropDownOpen
        {
            get { return _isDropDownOpen; }
            set
            {
                SetValueAndRaise(ref _isDropDownOpen, value);

                Debug.WriteLine($"Set IsDropDownOpen {value}");
                if (_isDropDownOpen)
                {
                    UpdateViewData();
                }
            }
        }

        /// <summary>
        /// Create a instance of <seealso cref="DateTimePickerViewModel"/> class.
        /// </summary>
        /// <param name="timeZone">Current selected time zone.</param>
        /// <param name="dateTimeUtc">Initial UTC date time.</param>
        /// <param name="isDescendingOrder">Initial time stamp sort order.</param>
        public DateTimePickerViewModel(TimeZoneInfo timeZone, DateTime dateTimeUtc, bool isDescendingOrder)
        {
            _timeZone = timeZone;
            DateTimeUtc = dateTimeUtc;
            IsDescendingOrder = isDescendingOrder;
            GoCommand = new ProtectedCommand(OnGoButtonCommand);
            CancelCommand = new ProtectedCommand(() => IsDropDownOpen = false);
        }

        /// <summary>
        /// Change the control time zone.
        /// </summary>
        /// <param name="timeZone">The time zone to be changed to.</param>
        public void ChangeTimeZone(TimeZoneInfo timeZone)
        {
            _timeZone = timeZone;
        }

        /// <summary>
        /// When dropdown is open, update view data from model.
        /// </summary>
        private void UpdateViewData()
        {
            DateTime dt = TimeZoneInfo.ConvertTimeFromUtc(DateTimeUtc, _timeZone);
            UiElementDate = dt.Date;
            UiElementTime = dt.TimeOfDay;
            SelectedOrderIndex = IsDescendingOrder ? 0 : 1;
        }

        /// <summary>
        /// When the GO button is clicked, update the model data.
        /// </summary>
        private void SetDataFromView()
        {
            DateTime newDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(UiElementDate.Date.Add(UiElementTime));
            bool newOrder = _uiSelectedOrderIndex == 0;

            if (DateTimeUtc != newDateTimeUtc || newOrder != IsDescendingOrder)
            {
                Debug.WriteLine("OnChangedDateTime Invoke DateTimeFilterChange, changed");
                DateTimeUtc = newDateTimeUtc;
                IsDescendingOrder = newOrder;
                DateTimeFilterChange?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Go button command handler.
        /// </summary>
        private void OnGoButtonCommand()
        {
            SetDataFromView();
            IsDropDownOpen = false;
        }

        /// <summary>
        /// The current time of the  <seealso cref="_timeZone"/>.
        /// </summary>
        private DateTime Now
        {
            get { return TimeZoneInfo.ConvertTime(DateTime.UtcNow, _timeZone); }
        }
    }
}
