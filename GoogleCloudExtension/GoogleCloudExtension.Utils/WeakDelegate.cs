// Copyright 2016 Google Inc. All Rights Reserved.
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

using System;
using System.Reflection;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Mimics the built-in Delegate class but having a weak reference to the target
    /// instead of a strong reference. 
    /// </summary>
    public sealed class WeakDelegate
    {
        private readonly WeakReference _target;
        private readonly MethodInfo _method;

        /// <summary>
        /// Initializes a new instance of the weak delegate.
        /// </summary>
        /// <param name="src">The delgate to mimic</param>
        public WeakDelegate(Delegate src)
        {
            _target = new WeakReference(src.Target);
            _method = src.Method;
        }

        /// <summary>
        /// Invokes the delegate with the given arguments only if the weak reference can be resolved, otherwise
        /// it is a noop.
        /// </summary>
        /// <param name="args">The arguments to pass on to the delegate.</param>
        public void Invoke(params object[] args)
        {
            var target = _target.Target;
            if (target != null)
            {
                _method.Invoke(target, args);
            }
        }
    }
}
