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
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.FirewallManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static GoogleCloudExtension.Utils.ArgumentCheckUtils;

namespace GoogleCloudExtension.AttachDebuggerDialog
{
    /// <summary>
    /// This class encapsulates firewall port operations used by the attaching debugger feature.
    /// </summary>
    public class AttachDebuggerFirewallPort
    {
        private static readonly TimeSpan ConnectivityTestTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan FirewallRuleWaitMaxTime = TimeSpan.FromMinutes(5);
        private readonly Instance _gceInstance;
        private readonly Lazy<GceDataSource> _lazyDataSource;
        private GceDataSource _dataSource => _lazyDataSource.Value;
        private DateTime _portEnabledTime = DateTime.MinValue;

        /// <summary>
        /// Gets the port information.
        /// </summary>
        public PortInfo PortInfo { get; }

        public AttachDebuggerFirewallPort(PortInfo portInfo, Instance gceInstance, Lazy<GceDataSource> lazyDataSource)
        {
            PortInfo = portInfo.ThrowIfNull(nameof(portInfo));
            _gceInstance = gceInstance.ThrowIfNull(nameof(gceInstance));
            _lazyDataSource = lazyDataSource.ThrowIfNull(nameof(lazyDataSource));
        }

        /// <summary>
        /// Add firewall rule to unblock the port to the GCE instance.
        /// </summary>
        public async Task EnablePort()
        {
            string portTag = PortInfo.GetTag(_gceInstance);

            List<FirewallPort> toEnable = new List<FirewallPort>() { new FirewallPort(portTag, PortInfo.Port) };
            var operation = _dataSource.UpdateInstancePorts(
                _gceInstance,
                portsToEnable: toEnable,
                portsToDisable: new List<FirewallPort>());
            await operation.OperationTask;

            _portEnabledTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Test if we should continue to wait for firewall rule to take effect.
        /// </summary>
        public bool ShouldWaitForFirewallRule()
            => (DateTime.UtcNow - _portEnabledTime) < FirewallRuleWaitMaxTime;

        /// <summary>
        /// Check if GCE firewall rules include a rule that enables the port to target GCE VM.
        /// A firewall rule contains tag, 
        /// if the GCE instance also has the tag, the rule is applied to the GCE instance.
        /// </summary>
        public async Task<bool> IsPortEnabled()
        {
            string portTag = PortInfo.GetTag(_gceInstance);

            // If the instance does not contain the tag, the firewall rule is not set.
            if (_gceInstance.Tags?.Items?.Contains(portTag) ?? false)
            {
                var rules = await _dataSource.GetFirewallListAsync();
                foreach (var rule in rules)
                {
                    // Left oprand is nullable bool.
                    if (rule.TargetTags?.Contains(portTag) ?? false)
                    {
                        continue;   // Skip, rules does not contain the tag.
                    }
                    foreach (var allowed in rule.Allowed)
                    {
                        if (allowed.IPProtocol == "tcp" && allowed.Ports.Any(y => y == PortInfo.Port.ToString()))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if current machine can establish TCP connection to the remote applicaton.
        /// The test succeeds only if 
        ///   (a) The network is connected (i.e no firewall blocks it),
        ///   (b) The target application is started and listening at the TCP port.
        /// </summary>
        /// <returns>
        /// True: Local machine is able to connect to the target GCE VM at the port number.
        /// False: Failed to connect.
        /// </returns>
        public async Task<bool> ConnectivityTest()
        {
            using (TcpClient client = new TcpClient())
            {
                Exception exception = null;
                Func<Task<bool>> connectAsync = async () =>
                {
                    try
                    {
                        await client.ConnectAsync(_gceInstance.GetPublicIpAddress(), PortInfo.Port);
                        Debug.WriteLine("ConnectivityTest, Succeeded");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        var socketError = ex as SocketException;
                        Debug.WriteLine($"ConnectivityTest {socketError?.ErrorCode}, {ex}");
                        if (socketError?.SocketErrorCode == SocketError.ConnectionRefused 
                            || socketError?.SocketErrorCode == SocketError.TimedOut)
                        {
                            return false;
                        }
                        exception = ex;
                        return false;
                    }
                };

                var connectTask = connectAsync();
                // Please note, when it times out before connect operation completes, 
                // this exits the using clause defined above, and the TcpClient is closed.
                var retTask = await Task.WhenAny(connectTask, Task.Delay(ConnectivityTestTimeout));
                if (retTask != connectTask)
                {
                    Debug.WriteLine("ConnectivityTest, timed out");
                    return false;
                }

                if (exception != null)
                {
                    throw exception;
                }
                return connectTask.IsCompleted && connectTask.Result;
            }
        }
    }
}
