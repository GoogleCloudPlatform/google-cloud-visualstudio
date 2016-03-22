// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.


namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class represents the result from running validation on the DNX runtime installation.
    /// </summary>
    public class DnxValidationResult
    {
        /// <summary>
        /// Whether the DNX runtime is installed.
        /// </summary>
        public bool IsDnxInstalled { get; }

        public DnxValidationResult(bool isDnxInstalled)
        {
            IsDnxInstalled = isDnxInstalled;
        }
    }
}
