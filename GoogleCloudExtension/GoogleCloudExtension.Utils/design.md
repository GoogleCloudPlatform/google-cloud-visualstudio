# Design for the utils library used by the VS extension.

This library contains various utilities used by the VS extension that
are of general use.

## Converters for WPF development.

### BooleanConverter
The `BooleanConverter` class implements a value converter incoming boolean values to either the value of the `TrueValue` or `FalseValue` properties.

### VisiblityConverter
The `VisiblityConverter` class implementes a value converter that converts a boolean value to either `Visiblity.Visible` if the incoming value is `true` or `Visiblity.Collapsed` if the incoming value is `false`. As such is a specialized version of the `BooleanConverter` for visiblity.

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
