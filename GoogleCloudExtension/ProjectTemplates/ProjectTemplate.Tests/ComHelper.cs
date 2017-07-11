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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace ProjectTemplate.Tests
{
    /// <summary>
    /// Helper class to get com objects from the running object table.
    /// </summary>
    /// <seealso href="https://www.viva64.com/en/b/0169/"/>
    /// <seealso href="http://www.helixoft.com/blog/creating-envdte-dte-for-vs-2017-from-outside-of-the-devenv-exe.html"/>
    public static class ComHelper
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static class HRESULT
        {
            public const int S_OK = 0;
        }

        ///<see href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms678542"/>
        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(uint zero, out IBindCtx ctx);

        /// <summary>
        /// Get the COM object from the running object table that matches the predicate.
        /// </summary>
        /// <param name="predicate">The test to check the monikers against.</param>
        public static object GetRunningObject(Func<IMoniker, IBindCtx, bool> predicate)
        {
            IBindCtx bindCtx;
            Marshal.ThrowExceptionForHR(CreateBindCtx(0, out bindCtx));
            IRunningObjectTable rot;
            bindCtx.GetRunningObjectTable(out rot);
            var targetMoniker = rot.GetRunningMonikers().FirstOrDefault(m => predicate(m, bindCtx));
            if (targetMoniker != null)
            {
                object dte;
                Marshal.ThrowExceptionForHR(rot.GetObject(targetMoniker, out dte));
                return dte;
            }
            else
            {
                return null;
            }
        }

        private static IEnumerable<IMoniker> GetRunningMonikers(this IRunningObjectTable rot)
        {
            IEnumMoniker monikersEnum;
            rot.EnumRunning(out monikersEnum);
            return monikersEnum.ToEnumerable();
        }

        private static IEnumerable<IMoniker> ToEnumerable(this IEnumMoniker monikersEnum)
        {
            int fetchSize = 1;
            var monikers = new IMoniker[fetchSize];
            IntPtr fetchedCount = IntPtr.Zero;
            while (monikersEnum.Next(fetchSize, monikers, fetchedCount) == HRESULT.S_OK)
            {
                var currentMoniker = monikers[0];
                if (currentMoniker != null)
                {
                    yield return currentMoniker;
                }
            }
        }
    }
}