
namespace GoogleCloudExtension.GcsUtils
{
    /// <summary>
    /// This class represents an operation to delete a GCS file.
    /// </summary>
    public class GcsDeleteFileOperation : GcsOperation
    {
        /// <summary>
        /// The file to be deleted.
        /// </summary>
        public override GcsItemRef GcsItem { get; }

        public GcsDeleteFileOperation(GcsItemRef gcsItem)
        {
            GcsItem = gcsItem;
        }
    }
}
