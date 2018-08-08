// Copyright 2018 Google Inc. All Rights Reserved.
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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension
{
    public static class RegisterUIFactoriesExtensions
    {
        public static async Task<int> RegisterUIFactoryAsync<T>(
            this IVsRegisterUIFactories registerUIFactories,
            T factory,
            CancellationToken token) where T : IVsUIFactory
        {
            await GoogleCloudExtensionPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync(token);
            Guid guid = typeof(T).GUID;
            return ErrorHandler.ThrowOnFailure(registerUIFactories.RegisterUIFactory(ref guid, factory));
        }
    }
}