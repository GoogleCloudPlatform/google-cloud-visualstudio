using Google;
using Google.Apis.Http;
using Google.Apis.Requests;
using Google.Apis.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources.UnitTests
{
    public class DataSourceUnitTestsBase
    {
        protected const string MockExceptionMessage = "MockException";

        /// <summary>
        /// Gets a mock for a service that extends <see cref="BaseClientService"/>.
        /// </summary>
        /// <typeparam name="S">The type of service to mock.</typeparam>
        /// <typeparam name="P1">The first resource type of the service.</typeparam>
        /// <typeparam name="P2">The second resource type of the servcie.</typeparam>
        /// <typeparam name="RequestType">The type of the request.</typeparam>
        /// <typeparam name="ResponseType">The type of the response the request returns.</typeparam>
        /// <param name="outerResourcePath">
        ///     The path to the resource of the service. (e.g. (PubsubService service) => service.Projects)
        /// </param>
        /// <param name="innerResourcePath">
        ///     The path to the inner resource of the outer resource. (e.g. Projects => Projects.Topics)
        /// </param>
        /// <param name="requestMethod">
        ///     The request expression on the second resource (e.g. Topics => Topics.List(It.IsAny&lt;string&gt;()))
        /// </param>
        /// <param name="parameterValues">
        ///     Parameter values to pretend the method was given.
        /// </param>
        /// <param name="responses">The list of reponses for the request to return.</param>
        /// <returns>A mocked version of the service.</returns>
        protected static S GetMockedService<S, P1, P2, RequestType, ResponseType>(
            Expression<Func<S, P1>> outerResourcePath,
            Expression<Func<P1, P2>> innerResourcePath,
            Expression<Func<P2, RequestType>> requestMethod,
            IEnumerable<object> parameterValues,
            IEnumerable<ResponseType> responses)
            where S : BaseClientService
            where P1 : class
            where P2 : class
            where RequestType : ClientServiceRequest<ResponseType>
        {
            var clientServiceMock = new Mock<IClientService>();
            var serviceMock = new Mock<S>();
            var outerResourceMock = new Mock<P1>(clientServiceMock.Object);
            var innerResourceMock = new Mock<P2>(clientServiceMock.Object);
            var requestMock = GetRequestMock<RequestType, ResponseType>(responses, parameterValues, clientServiceMock);

            innerResourceMock.Setup(requestMethod).Returns(requestMock.Object);
            outerResourceMock.Setup(innerResourcePath).Returns(innerResourceMock.Object);
            serviceMock.Setup(outerResourcePath).Returns(outerResourceMock.Object);
            return serviceMock.Object;
        }

        /// <summary>
        ///     Gets a mock for a request that returns the given responce values.
        /// </summary>
        /// <typeparam name="RequestType">The type of request to mock.</typeparam>
        /// <typeparam name="ResponeType">The type of the responce the request gives.</typeparam>
        /// <param name="responses">The list of responses to return.</param>
        /// <param name="additionalConstructorParams">
        ///     Any additional parameters the request type constructor requires.
        /// </param>
        /// <param name="clientServiceMock">
        ///     The mock of the <see cref="IClientService"/> for the requests to use.
        /// </param>
        /// <returns>A mock of the given request type.</returns>
        private static Mock<RequestType> GetRequestMock<RequestType, ResponeType>(
            IEnumerable<ResponeType> responses,
            IEnumerable<object> additionalConstructorParams,
            Mock<IClientService> clientServiceMock)
            where RequestType : ClientServiceRequest<ResponeType>
        {
            var requestMock =
                new Mock<RequestType>(
                    Enumerable.Repeat(clientServiceMock.Object, 1).Concat(additionalConstructorParams).ToArray())
                {
                    CallBase = true
                };

            var handlerMock = new Mock<HttpMessageHandler>();
            // Use MockBehavior.Strict to ensure we make no acutal http requests.
            var configurableHandlerMock =
                new Mock<ConfigurableMessageHandler>(MockBehavior.Strict, handlerMock.Object);
            var clientMock = new Mock<ConfigurableHttpClient>(MockBehavior.Strict, configurableHandlerMock.Object);
            clientMock.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new HttpResponseMessage()));

            clientServiceMock.Setup(c => c.BaseUri).Returns("https://mock.uri");
            clientServiceMock.Setup(c => c.HttpClient).Returns(clientMock.Object);
            clientServiceMock.Setup(c => c.Serializer.Format).Returns("json");
            clientServiceMock.Setup(c => c.SerializeObject(It.IsAny<object>())).Returns("{}");

            var deserializeSetup =
                clientServiceMock.Setup(c => c.DeserializeResponse<ResponeType>(It.IsAny<HttpResponseMessage>()));
            var responseQueue =
                new Queue<Task<ResponeType>>(
                    responses?.Select(Task.FromResult) ?? Enumerable.Empty<Task<ResponeType>>());
            if (responseQueue.Count == 0)
            {
                deserializeSetup.Throws(new GoogleApiException(typeof(RequestType).FullName, MockExceptionMessage));
            }
            else
            {
                deserializeSetup.Returns(responseQueue.Dequeue);
            }
            requestMock.Setup(r => r.RestPath).Returns("/");
            requestMock.Object.RequestParameters.Clear();
            return requestMock;
        }
    }
}