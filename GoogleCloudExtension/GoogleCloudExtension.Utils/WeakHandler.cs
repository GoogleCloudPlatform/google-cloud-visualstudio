// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Reflection;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Implements the weak handler pattern for events, useful for avoiding leaks in 
    /// static (or global) event handlers.
    /// </summary>
    public class WeakHandler
    {
        private readonly WeakReference _target;
        private readonly MethodInfo _method;
        public WeakHandler(EventHandler handler)
        {
            _target = new WeakReference(handler.Target);
            _method = handler.Method;
        }

        public void OnEvent(object src, EventArgs args)
        {
            object target = _target.Target;
            if (target == null)
            {
                return;
            }
            _method.Invoke(target, new[] { src, args });
        }
    }
}
