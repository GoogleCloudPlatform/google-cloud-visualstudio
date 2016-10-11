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
using System.Runtime.CompilerServices;

namespace GoogleCloudExtension.PubSubWindows
{
    /// <summary>
    /// Data object that backs the new topic window. Contains necessicary data for creating a new topic.
    /// </summary>
    public class NewTopicViewModel : ViewModelBase
    {
        private string _topicName;

        public string Project { get; }

        public string TopicName
        {
            get { return _topicName; }
            set
            {
                _topicName = value;
                OnPropertyChanged();
            }
        }

        public WeakCommand CreateCommand { get; }

        public NewTopicViewModel(string project, WeakCommand createCommand)
        {
            Project = project;
            CreateCommand = createCommand;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {

            RaisePropertyChanged(propertyName);
        }
    }
}