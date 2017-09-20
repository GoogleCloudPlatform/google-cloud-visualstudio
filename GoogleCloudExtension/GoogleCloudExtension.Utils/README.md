# Design for the utils library used by the VS extension.

This library contains various utilities used by the VS extension that
are of general use.

## Converters for WPF development.

### Model
The `Model` class is the base class for all Binding models. It implementes `INotifyPropertyChanged` is the right way and exposes protected methods to raise property changed events manually if necessary.

The most important method in the class is `SetValueAndRaise<T>()` which takes care of updating the storage field and raising the property changed event, using the name of the caller as the name of the property. This method should only be called from a property setter.

### BooleanConverter
The `BooleanConverter` class implements a value converter incoming boolean values to either the value of the `TrueValue` or `FalseValue` properties.

### VisiblityConverter
The `VisiblityConverter` class implements a value converter that converts a boolean value to either `Visiblity.Visible` if the incoming value is `true` or `Visiblity.Collapsed` if the incoming value is `false`. As such is a specialized version of the `BooleanConverter` for visiblity.

The common way of using this converter is to use it in a binding to a boolean property on the view model for the view, this boolean property controlling whether a UI item is visible or not.

## General WPF utils
These utility classes make developing WPF class using MVVM more convenient.

### BindableList class
The `BindableList<T>` class propagates the `DataContext` down to the `FrameworkElements` that it contains. It is very useful when developing custom controls.

### DataGridBehaviors class
The `DataGridBehaviors` class implements attached properties that can be applied to `DataGrid` instances to provide custom sorting behaviors for the columns. It interacts with the `DataGridCustomSorter` to control how the rows in a `DataGrid` instance are sorted.

### PlaceholderMessage class
The `PlaceholderMessage` class is a simple model class to be used when a dispaying a message instead of a real object in lists in the UI.

## General utils

### ProcessUtils
The `ProcessUtils` class provides helpers to run processes and read the `stdout` and `stderr` streams from the process.

The class can run a sub-process in various modes:
* Run the process and provide the lines of output via a callback, using the `ProcessUtils.RunCommandAsync` method. This method will return once the process exits. The resulting boolean will be `true` if the process suceeded (the exit code is 0) or false otherwise.
* Run the process and collect all of the output from the process, using the `ProcessUtils.GetCommandOutputAsync` method. This method will run the process and collect all of the `stdout` and `stderr` output and return them, together with whether the process suceeded or not.
* Run the process and only know if the process failed or not, using the `ProcessUtils.LaunchCommandAsync` method. This method will run the process and return whether the process failed or not but nothing else.

On top of these ther eis a helper that will run the process using `ProcessUtils.GetCommandOutputAsync` and parse the `stdout` output, if the process suceeded, parse it as json output and return that, very very useful for parsing the output of running gcloud commands.

### Disposable class
This class implements an extensible `IDisposable` pattern by allowing the user to specify an `Action` instance to be called when the object is disposed. This is very useful when combined with the `using` keyword in C# to ensure that a _cleanup_ action happens when existing a scope.

### PathUtils class
This class provides various utility methods for manipulating file paths.

### AsyncProperty
This class enables binding to the completion of a `Task` that it wraps. This allows the UI to know when a task is completed and therefore when to change its visual state.

### AsyncProperty<T>
The `AsyncProperty<T>` class enables binding to the result of the `Task<T>` it wraps. It exposes various properties about the state of the `Task<T>` so bindings to that state can be created.

There are also helper methods to create instances of `AsynProperty<T>` from `Task<T>` and even from just a `T`.

