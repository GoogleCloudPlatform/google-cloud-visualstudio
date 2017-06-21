# Writing Unit Tests for DataSources
## Create [Moq][Moq] mocks of [Google.Apis][DotNetApis] services

### Intro

Writing unit tests for [GoogleCloudExtension.DataSources][GoogleCloudExtension.DataSources] is hard because the
[Google.Apis][DotNetApis] services are hard to mock.
The [Moq][Moq] library used by the Google Cloud Visual Studio Extension unit test projects can only mock virtual
members, and the [Google.Apis][DotNetApis] libraries have only a handful of interfaces and few virutal methods.
Fortunatly, they are generated; once you figure out how to mock one, you can mock them all.

### Getting service mocks for unit tests

```
PubsubService mockedService = GetMockedService(
    (PubsubService s) => s.Projects,
    p => p.Topics,
    t => t.List(It.IsAny<string>()),
    new[] { DummyString },
    responses);
```

[`DataSourceUnitTestsBase`][DataSourceUnitTestsBase] contains a `GetMockedService()` method to mock a
[Google.Apis][DotNetApis] service with two levels of resources
(e.g. [`PubsubService`][PubsubService].[`Projects`][Projects].[`Topics`][Topics].[`List(string project)`][Topics.List]).
To use this method, you must provide it three expressions and two lists.
The three expressions make the path to the request.
 * The first expression defines the path to the first resource (e.g. `(PubSubService s) => s.Projects`).
     * Declaring the parameter of this expression to be of the target service type is requried for generic type inference to work.
       Otherwise, you would have to manually set all 5 generic type parameters.
 * The second expression defines the path to the second resource (e.g. `p => p.Topics`).
 * The third expression defines the actaul request method to mock (e.g. `t => t.List(It.IsAny<string>())`).
     * Notice the use of [`Moq.It`][Moq.It] syntax to specify parameters.
 * The fourth argument is an array of additional argument to send to the request constructor.
     * These should match the types taken by the request building method.
     * They are required because the request types do not have an empty constructor.
     * The values of this list do not matter; only the types matter. We can later verify the correct parameters were given,
       and we are mocking the result.
 * The final argument is the list of responces you wish the request to return in sequence.
     * The first call to the request will return the first element; the second call returns the second element and so on.
     * If the list is empty, the request will instead throw a [`GoogleApiException`][GoogleApiException].

The created service will successfully handle calls to the request method without making any actual HTTP requests
(e.g. `mockedService.Projects.Topics.List("TestString")`).

### Writing unit tests

After getting the mocked service, you can inject it into your DataSource to test its methods
(e.g. `new PubsubDataSource(mockedService, "projectId")`).
This may require refactoring the DataSource to include a dependency injection constructor.

You can verify the correct methods are called by first getting the mock of the target resource using
[`Mock.Get(object mocked)`][Moq.Mock.Get] (e.g. `var mock = Mock.Get(mockedService.Projects.Topics);`), and then by
calling [`Mock.Verify()`][Moq.Mock.Verify] (e.g. `mock.Verify(t => t.List("expectedString"), Times.Once);`).

You can also check that superfluous calls are not made using [`It.IsNotIn()`][Moq.It.IsNotIn] and [`Times.Never()`][Moq.Times.Never]
(e.g. `mock.Verify(t => t.List(It.IsNotIn("expectedString")), Times.Never)`).

### Updating DataSourceUnitTestsBase.GetMockedService()

Currently, `GetMockedService` only works for requests beneath two resource properties.
Because each resource is a type parameter to the method, different resource property depths will require different methods.
Fortunatly most of the actual difficult work is done inside the `GetRequestMock()` method.
Building new versions of `GetMockedService` to take different resource property depths should be a simple matter of 
copying the existing function and adding or removing parameters, type parameters, mock local variables, and mock setups.


[Moq]: https://github.com/moq/moq4/blob/master/README.md
[Moq.It]: http://www.nudoq.org/#!/Packages/Moq/Moq/It
[Moq.It.IsNotIn]: http://www.nudoq.org/#!/Packages/Moq/Moq/It/M/IsNotIn(TValue)
[Moq.Mock.Get]: http://www.nudoq.org/#!/Packages/Moq/Moq/Mock/M/Get(T)
[Moq.Mock.Verify]: http://www.nudoq.org/#!/Packages/Moq/Moq/Mock(T)/M/Verify(TResult)#Verify%3CTResult%3E(Expression%3CFunc%3CT,%20TResult%3E%3E,%20Times)
[Moq.Times.Never]: http://www.nudoq.org/#!/Packages/Moq/Moq/Times/M/Never
[GoogleCloudExtension.DataSources]: https://github.com/GoogleCloudPlatform/google-cloud-visualstudio/tree/master/GoogleCloudExtension/GoogleCloudExtension.DataSources
[DotNetApis]: https://developers.google.com/api-client-library/dotnet/apis/
[DataSourceUnitTestsBase]: https://github.com/GoogleCloudPlatform/google-cloud-visualstudio/blob/master/GoogleCloudExtension/GoogleCloudExtension.DataSources.UnitTests/DataSourceUnitTestsBase.cs
[PubsubService]: https://developers.google.com/resources/api-libraries/documentation/pubsub/v1/csharp/latest/classGoogle_1_1Apis_1_1Pubsub_1_1v1_1_1PubsubService.html
[Projects]: https://developers.google.com/resources/api-libraries/documentation/pubsub/v1/csharp/latest/classGoogle_1_1Apis_1_1Pubsub_1_1v1_1_1ProjectsResource.html
[Topics]: https://developers.google.com/resources/api-libraries/documentation/pubsub/v1/csharp/latest/classGoogle_1_1Apis_1_1Pubsub_1_1v1_1_1ProjectsResource_1_1TopicsResource.html
[Topics.List]: https://developers.google.com/resources/api-libraries/documentation/pubsub/v1/csharp/latest/classGoogle_1_1Apis_1_1Pubsub_1_1v1_1_1ProjectsResource_1_1TopicsResource.html#aab633b6c978bbcb1fe154a1c441bc67d
[GoogleApiException]: https://developers.google.com/api-client-library/dotnet/reference/1.9.2/classGoogle_1_1GoogleApiException
