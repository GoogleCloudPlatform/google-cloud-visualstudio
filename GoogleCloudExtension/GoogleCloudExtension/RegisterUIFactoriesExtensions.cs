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
    /// <summary>
    /// Extension methods for the <see cref="IVsRegisterUIFactories"/> interface.
    /// </summary>
    /// <remarks>
    /// These method ensures the methods of <see cref="IVsRegisterUIFactories"/>
    /// are properly called on the main thread.
    /// </remarks>
    public static class RegisterUIFactoriesExtensions
    {
        /// <summary>
        /// Registers the UI factory.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the factory being registered.
        /// The <see cref="Type.GUID"/> of this type is used as the identifier for the new factory.
        /// </typeparam>
        /// <param name="registerUIFactories">
        /// The instance of <see cref="IVsRegisterUIFactories"/> used to register the new factory.
        /// </param>
        /// <param name="factory">The factory object to register.</param>
        /// <param name="token">
        /// The <see cref="CancellationToken"/> that can cancel switching to the main thread.
        /// </param>
        /// <returns>Returns <see cref="VSConstants.S_OK"/>.</returns>
        /// <seealso cref="IVsRegisterUIFactories.RegisterUIFactory"/>
        public static async Task RegisterUIFactoryAsync<T>(
            this IVsRegisterUIFactories registerUIFactories,
            T factory,
            CancellationToken token) where T : IVsUIFactory
        {
            await GoogleCloudExtensionPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync(token);
            Guid guid = typeof(T).GUID;
            ErrorHandler.ThrowOnFailure(registerUIFactories.RegisterUIFactory(ref guid, factory));
        }
    }
}