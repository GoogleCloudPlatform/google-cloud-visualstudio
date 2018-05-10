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
using System.Runtime.InteropServices;
using System.Text;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// A C# wrapper for CredWrite Windows API.
    /// The API manages credentials for "Control Panel\User Accounts\Credential Manager"
    /// </summary>
    public static class WindowsCredentialManager
    {
        /// <summary>
        /// Write credential to Windows Credential Manager 
        /// </summary>
        /// <param name="targetName">
        /// This can be a remote computer name, a web service address, remote computer IP.
        /// </param>
        /// <param name="username">The credential username.</param>
        /// <param name="password">The credential password.</param>
        /// <param name="credentialType">Credentrial type</param>
        /// <param name="persistenceType">Credential persistence type</param>
        public static bool Write(
            string targetName, string username, string password,
            CredentialType credentialType,
            CredentialPersistence persistenceType)
        {
            password.ThrowIfNullOrEmpty(nameof(password));
            username.ThrowIfNullOrEmpty(nameof(username));
            targetName.ThrowIfNullOrEmpty(nameof(targetName));

            byte[] byteArray = Encoding.Unicode.GetBytes(password);
            // 512 * 5 is the password lengh limit enforced by CredWrite API. Verify it here.
            if (byteArray.Length > 512 * 5)
            {
                throw new ArgumentOutOfRangeException(nameof(password), "The password has exceeded 2560 bytes.");
            }

            CREDENTIAL credential = new CREDENTIAL();
            credential.AttributeCount = 0;
            credential.Attributes = IntPtr.Zero;
            credential.Comment = IntPtr.Zero;
            credential.TargetAlias = IntPtr.Zero;
            credential.Type = credentialType;
            credential.Persist = (uint)persistenceType;
            credential.CredentialBlobSize = (uint)(byteArray == null ? 0 : byteArray.Length);
            credential.TargetName = Marshal.StringToCoTaskMemUni(targetName);
            credential.CredentialBlob = Marshal.StringToCoTaskMemUni(password);
            credential.UserName = Marshal.StringToCoTaskMemUni(username);

            try
            {
                return CredWrite(ref credential, 0);
            }
            finally
            {
                Marshal.FreeCoTaskMem(credential.TargetName);
                Marshal.FreeCoTaskMem(credential.CredentialBlob);
                Marshal.FreeCoTaskMem(credential.UserName);
            }
        }

        [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] UInt32 flags);

        /// <summary>
        /// For more detail, 
        /// <see href="https://msdn.microsoft.com/en-us/library/windows/desktop/aa374788(v=vs.85).aspx">
        /// CREDENTIAL structure</see>
        /// Checkout Persist field.
        /// </summary>
        public enum CredentialPersistence : uint
        {
            Session = 1,
            LocalMachine,
            Enterprise
        }

        /// <summary>
        /// For more detail, 
        /// <see href="https://msdn.microsoft.com/en-us/library/windows/desktop/aa374788(v=vs.85).aspx">
        /// CREDENTIAL structure</see>
        /// Checkout Type field.
        /// </summary>
        public enum CredentialType
        {
            Generic = 1,
            DomainPassword,
            DomainCertificate,
            DomainVisiblePassword,
            GenericCertificate,
            DomainExtended,
            Maximum,
            MaximumEx = Maximum + 1000,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL
        {
            public uint Flags;
            public CredentialType Type;
            public IntPtr TargetName;
            public IntPtr Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public IntPtr TargetAlias;
            public IntPtr UserName;
        }
    }
}