// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Reflection;

namespace GoogleCloudExtension.Utils
{
    public class WeakDelegateBase
    {
        private WeakReference Target { get; }
        private MethodInfo Method { get; }

        protected WeakDelegateBase(Delegate src)
        {
            Target = new WeakReference(src.Target);
            Method = src.Method;
        }

        protected void Invoke(params object[] args)
        {
            var target = Target.Target;
            if (target != null)
            {
                Method.Invoke(target, args);
            }
        }
    }

    /// <summary>
    /// This class represents a delegate that holds a weak reference to the target object instead
    /// of the strong reference hold on by the built-in delegate class. This allows to write WPF 
    /// code avoiding leaks of objects by the UI.
    /// This variant of the class represents a delegate that accepts a single parameter of type <typeparamref name="TIn"/>
    /// </summary>
    /// <typeparam name="TIn">The type of parameter accepted by the delegate.</typeparam>
    public class WeakDelegate<TIn> : WeakDelegateBase
    {
        public WeakDelegate(Action<TIn> handler) : base(handler)
        {
        }

        public void Invoke(TIn parameter)
        {
            base.Invoke(parameter);
        }
    }

    /// <summary>
    /// This variant represents a weak delegate with two parameters.
    /// </summary>
    /// <typeparam name="TIn1"></typeparam>
    /// <typeparam name="TIn2"></typeparam>
    public class WeakDelegate<TIn1, TIn2> : WeakDelegateBase
    {
        public WeakDelegate(Action<TIn1, TIn2> handler) : base(handler)
        {
        }

        public void Invoke(TIn1 param1, TIn2 param2)
        {
            base.Invoke(param1, param2);
        }
    }

    /// <summary>
    /// This variant represents a weak delegate that accepts no parameters.
    /// </summary>
    public class WeakDelegate : WeakDelegateBase
    {
        public WeakDelegate(Action handler) : base(handler)
        {
        }

        public void Invoke()
        {
            base.Invoke();
        }
    }
}
