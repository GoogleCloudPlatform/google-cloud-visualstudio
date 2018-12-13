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

using Moq;
using Moq.Language;
using Moq.Language.Flow;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace TestingHelpers
{
    public static class MockHelpers
    {
        /// <summary>
        /// Creates a <see cref="Lazy{T}"/> that creates a mocked <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the mocked value to create.</typeparam>
        /// <returns>The <see cref="Lazy{T}"/> that creates the mocked <typeparamref name="T"/>.</returns>
        public static Lazy<T> LazyOf<T>() where T : class => new Lazy<T>(Mock.Of<T>);

        /// <summary>
        /// Creates a <see cref="Lazy{T}"/> that creates a mocked <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the mocked value to create.</typeparam>
        /// <param name="predicate">
        /// The predicate with the specification of how the mocked object should behave.
        /// <see cref="Mock.Of{T}(Expression{Func{T,bool}})"/>
        /// </param>
        /// <returns>The <see cref="Lazy{T}"/> that creates the mocked <typeparamref name="T"/>.</returns>
        public static Lazy<T> LazyOf<T>(Expression<Func<T, bool>> predicate) where T : class => new Lazy<T>(() => Mock.Of(predicate));

        /// <summary>
        /// Creates a <see cref="Lazy{T}"/> who's <see cref="Lazy{T}.Value"/> returns the <paramref name="mock"/>'s
        /// <see cref="Mock{T}.Object"/>.
        /// </summary>
        /// <typeparam name="T">The mocked type.</typeparam>
        /// <param name="mock">The <see cref="Mock{T}"/> that defines the mocked <typeparamref name="T"/>.</param>
        /// <returns>
        /// A <see cref="Lazy{T}"/> that initializes <see cref="Lazy{T}.Value"/> to
        /// <paramref name="mock"/>.<see cref="Mock{T}.Object"/>.
        /// </returns>
        public static Lazy<T> ToLazy<T>(this Mock<T> mock) where T : class => new Lazy<T>(() => mock.Object);

        /// <summary>
        /// Creates a <see cref="Task{T}"/> that results in the <paramref name="mock"/>'s
        /// <see cref="Mock{T}.Object"/>.
        /// </summary>
        /// <typeparam name="T">The mocked type.</typeparam>
        /// <param name="mock">The <see cref="Mock{T}"/> that defines the mocked <typeparamref name="T"/>.</param>
        /// <returns>
        /// A <see cref="Task{T}"/> that synchronously results in <paramref name="mock"/>.<see cref="Mock{T}.Object"/>.
        /// </returns>
        public static Task<T> ToTask<T>(this Mock<T> mock) where T : class => Task.FromResult(mock.Object);

        /// <typeparam name="TMock">The mocked type.</typeparam>
        /// <typeparam name="TResult">Type of the return value from the expression.</typeparam>
        /// <param name="setup">The setup to specify the return value of.</param>
        /// <param name="result">The result to wrap in a task.</param>
        public static IReturnsResult<TMock> ReturnsResult<TMock, TResult>(this IReturns<TMock, Task<TResult>> setup, TResult result)
            where TMock : class => setup.Returns(Task.FromResult(result));
    }
}
