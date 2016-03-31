# GoogleCloudExtension.DataSources
This library implements a set of very simple data sources that allow the extension to access data about Google Cloud Platform services. The library also contains a set of supporting classes used by said data sources.

## Data sources supported
The data sources implemented in the library are:

* `GaeDataSource` which implements access to App Engine data, modules and versions.
* `GceDataSource` which implements access to Google Compute Engine instances.
* `GcsDataSource` which implements access to Google Storage buckets.

All of the data sources are implemented as static classes, with the necessary oauth tokens needed for authorizing the classes being passed explicitly in the method calls. This way they act as stateless data sources, merely proxying the calls to the underlying json APIs implemented by the various services.

### GaeDataSource class
This class provides somewhat limited functionality related to the AppEngine data, using the [App Engine admin API](https://cloud.google.com/appengine/docs/admin-api/) as the underlying data source for all of the data an behavior.

It allows the implementation of UI that will:
* List the services (aka modules) and versions within those modules, using the `GetServicesAsync()` and `GetServiceVersionsAsync()` methods.
* Change the traffic allocations of the versions in a service, using the `SetServiceTrafficAllocationAsync()` method.
* Delete a version in a service, using the `DeleteVersionAsync()` method.

### GceDataSource class
This class provides access to Google Compoute Engine instances, using the [Google Compute Engine API](https://cloud.google.com/compute/docs/reference/latest/) as the underlying data source.

It allows the implementation of UI that will:
* The list of _all_ the instances in the given project, in all of the various zones supported by the project, using the method `GetInstanceListAsync()`.
* Get the data of a particular instance, using the method `GetInstance()` or refresh the data about a particular instance using the method, `RefreshInstance()`.
* Change the metadata values for an instance, using the method `StoreMetadataAsync()`.

### GcsDatasource class
This class gives access to the list of buckets for a particular project, using the [Google Storage JSON API](https://cloud.google.com/storage/docs/json_api/) as the underlying data source.

Only one method is exposed for this class, `GetBucketListAsync()` which returns the list of all buckets in the given project.

## Support classes
The support classes cover things like paging of data, waiting for operaitons (be it gRPC or plain JSON) to finish in a way compatible with await/async.

### The ApiHelpers class
This class contains helpers for calling Google APIs, at the moment it only contains a paging helper.

The method `LoadPagedListAsync()` lists all of the _items_ contained within the _page_ provided, and is used by all of the APIs that access paged data.

### The gRPC and JSON operation helpers
These two classes, `ZoneOperationExtension` and `GrpcOperationExtension` implement the same algorithm for both types of _operations_, namely waiting for the operation to complete and return a `Task` instance that will be fullfilled once the operation finishes, either in success or with an exception in the case of failure.

These operations require the client to poll for the status of the operation, which these two methods do, periodically checking whether the operaiton is _done_ and checking if the operation suceeded or failed.

* The gPRC operation uses the endpoint defined [here](https://cloud.google.com/appengine/docs/admin-api/reference/rest/v1beta5/apps.operations/get).
* The JSON operation uses the end point defined [here](https://cloud.google.com/compute/docs/reference/latest/zoneOperations/get).
