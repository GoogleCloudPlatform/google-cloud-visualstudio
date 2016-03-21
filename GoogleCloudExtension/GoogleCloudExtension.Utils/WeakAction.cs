// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class mimics a <see cref="Action{T}"/> that holds a weak reference to the target object instead
    /// of the strong reference hold on by the built-in delegate class. This allows to write WPF 
    /// code avoiding leaks of objects by the UI.
    /// This variant of the class represents a delegate that accepts a single parameter of type <typeparamref name="TIn"/>
    /// </summary>
    /// <typeparam name="TIn">The type of parameter accepted by the delegate.</typeparam>
    public sealed class WeakAction<TIn>
    {
        private readonly WeakDelegate _handler;

        public WeakAction(Action<TIn> handler)
        {
            _handler = new WeakDelegate(handler);
        }

        public void Invoke(TIn parameter)
        {
            _handler.Invoke(parameter);
        }
    }

    /// <summary>
    /// This variant represents a weak delegate with two parameters.
    /// </summary>
    /// <typeparam name="TIn1"></typeparam>
    /// <typeparam name="TIn2"></typeparam>
    public sealed class WeakAction<TIn1, TIn2>
    {
        private readonly WeakDelegate _handler;

        public WeakAction(Action<TIn1, TIn2> handler)
        {
            _handler = new WeakDelegate(handler);
        }

        public void Invoke(TIn1 param1, TIn2 param2)
        {
            _handler.Invoke(param1, param2);
        }
    }

    /// <summary>
    /// This variant represents a weak delegate that accepts no parameters.
    /// </summary>
    public sealed class WeakAction
    {
        private readonly WeakDelegate _handler;

        public WeakAction(Action handler)
        {
            _handler = new WeakDelegate(handler);
        }

        public void Invoke()
        {
            _handler.Invoke();
        }
    }
}