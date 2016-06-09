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
    /// This class represents a TCP port that needs to be opened in the firewall.
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

        public FirewallPort(string name, int port, PortProtocol protocol = PortProtocol.Tcp)
        {
            Name = name;
            Port = port;
            Protocol = protocol;
        }
    }
}
