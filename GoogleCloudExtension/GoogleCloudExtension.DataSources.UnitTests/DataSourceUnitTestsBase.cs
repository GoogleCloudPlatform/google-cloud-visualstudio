// Copyright 2017 Google Inc. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google;
using Google.Apis.Http;
using Google.Apis.Requests;
using Google.Apis.Services;
using Moq;
using Moq.Language.Flow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources.UnitTests
{
    /// <summary>
    /// A base class for testing data sources.
    /// This class includes methods for mocking Apiary services.
    /// </summary>
    public class DataSourceUnitTestsBase
    {
        protected const string MockExceptionMessage = "MockException";

        /// <summary>
        /// Gets a mock for a service that extends <see cref="BaseClientService"/>.
        /// </summary>
        /// <typeparam name="TService">The type of service to mock.</typeparam>
        /// <typeparam name="TResource1">A resource type in the service.</typeparam>
        /// <typeparam name="TResource2">A resource type in the outer resource type.</typeparam>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response the request returns.</typeparam>
        /// <param name="outerResourceExpression">
        ///     The path to the resource of the service. (e.g. (PubsubService service) => service.Projects)
        /// </param>
        /// <param name="innerResourceExpression">
        ///     The path to the inner resource of the outer resource. (e.g. Projects => Projects.Topics)
        /// </param>
        /// <param name="requestExpression">
        ///     The request expression on the second resource (e.g. Topics => Topics.List(It.IsAny&lt;string&gt;()))
        /// </param>
        /// <param name="responses">The list of reponses for the request to return.</param>
        /// <returns>A mocked version of the service.</returns>
        protected static TService GetMockedService<TService, TResource1, TResource2, TRequest, TResponse>(
            Expression<Func<TService, TResource1>> outerResourceExpression,
            Expression<Func<TResource1, TResource2>> innerResourceExpression,
            Expression<Func<TResource2, TRequest>> requestExpression,
            IEnumerable<TResponse> responses)
            where TService : BaseClientService
            where TResource1 : class
            where TResource2 : class
            where TRequest : ClientServiceRequest<TResponse>
        {
            IClientService clientService = GetMockedClientService<TRequest, TResponse>(responses);
            TRequest request = GetMockedRequest<TRequest, TResponse>(requestExpression, clientService);

            var innerResourceMock = new Mock<TResource2>(clientService);
            innerResourceMock.Setup(requestExpression).Returns(request);

            var outerResourceMock = new Mock<TResource1>(clientService);
            outerResourceMock.Setup(innerResourceExpression).Returns(innerResourceMock.Object);

            var serviceMock = new Mock<TService>();
            serviceMock.Setup(outerResourceExpression).Returns(outerResourceMock.Object);
            return serviceMock.Object;
        }

        /// <summary>
        /// Gets a mock for a service that extends <see cref="BaseClientService"/>.
        /// </summary>
        /// <typeparam name="TService">The type of service to mock.</typeparam>
        /// <typeparam name="TResource">A resource type of the service.</typeparam>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response the request returns.</typeparam>
        /// <param name="resourceExpression">
        ///     The path to a resource of the service. (e.g. (PubsubService service) => service.Projects)
        /// </param>
        /// <param name="requestExpression">
        ///     The request expression on the resource (e.g. Topics => Topics.List(It.IsAny&lt;string&gt;()))
        /// </param>
        /// <param name="responses">The list of reponses for the request to return.</param>
        /// <returns>A mocked version of the service.</returns>
        protected static TService GetMockedService<TService, TResource, TRequest, TResponse>(
            Expression<Func<TService, TResource>> resourceExpression,
            Expression<Func<TResource, TRequest>> requestExpression,
            IEnumerable<TResponse> responses)
            where TService : BaseClientService
            where TResource : class
            where TRequest : ClientServiceRequest<TResponse>
        {
            IClientService clientService = GetMockedClientService<TRequest, TResponse>(responses);
            TRequest request = GetMockedRequest<TRequest, TResponse>(requestExpression, clientService);

            var resourceMock = new Mock<TResource>(clientService);
            resourceMock.Setup(requestExpression).Returns(request);

            var serviceMock = new Mock<TService>();
            serviceMock.Setup(resourceExpression).Returns(resourceMock.Object);
            return serviceMock.Object;
        }

        private static TRequest GetMockedRequest<TRequest, TResponse>(
            LambdaExpression requestExpression,
            IClientService clientService)
            where TRequest : ClientServiceRequest<TResponse>
        {
            var requestMethod = requestExpression.Body as MethodCallExpression;
            if (requestMethod == null)
            {
                throw new ArgumentException(
                    $"{nameof(requestExpression)}.{nameof(requestExpression.Body)} " +
                    $"must be of type {nameof(MethodCallExpression)} " +
                    $"but was {requestExpression.Body.GetType()}");
            }

            IEnumerable<object> methodArgs =
                requestMethod.Arguments.Select(a => a.Type)
                .Select(Expression.Default)
                .Select(e => Expression.Convert(e, typeof(object)))
                .Select(e => Expression.Lambda<Func<object>>(e).Compile()());
            object[] constructorArgs = new[] { clientService }.Concat(methodArgs).ToArray();
            var requestMock = new Mock<TRequest>(constructorArgs)
            {
                CallBase = true
            };

            requestMock.Setup(r => r.RestPath).Returns("/");
            requestMock.Object.RequestParameters.Clear();
            return requestMock.Object;
        }

        private static IClientService GetMockedClientService<TRequest, TResponse>(IEnumerable<TResponse> responses)
            where TRequest : ClientServiceRequest<TResponse>
        {
            var clientServiceMock = new Mock<IClientService>();
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
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

            ISetup<IClientService, Task<TResponse>> deserializeSetup =
                clientServiceMock.Setup(c => c.DeserializeResponse<TResponse>(It.IsAny<HttpResponseMessage>()));
            var responseQueue =
                new Queue<Task<TResponse>>(
                    responses?.Select(Task.FromResult) ?? Enumerable.Empty<Task<TResponse>>());
            if (responseQueue.Count == 0)
            {
                deserializeSetup.Throws(new GoogleApiException(typeof(TRequest).FullName, MockExceptionMessage));
            }
            else
            {
                deserializeSetup.Returns(responseQueue.Dequeue);
            }
            return clientServiceMock.Object;
        }
    }
}