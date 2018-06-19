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
using System;
using System.Linq.Expressions;

namespace TestingHelpers
{
    public static class LazyMock
    {
        /// <summary>
        /// Creates a <see cref="Lazy{T}"/> that creates a mocked <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the mocked value to create.</typeparam>
        /// <returns>The <see cref="Lazy{T}"/> that creates the mocked <typeparamref name="T"/>.</returns>
        public static Lazy<T> Of<T>() where T : class => new Lazy<T>(Mock.Of<T>);

        /// <summary>
        /// Creates a <see cref="Lazy{T}"/> that creates a mocked <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the mocked value to create.</typeparam>
        /// <param name="predicate">
        /// The predicate with the specification of how the mocked object should behave.
        /// <see cref="Mock.Of{T}(Expression{Func{T,bool}})"/>
        /// </param>
        /// <returns>The <see cref="Lazy{T}"/> that creates the mocked <typeparamref name="T"/>.</returns>
        public static Lazy<T> Of<T>(Expression<Func<T, bool>> predicate) where T : class => new Lazy<T>(() => Mock.Of(predicate));

        /// <summary>
        /// Creates a <see cref="Lazy{T}"/> who's <see cref="Lazy{T}.Value"/> returns the <paramref name="mock"/>'s
        /// <see cref="Mock{T}.Object"/>.
        /// </summary>
        /// <typeparam name="T">The mocked type.</typeparam>
        /// <param name="mock">The <see cref="Mock{T}"/> that defines the mocked <typeparamref name="T"/>.</param>
        /// <returns>
        /// A <see cref="Lazy{T}"/> that initalizes <see cref="Lazy{T}.Value"/> to
        /// <paramref name="mock"/>.<see cref="Mock{T}.Object"/>.
        /// </returns>
        public static Lazy<T> ToLazy<T>(this Mock<T> mock) where T : class => new Lazy<T>(() => mock.Object);
    }
}
