namespace GoogleCloudExtension.DataSources
{
    public interface IDeleteOperation
    {
        void Completed();

        void Cancelled();

        void Error(DataSourceException ex);
    }
}