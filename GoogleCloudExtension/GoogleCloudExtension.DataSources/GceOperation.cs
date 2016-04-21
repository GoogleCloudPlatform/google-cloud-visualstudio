using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    public enum OperationType
    {
        StopInstance,
        StartInstance,
        StoreMetadata,
    }

    public class GceOperation
    {
        public OperationType OperationType { get; }

        public string ProjectId { get; }

        public string ZoneName { get; }

        public string Name { get; }

        public Task OperationTask { get; internal set; }

        public GceOperation(OperationType operationType, string projectId, string zoneName, string name)
        {
            OperationType = operationType;
            ProjectId = projectId;
            ZoneName = zoneName;
            Name = name;
        }
    }
}
