
namespace GoogleCloudExtension.GcsUtils
{
    public class GcsMoveFileOperation : GcsOperation
    {
        /// <summary>
        /// The reference to the GCS file that is going to be renamed.
        /// </summary>
        public override GcsItemRef GcsItem { get; }

        /// <summary>
        /// The reference to the new name to use for the file.
        /// </summary>
        public GcsItemRef ToItem { get; }

        public GcsMoveFileOperation(GcsItemRef fromItem, GcsItemRef toItem)
        {
            GcsItem = fromItem;
            ToItem = toItem;
        }
    }
}
