// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;

namespace GoogleCloudExtension.Utils
{
    public class ScopeGuard : IDisposable
    {
        private readonly Action _action;
        public ScopeGuard(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action();
        }
    }
}
