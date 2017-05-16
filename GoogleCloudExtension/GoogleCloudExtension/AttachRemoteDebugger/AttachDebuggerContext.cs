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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.FirewallManagement;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Controls;
using static GoogleCloudExtension.Utils.ArgumentCheckUtils;

namespace GoogleCloudExtension.AttachRemoteDebugger
{
    /// <summary>
    /// This class contains common data, methods for attach remote debugger steps.
    /// </summary>
    internal class AttachDebuggerContext
    {
        private const int ConnectivityTestTimeout = 2000;   // In milliseconds
        private const int FirewallRuleWaitMaxTime = 5 * 60;    // 5 minutes

        private static Lazy<AttachDebuggerContext> s_Instance;

        private readonly Lazy<GceDataSource> _dataSource;
        private readonly Dictionary<int, DateTime> _lastPortEnableTime = new Dictionary<int, DateTime>();

        /// <summary>
        /// Singleton instance of <seealso cref="AttachDebuggerContext"/>.
        /// The object is valid when <seealso cref="AttachDebuggerWindow"/> is opened.
        /// </summary>
        public static AttachDebuggerContext Context => s_Instance?.Value;

        /// <summary>
        /// Gets the <seealso cref="DataSource"/> object.
        /// </summary>
        public GceDataSource DataSource => _dataSource.Value;

        /// <summary>
        /// The attaching dialog window that is the container of all steps.
        /// </summary>
        public AttachDebuggerWindow DialogWindow { get; }

        /// <summary>
        /// The GCE VM <seealso cref="Instance"/> object.
        /// </summary>
        public Instance GceInstance { get; private set; }

        /// <summary>
        /// The GCE VM instance public ip address.
        /// </summary>
        public string PublicIp { get; }

        /// <summary>
        /// Create the <seealso cref="Context"/> to associate the <paramref name="gceInstance"/>.
        /// </summary>
        /// <param name="gceInstance">GCE VM instance object.</param>
        /// <param name="dialogWindow">The dialog window</param>
        public static void CreateContext(Instance gceInstance, AttachDebuggerWindow dialogWindow)
        {
            s_Instance = new Lazy<AttachDebuggerContext>(() => new AttachDebuggerContext(gceInstance, dialogWindow));
        }

        /// <summary>
        /// Create a new <seealso cref="IAttachDebuggerStep"/> object.
        /// </summary>
        /// <typeparam name="TStep">The type that implements IAttachDebuggerStep interface.</typeparam>
        /// <typeparam name="TContent">The user control for the step.</typeparam>
        public IAttachDebuggerStep CreateStep<TStep, TContent>() 
            where TStep: IAttachDebuggerStep
            where TContent: UserControl
        {
            var content = CreateObject<TContent>();
            var viewModel = CreateObject<TStep>(content);
            content.DataContext = viewModel;
            return viewModel;
        }

        /// <summary>
        /// Checks if current machine can establish TCP connection to the remote applicaton.
        /// The test succeeds only if 
        ///   (a) The network is connected (i.e no firewall blocks it),
        ///   (b) The target application is started and listening at the TCP port.
        /// </summary>
        /// <param name="port">The target TCP port number</param>
        /// <returns>
        /// True: Local machine is able to connect to the target GCE VM at the port number.
        /// False: Failed to connect.
        /// </returns>
        public async Task<bool> ConnectivityTest(int port)
        {
            using (TcpClient client = new TcpClient())
            {
                try
                {
                    var task = client.ConnectAsync(PublicIp, port);
                    return ((await Task.WhenAny(task, Task.Delay(ConnectivityTestTimeout))) == task);
                }
                catch (SocketException ex)
                {
                    Debug.WriteLine(ex.ToString());
                    if (ex.SocketErrorCode == SocketError.ConnectionRefused || ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        return false;
                    }
                    throw;
                }
            }
        }

        /// <summary>
        /// Add firewall rule to unblock the port to the GCE instance.
        /// </summary>
        /// <param name="portInfo">A <seealso cref="PortInfo"/> object that represents the port.</param>
        public async Task EnablePort(PortInfo portInfo)
        {
            portInfo.ThrowIfNull(nameof(portInfo));
            string portTag = portInfo.GetTag(GceInstance);

            List<FirewallPort> toEnables = new List<FirewallPort>();
            toEnables.Add(new FirewallPort(portTag, portInfo.Port));
            var operation = Context.DataSource.UpdateInstancePorts(
                GceInstance,
                portsToEnable: toEnables,
                portsToDisable: new List<FirewallPort>());
            await operation.OperationTask;

            _lastPortEnableTime.Add(portInfo.Port, DateTime.Now);
            GceInstance = await Context.DataSource.RefreshInstance(GceInstance);
        }

        /// <summary>
        /// Test if we should continue to wait for firewall rule to take effect.
        /// </summary>
        /// <param name="port">The port number to check.</param>
        public bool ShouldWaitForFirewallRule(int port)
        {
            DateTime datetime;
            if (!_lastPortEnableTime.TryGetValue(port, out datetime))
            {
                return false;
            }
            return (DateTime.Now - datetime).TotalSeconds < FirewallRuleWaitMaxTime;
        }

        private AttachDebuggerContext(Instance gceInstance, AttachDebuggerWindow dialogWindow)
        {
            GceInstance = gceInstance.ThrowIfNull(nameof(gceInstance));
            DialogWindow = dialogWindow.ThrowIfNull(nameof(dialogWindow));
            PublicIp = gceInstance.GetPublicIpAddress();
            _dataSource = new Lazy<GceDataSource>(CreateDataSource);
        }

        private static T CreateObject<T>(params object[] args)
        {
            return (T)Activator.CreateInstance(typeof(T), args);
        }

        private static GceDataSource CreateDataSource()
        {
            if (CredentialsStore.Default.CurrentProjectId != null)
            {
                return new GceDataSource(
                    CredentialsStore.Default.CurrentProjectId,
                    CredentialsStore.Default.CurrentGoogleCredential,
                    GoogleCloudExtensionPackage.VersionedApplicationName);
            }
            else
            {
                return null;
            }
        }
    }
}
