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

using Microsoft.VisualStudio.OLE.Interop;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ProjectTemplate.Tests
{
    /// <summary>
    /// A COM IMessageFilter that prevents call rejection.
    /// <see href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms693740"/>
    /// </summary>
    /// <seealso href="https://www.viva64.com/en/b/0169/"/>
    public class ComMessageFilter : MarshalByRefObject, IDisposable, IMessageFilter
    {
        private readonly IMessageFilter _oldFilter;

        public ComMessageFilter()
        {
            Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(this, out _oldFilter));
        }

        ~ComMessageFilter()
        {
            Dispose(false);
        }

        /// <summary>
        /// Unregisters this message filter and reregisters the old filter.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Unregisters this message filter and reregisters the old filter.
        /// </summary>
        [SuppressMessage("ReSharper", "UnusedParameter.Global")]
        protected virtual void Dispose(bool disposing)
        {
            IMessageFilter dummy;
            Exception hrException = Marshal.GetExceptionForHR(CoRegisterMessageFilter(_oldFilter, out dummy));
            if (hrException != null)
            {
                Debug.WriteLine($"Error in {nameof(ComMessageFilter)}.{nameof(Dispose)}: {hrException}");
            }
        }

        /// <seealso href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms687237"/>
        public uint HandleInComingCall(
                uint callType,
                IntPtr taskCallerThreadId,
                uint tickCount,
                INTERFACEINFO[] interfaceInfos)
        {
            // Return the ole default.
            return (uint)SERVERCALL.SERVERCALL_ISHANDLED;
        }

        /// <summary>
        /// Always retry rejected calls.
        /// </summary>
        /// <seealso href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms680739"/>
        public uint RetryRejectedCall(IntPtr taskCalleeThreadId, uint tickCount, uint rejectType)
        {
            // Wait 200 mills before retry.
            return 200;
        }

        /// <seealso href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms694352"/>
        public uint MessagePending(IntPtr taskCalleeThreadId, uint tickCount, uint pendingType)
        {
            return (uint)PENDINGMSG.PENDINGMSG_WAITDEFPROCESS;
        }

        /// <summary>
        /// Registers COM IMessageFilters.
        /// </summary>
        /// <param name="newMessageFilter">The message filter to register.</param>
        /// <param name="oldMessageFilter">The message filter that was previously registered.</param>
        /// <returns>An HRESULT value.</returns>
        /// <seealso href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms693324"/>
        [DllImport("ole32.dll")]
        private static extern int CoRegisterMessageFilter(
            IMessageFilter newMessageFilter,
            out IMessageFilter oldMessageFilter);
    }
}
