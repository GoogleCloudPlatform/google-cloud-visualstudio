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

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// This interface encapsulates all of the callbacks for an operation to display progress.
    /// </summary>
    public interface IGcsFileOperation
    {
        /// <summary>
        /// Called to update of the progress so far of the operation.
        /// </summary>
        /// <param name="value">A value between 0.0 to 1.0 representing the fraction of the operation finished.</param>
        void Progress(double value);

        /// <summary>
        /// Called when the operation is completed.
        /// </summary>
        void Completed();

        /// <summary>
        /// Called if the user cancelled the operation.
        /// </summary>
        void Cancelled();

        /// <summary>
        /// Called when the operation failed.
        /// </summary>
        /// <param name="ex">The exception thrown when the operaiton failed.</param>
        void Error(DataSourceException ex);
    }
}
