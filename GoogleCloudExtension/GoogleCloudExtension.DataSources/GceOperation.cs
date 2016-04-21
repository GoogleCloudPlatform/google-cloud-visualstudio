using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// The type of operation.
    /// </summary>
    public enum OperationType
    {
        StopInstance,
        StartInstance,
    }

    /// <summary>
    /// This class represents a an operation on a Google Compute Engine VM instance.
    /// </summary>
    public class GceOperation
    {
        /// <summary>
        /// The type of operation this instance represents.
        /// </summary>
        public OperationType OperationType { get; }

        /// <summary>
        /// The project id on which this operation was created.
        /// </summary>
        public string ProjectId { get; }

        /// <summary>
        /// The zone in which this operation was created.
        /// </summary>
        public string ZoneName { get; }

        /// <summary>
        /// The name for the instance for which this operation is being done.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The task that will be completed once the operaiton is finished.
        /// </summary>
        public Task OperationTask { get; internal set; }

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="operationType"></param>
        /// <param name="projectId"></param>
        /// <param name="zoneName"></param>
        /// <param name="name"></param>
        public GceOperation(OperationType operationType, string projectId, string zoneName, string name)
        {
            OperationType = operationType;
            ProjectId = projectId;
            ZoneName = zoneName;
            Name = name;
        }
    }
}
