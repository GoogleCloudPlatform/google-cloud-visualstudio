namespace GoogleCloudExtension.DataSources
{
    public class LocationName
    {
        public string DisplayName { get; }

        public string Location { get; }

        public LocationName(string displayName, string location)
        {
            DisplayName = displayName;
            Location = location;
        }
    }
}