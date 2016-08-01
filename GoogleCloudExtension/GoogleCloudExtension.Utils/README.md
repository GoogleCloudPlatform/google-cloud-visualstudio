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

## Weak action handlers.
This weak action handlers help to ensure that there will not be any leaks when binding UI elements to view models and models, ensuring that the models will not keep the UI elements unnecessary alive.

### WeakDelegate.
The `WeakDelegate` class implements the same interface as a delegate but keeping a weak reference back to the delegate target.

### WeakAction.
The `WeakAction` class implements a class similar to `Action` but using `WeakDelegate` instead of a regular delegate.

Simple overloads of the generic are available with, 1, 2 ad no parameters.

### WeakCommand.
The `WeakCommand` class implements the `ICommand` interface using `WeakActions` to point back to the actual handler of the commands.

There is a variang of `WeakCommand` that accept a parameter and another one that doesn't . The `WeakCommand` variant that accept commands ensures that the argument is not `null` before invoking the command, as it is expected that the command will only support non-null arguments.

## General utils

### ProcessUtils
The `ProcessUtils` class provides helpers to run processes and read the `stdout` and `stderr` streams from the process.

The class can run a sub-process in various modes:
* Run the process and provide the lines of output via a callback, using the `ProcessUtils.RunCommandAsync` method. This method will return once the process exits. The resulting boolean will be `true` if the process suceeded (the exit code is 0) or false otherwise.
* Run the process and collect all of the output from the process, using the `ProcessUtils.GetCommandOutputAsync` method. This method will run the process and collect all of the `stdout` and `stderr` output and return them, together with whether the process suceeded or not.
* Run the process and only know if the process failed or not, using the `ProcessUtils.LaunchCommandAsync` method. This method will run the process and return whether the process failed or not but nothing else.

On top of these ther eis a helper that will run the process using `ProcessUtils.GetCommandOutputAsync` and parse the `stdout` output, if the process suceeded, parse it as json output and return that, very very useful for parsing the output of running gcloud commands.

### AsyncPropertyValue<T>
The `AsyncPropertyValue<T>` class enables binding to the result of the `Task<T>` it wraps. It exposes various properties about the state of the `Task<T>` so bindings to that state can be created.

There are also helper methods to create instances of `AsynPropertyValue<T>` from `Task<T>` and even from just a `T`.

