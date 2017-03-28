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
        private string _selected;
        private readonly List<string> _fileList;
        private PickFileWindow _owner;

        /// <summary>
        /// Gets the list of files.
        /// </summary>
        public List<string> FileList => _fileList;

        /// <summary>
        /// Gets or sets the selected file item.
        /// </summary>
        public string Selected
        {
            get { return _selected; }
            set { SetValueAndRaise(ref _selected, value); }
        }

        public ProtectedCommand SelectFileCommand { get; }

        public string Result { get; private set; }

        public PickFileWindowViewModel(PickFileWindow owner, List<string> fileList)
        {
            _owner = owner;
            if (fileList?.Count <= 0)
            {
                throw new ArgumentException($"{nameof(fileList)} is null or empty.");
            }
            _fileList = fileList;
            Selected = fileList.FirstOrDefault();
            SelectFileCommand = new ProtectedCommand(OnSelectFileCommand);
        }

        private void OnSelectFileCommand()
        {
            Result = Selected;
            _owner.Close();
        }
    }
}
