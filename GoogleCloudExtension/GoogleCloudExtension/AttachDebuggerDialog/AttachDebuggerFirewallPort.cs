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
        private static readonly TimeSpan s_connectivityTestTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_firewallRuleWaitMaxTime = TimeSpan.FromMinutes(5);
        private readonly Lazy<GceDataSource> _lazyDataSource;
        private Instance _gceInstance;
        private GceDataSource _dataSource => _lazyDataSource.Value;
        private DateTime _portEnabledTime = DateTime.MinValue;

        /// <summary>
        /// Gets the port information.
        /// </summary>
        public PortInfo PortInfo { get; }

        /// <summary>
        /// Description of the port.
        /// Can be either Debugger Remote Tool or Remote PowerShell.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Initializes the <seealso cref="AttachDebuggerFirewallPort"/> object.
        /// </summary>
        /// <param name="portInfo">A <seealso cref="PortInfo"/> object that specifies the port.</param>
        /// <param name="description">A description shown during testing connectivity step.</param>
        /// <param name="gceInstance">The GCP Windows VM instance.</param>
        /// <param name="lazyDataSource">The data source object.</param>
        public AttachDebuggerFirewallPort(
            PortInfo portInfo,
            string description,
            Instance gceInstance,
            Lazy<GceDataSource> lazyDataSource)
        {
            PortInfo = portInfo.ThrowIfNull(nameof(portInfo));
            Description = description.ThrowIfNullOrEmpty(nameof(description));
            _gceInstance = gceInstance.ThrowIfNull(nameof(gceInstance));
            _lazyDataSource = lazyDataSource.ThrowIfNull(nameof(lazyDataSource));
        }

        /// <summary>
        /// Add firewall rule to unblock the port to the GCE instance.
        /// </summary>
        public async Task EnablePort()
        {
            // Get a refreshed list of firewall rules. 
            // If not refreshed, UpdateInstancePorts may fail. 
            _gceInstance = await _dataSource.RefreshInstance(_gceInstance);
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
        /// Gets how long to wait for firewall rule to take effect.
        /// There are several cases here. 
        /// 1) Firewall was already enabled, we did not enable it. Then we don't wait.
        /// 2) Need to wait for some time.
        /// 3) Waited for too long, stop waiting. 
        /// Case 1) and 3) , it returns value less than or equal to 0.
        /// case 2, it returns positive value.
        /// </summary>
        public int WaitForFirewallRuleTimeInSeconds()
            => (int)(s_firewallRuleWaitMaxTime - (DateTime.UtcNow - _portEnabledTime)).TotalSeconds;

        /// <summary>
        /// Check if GCE firewall rules include a rule that enables the port to target GCE VM.
        /// If a firewall rule contains tag, 
        /// and the GCE instance also has the tag, the rule is applied to the GCE instance.
        /// </summary>
        public async Task<bool> IsPortEnabled()
        {
            string portTag = PortInfo.GetTag(_gceInstance);

            // If the instance does not contain the tag, the firewall rule is not set.
            if (_gceInstance.Tags?.Items?.Contains(portTag) != true)
            {
                return false;
            }
            var rules = await _dataSource.GetFirewallListAsync();

            var query = rules
                // x is FireWall, test if the rule contains the tag
                .Where(x => x?.TargetTags?.Contains(portTag) ?? false)
                .SelectMany(x => x.Allowed)
                // x is now FireWall.AllowedData
                // Check if the allowed protocol is tcp
                .Where(x => x?.IPProtocol == "tcp" && x.Ports != null)
                .SelectMany(x => x.Ports)
                // x is now port number in string type
                // Check if the allowed port number matches
                .Where(x => x == PortInfo.Port.ToString());
            return query.Any();
        }

        /// <summary>
        /// Checks if current machine can establish TCP connection to the remote applicaton.
        /// The test succeeds only if 
        ///   (a) The network is connected (i.e no firewall blocks it),
        ///   (b) The target application is started and listening at the TCP port.
        /// </summary>
        /// <param name="cancelToken">The cancellation token</param>
        /// <returns>
        /// True: Local machine is able to connect to the target GCE VM at the port number.
        /// False: Failed to connect.
        /// </returns>
        public async Task<bool> ConnectivityTest(CancellationToken cancelToken)
        {
            using (TcpClient client = new TcpClient())
            {
                try
                {
                    var connectTask = client.ConnectAsync(_gceInstance.GetPublicIpAddress(), PortInfo.Port);
                    if (connectTask == await Task.WhenAny(connectTask, Task.Delay(s_connectivityTestTimeout, cancelToken)))
                    {
                        await connectTask;
                        Debug.WriteLine("ConnectivityTest, Succeeded");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine("ConnectivityTest, timed out");
                        return false;
                    }
                }
                catch (SocketException ex)
                {
                    var socketError = ex as SocketException;
                    Debug.WriteLine($"ConnectivityTest {socketError?.ErrorCode}, {ex}");
                    if (socketError?.SocketErrorCode == SocketError.ConnectionRefused
                        || socketError?.SocketErrorCode == SocketError.TimedOut)
                    {
                        return false;
                    }
                    throw;
                }
            }
        }
    }
}
