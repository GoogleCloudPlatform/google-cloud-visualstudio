// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This is the base class for all models to be used in bindings in Xaml, it implements
    /// INotifyPropertyChanged correctly and offers helpers to incorporate the necessary notification
    /// mechanisms into the model themselves.
    /// </summary>
    public class Model : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Sets the value in the given reference and raises the property changed event for the property.
        /// </summary>
        /// <typeparam name="T">The type of storage, let the compiler determine it.</typeparam>
        /// <param name="storage">Typically a ref to a field where the value is stored.</param>
        /// <param name="value">The new value.</param>
        /// <param name="propertyName">The name of the property that is changing, do not specify let the compiler determine it.</param>
        protected void SetValueAndRaise<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
        {
            storage = value;
            RaisePropertyChanged(propertyName);
        }

        /// <summary>
        /// Raises the changed event for the given property name, useful when invalidating properties.
        /// </summary>
        /// <param name="propertyName">The name of the property that is changing. If null then the event will signify
        /// that all properties in the object changed.</param>
        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the changed event, notifying that all properties have changed, it is a short form of
        /// RaisePropertyChanged(null).
        /// </summary>
        protected void RaiseAllPropertyChanged() => RaisePropertyChanged(null);
    }
}
