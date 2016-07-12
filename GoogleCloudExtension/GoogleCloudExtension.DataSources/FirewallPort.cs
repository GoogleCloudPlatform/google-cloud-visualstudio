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

using System;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// The protocols for the port.
    /// </summary>
    public enum PortProtocol
    {
        Tcp,
        Udp,
    }

    /// <summary>
    /// This class represents a port that needs to be opened in the firewall.
    /// </summary>
    public class FirewallPort
    {
        /// <summary>
        /// The name to use for the port rule and port tags.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The port number.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// The port protocol.
        /// </summary>
        public PortProtocol Protocol { get; }

        internal string ProtocolString
        {
            get
            {
                switch (Protocol)
                {
                    case PortProtocol.Tcp:
                        return "tcp";

                    case PortProtocol.Udp:
                        return "udp";

                    default:
                        throw new InvalidOperationException($"Unknown protocol {Protocol}");
                }
            }
        }

        public FirewallPort(string name, int port, PortProtocol protocol = PortProtocol.Tcp)
        {
            Name = name;
            Port = port;
            Protocol = protocol;
        }
    }
}
