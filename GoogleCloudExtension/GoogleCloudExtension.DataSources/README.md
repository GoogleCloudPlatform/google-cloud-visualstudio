# GoogleCloudExtension.DataSources
This library implements a set of very simple data sources that allow the extension to access data about Google Cloud Platform services. The library also contains a set of supporting classes used by said data sources.

None of these classes are thread safe, being designed to be used from UI applications and therefore accessed from the UI thread only. All operations are started and awaited from the UI thread only, any other use is not supported.

## Data sources supported
The data sources implemented in the library are:

* `GceDataSource` which implements access to Google Compute Engine instances.
* `GcsDataSource` which implements access to Google Storage buckets.
* `GPlusDataSource` which implement access to the Google Plust API.
* `ResourceManagerDataSource` which implements access to the Resource Manager API.

All of the data sources contain a set of credentials and for those that are against an API that requires a project id they will also contain one for each call.

### GceDataSource class
This class provides access to Google Compoute Engine instances, using the [Google Compute Engine API](https://cloud.google.com/compute/docs/reference/latest/) as the underlying data source.

It allows the implementation of UI that will:
* The list of _all_ the instances in the given project, in all of the various zones supported by the project, using the method `GetInstanceListAsync()`.
* Get the data of a particular instance, using the method `GetInstance()` or refresh the data about a particular instance using the method, `RefreshInstance()`.

### GcsDatasource class
This class gives access to the list of buckets for a particular project, using the [Google Storage JSON API](https://cloud.google.com/storage/docs/json_api/) as the underlying data source.

Only one method is exposed for this class, `GetBucketListAsync()` which returns the list of all buckets in the given project.

### GPlusDataSource class
This class provides access to the Google Plus API using the Google Plus client side library.

### ResourceManagerDataSource class
This class provides access to the [Resource Manager API](https://cloud.google.com/resource-manager/) using the C# client library for that.

## Support classes
### DataSourceBase class
This class provides the base functionality for all data sources, including the `LoadPagedListAsync()` method which implements a very simple paginator for all paginated sources.
