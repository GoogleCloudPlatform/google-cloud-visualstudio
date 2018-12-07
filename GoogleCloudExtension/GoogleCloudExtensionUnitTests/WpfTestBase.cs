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

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Threading;
using Moq;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace GoogleCloudExtensionUnitTests
{

    /// <summary>
    /// Base class for testing WPF windows.
    /// </summary>
    /// <typeparam name="TWindow">The type of window being tested.</typeparam>
    public abstract class WpfTestBase<TWindow> : MockedGlobalServiceProviderTestsBase where TWindow : Window
    {
        private JoinableTaskFactory JoinableTaskFactory { get; } =
            AssemblyInitialize.JoinableApplicationContext.Factory;

        [TestInitialize]
        public void InitWpfServiceProvider()
        {
            // Allow previous windows to die before bringing up a new one.
            Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);

            Mock<IVsSettingsManager> settingsManagerMock =
                ServiceProviderMock.SetupService<SVsSettingsManager, IVsSettingsManager>();

            // ReSharper disable once RedundantAssignment
            var intValue = 0;
            // ReSharper disable once RedundantAssignment
            var store = Mock.Of<IVsSettingsStore>(
                ss => ss.GetIntOrDefault(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), out intValue) == 0);
            settingsManagerMock.Setup(sm => sm.GetReadOnlySettingsStore(It.IsAny<uint>(), out store)).Returns(0);

            ServiceProviderMock.SetupService<IVsUIShell, IVsUIShell>();

            // Reset the service provider in an internal microsoft class.
            Type windowHelper = typeof(Microsoft.Internal.VisualStudio.PlatformUI.WindowHelper);
            PropertyInfo serviceProviderProperty =
                windowHelper.GetProperty("ServiceProvider", BindingFlags.NonPublic | BindingFlags.Static);
            Debug.Assert(serviceProviderProperty != null);
            serviceProviderProperty.SetMethod.Invoke(null, new object[] { null });

            // Set the global service provider.
            //            RunPackageInitalize();
        }

        /// <summary>
        /// Gets the window.
        /// </summary>
        /// <param name="promptAction">The action used to bring up the window.</param>
        /// <returns>The window brought up.</returns>
        /// <remarks>
        /// If this method returns null and the window is visible, it is likely because the event handlers have not
        /// been set up properly.
        /// </remarks>
        protected async Task<TWindow> GetWindowAsync(Action promptAction)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            TWindow window = null;

            void OnPromptInitialized(object sender, EventArgs args)
            {
                window = (TWindow)sender;
                window.Close();
            }

            RegisterActivatedEvent(OnPromptInitialized);
            try
            {
                promptAction();
                return window;
            }
            finally
            {
                UnregisterActivatedEvent(OnPromptInitialized);
            }
        }

        /// <summary>
        /// Gets the result of the prompt action.
        /// </summary>
        /// <param name="closeAction">The action used to close the window.</param>
        /// <param name="promptAction">The action used to bring up the window.</param>
        /// <returns>The result of the prompt action.</returns>
        /// <remarks>
        /// If this method returns null and the window is visible, it is likely because the event handlers have not
        /// been set up properly.
        /// </remarks>
        protected async Task<TResult> GetResult<TResult>(Action<TWindow> closeAction, Func<TResult> promptAction)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            void OnPromptInitialized(object sender, EventArgs args) => closeAction((TWindow)sender);

            RegisterActivatedEvent(OnPromptInitialized);
            try
            {
                return promptAction();
            }
            finally
            {
                UnregisterActivatedEvent(OnPromptInitialized);
            }
        }

        /// <summary>
        /// Implementers must register the given handler to an event that is fired when the window to test is activated.
        /// </summary>
        /// <param name="handler">The event handler that will close the window.</param>
        protected abstract void RegisterActivatedEvent(EventHandler handler);

        /// <summary>
        /// Implementers must use this to unregister the given handler from the event registered in
        /// <see cref="RegisterActivatedEvent"/>.
        /// </summary>
        /// <param name="handler">The event handler to unregister from the event.</param>
        protected abstract void UnregisterActivatedEvent(EventHandler handler);
    }
}