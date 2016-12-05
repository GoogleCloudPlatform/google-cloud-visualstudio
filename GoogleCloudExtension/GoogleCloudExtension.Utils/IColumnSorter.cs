namespace GoogleCloudExtension.Utils
{
    public interface IColumnSorter
    {
        int Compare(object x, object y, bool descending);
    }
}
