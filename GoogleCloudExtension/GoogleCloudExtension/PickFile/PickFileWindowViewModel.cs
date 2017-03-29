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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace GoogleCloudExtension.PickFile
{
    public class PickFileWindowViewModel : ViewModelBase
    {
        private int _selectedIndex;
        private readonly IEnumerable<string> _fileList;
        private PickFileWindow _owner;

        /// <summary>
        /// Gets the list of files.
        /// </summary>
        public IEnumerable<string> FileList => _fileList;

        /// <summary>
        /// Gets or sets the selected file item.
        /// </summary>
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set { SetValueAndRaise(ref _selectedIndex, value); }
        }

        public ProtectedCommand SelectFileCommand { get; }

        public int Result { get; private set; }

        public PickFileWindowViewModel(PickFileWindow owner, IEnumerable<string> fileList)
        {
            _owner = owner.ThrowIfNull(nameof(owner));
            if (fileList?.Count() < 2)
            {
                throw new ArgumentException($"{nameof(fileList)} is null or count is less than 2.");
            }
            _fileList = fileList;
            SelectedIndex = 0;
            Result = -1;
            SelectFileCommand = new ProtectedCommand(OnSelectFileCommand);
        }

        private void OnSelectFileCommand()
        {
            Result = SelectedIndex;
            _owner.Close();
        }
    }
}
