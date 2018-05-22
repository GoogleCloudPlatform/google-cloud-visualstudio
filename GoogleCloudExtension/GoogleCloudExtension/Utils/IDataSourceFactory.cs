using GoogleCloudExtension.DataSources;

namespace GoogleCloudExtension.Utils
{
    public interface IDataSourceFactory
    {
        ResourceManagerDataSource CreateResourceManagerDataSource();

        IGPlusDataSource CreatePlusDataSource();
    }
}