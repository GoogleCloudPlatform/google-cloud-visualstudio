
namespace GoogleCloudExtension.AddTrafficSplit
{
    public class AddTrafficSplitResult
    {
        public string Version { get; }

        public int Allocation { get; }

        public AddTrafficSplitResult(
            string version,
            int allocation)
        {
            Version = version;
            Allocation = allocation;
        }
    }
}
