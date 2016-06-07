namespace GoogleCloudExtension.DataSources
{
    public class FirewallPort
    {
        public string Name { get; }

        public int Port { get; }

        public FirewallPort(string name, int port)
        {
            Name = name;
            Port = port;
        }
    }
}
