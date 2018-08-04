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

using Microsoft.VisualStudio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace GoogleCloudExtensionUnitTests
{
    public sealed class SimpleIServiceProvider : IServiceProvider, IEnumerable
    {
        private readonly Dictionary<Guid, object> _serviceMap = new Dictionary<Guid, object>();

        public void Add(Type sVsType, object service) => _serviceMap[sVsType.GUID] = service;

        public int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
        {
            if (!_serviceMap.ContainsKey(guidService))
            {
                ppvObject = IntPtr.Zero;
                return VSConstants.S_FALSE;
            }
            else
            {
                ppvObject = Marshal.GetIUnknownForObject(_serviceMap[guidService]);
                return VSConstants.S_OK;
            }
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        public IEnumerator GetEnumerator() => _serviceMap.GetEnumerator();
    }
}