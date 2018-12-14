// Copyright 2018 Google Inc. All Rights Reserved.
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
    /// A set of extension methods for to help when mocking google api services.
    /// </summary>
    public static class GoogleApiMockExtensions
    {
        /// <summary>
        /// Takes a mock for either a  <see cref="BaseClientService"/> or a resource and creates a mock of a child
        /// resource.
        /// </summary>
        /// <typeparam name="T">The mocked type to get a mocked child resource from.</typeparam>
        /// <typeparam name="TResource">A resource type in the mocked type.</typeparam>
        /// <param name="mock">The mock to setup the resource as a child of.</param>
        /// <param name="resourceExpression">The express that gets the resource.</param>
        public static Mock<TResource> Resource<T, TResource>(
            this Mock<T> mock,
            Expression<Func<T, TResource>> resourceExpression)
            where T : class
            where TResource : class
        {
            var resourceMock = new Mock<TResource>(Mock.Of<IClientService>());
            mock.Setup(resourceExpression).Returns(resourceMock.Object);
            return resourceMock;
        }

        /// <summary>
        /// Sets up a request on which a response can be set up. Pairs with <see cref="Response{TResponse}"/>.
        /// </summary>
        /// When setting up an error, using this with <see cref="Response{TResponse}"/> requires fewer explicit
        /// generic parameters.
        /// <code>
        /// projects.Request(p =&gt; p.List())Response&lt;ListProjectsResponse&gt;()
        ///     .Throws(new Exception("error-message"));
        /// </code>
        /// as opposed to
        /// <code>
        /// projects.SetupRequestError&lt;ProjectsResource, ProjectsResource.ListRequest, ListProjectsResponse&gt;(
        ///         p =&gt; o.Get(bucketName, objectName),
        ///         new Exception("error-message"));
        /// </code>
        /// <typeparam name="TResource">The type of resource creating the request.</typeparam>
        /// <typeparam name="TRequest">The type of the request to make.</typeparam>
        /// <param name="resourceMock">The mock of the resource that makes the request.</param>
        /// <param name="requestExpression">The expression of the request. Uses Moq.It functions for wildcards.</param>
        /// <returns>The mock of the request object. Useful for verification.</returns>
        public static Mock<IClientService> Request<TResource, TRequest>(
            this Mock<TResource> resourceMock,
            Expression<Func<TResource, TRequest>> requestExpression)
            where TRequest : class, IClientServiceRequest
            where TResource : class
        {
            Mock<IClientService> clientServiceMock = GetClientServiceMock();
            Mock<TRequest> requestMock = GetRequestMock(requestExpression, clientServiceMock.Object);
            resourceMock.Setup(requestExpression).Returns(requestMock.Object);
            return clientServiceMock;
        }

        /// <summary>
        /// Sets up a response that returns the value.
        /// </summary>
        /// <typeparam name="TResponse">The type of response to return.</typeparam>
        /// <param name="mock">
        /// The mock of a Client Service to setup. Usually comes from <see cref="Request{TResource,TRequest}"/>.
        /// </param>
        /// <param name="response">The response value to return.</param>
        public static IReturnsResult<IClientService> ResponseReturns<TResponse>(
            this Mock<IClientService> mock,
            params TResponse[] response)
        {
            var queue = new Queue<Task<TResponse>>(response.Select(Task.FromResult));
            return mock.Setup(c => c.DeserializeResponse<TResponse>(It.IsAny<HttpResponseMessage>()))
                .Returns(queue.Dequeue);
        }

        /// <summary>
        /// Sets up a response that returns the value.
        /// </summary>
        /// <typeparam name="TResponse">The type of response to return.</typeparam>
        /// <param name="mock">
        /// The mock of a Client Service to setup. Usually comes from <see cref="Request{TResource,TRequest}"/>.
        /// </param>
        public static ISetup<IClientService, Task<TResponse>> Response<TResponse>(this Mock<IClientService> mock) =>
            mock.Setup(c => c.DeserializeResponse<TResponse>(It.IsAny<HttpResponseMessage>()));

        private static Mock<TRequest> GetRequestMock<TRequest, TResource>(
            Expression<Func<TResource, TRequest>> requestExpression,
            IClientService clientService) where TRequest : class, IClientServiceRequest
        {
            if (!(requestExpression.Body is MethodCallExpression requestMethod))
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
            return requestMock;
        }

        private static Mock<IClientService> GetClientServiceMock()
        {
            var clientServiceMock = new Mock<IClientService>();
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            // Use MockBehavior.Strict to ensure we make no actual http requests.
            var configurableHandlerMock =
                new Mock<ConfigurableMessageHandler>(MockBehavior.Strict, handlerMock.Object);
            var clientMock = new Mock<ConfigurableHttpClient>(MockBehavior.Strict, configurableHandlerMock.Object);
            clientMock.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new HttpResponseMessage()));

            clientServiceMock.Setup(c => c.BaseUri).Returns("https://mock.uri");
            clientServiceMock.Setup(c => c.HttpClient).Returns(clientMock.Object);
            clientServiceMock.Setup(c => c.Serializer.Format).Returns("json");
            clientServiceMock.Setup(c => c.SerializeObject(It.IsAny<object>())).Returns("{}");

            return clientServiceMock;
        }
    }
}

