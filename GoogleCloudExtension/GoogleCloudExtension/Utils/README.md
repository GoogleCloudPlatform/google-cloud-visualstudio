# Design for the common utilities
This module contains utility classes that offer various services to the extension.

## The ActivityLogUtils
The `ActivityLogUtils` class offers methosd that output logging information to [Visual Studio's activity log](https://msdn.microsoft.com/en-gb/library/bb166359.aspx). This log is useful when debugging issues with the extension, since it can be activated when reproducing a problem and send to us for further debugging.

### The Initialize method
This method is to be called at `Package` initialization time to provide the right `IServiceProvider` instance to use.

### The LogInfo, LogError methods
The `LogInfo` and `LogError` methods log a line of text at the _info_ or _error_ log levels.

## The GcpOutputWindow
The `GcpOutputWindow` class helps manage the extension's output pane within the general Visual Studio's _Output Window_. It manages the creation and activation of this pane and output lines of text to it.

## The ResourceUtils class
The `ResourceUtils` class implements utilities to deal with resources in the main extension assembly.

## The SelectionUtils class
The `SelectionUtils` class offers methods that update the Visual Studio's selection, which affect the _item_ being shown in the properties window.

## The UserPromptUtils class
The `UserPromptUtils` class provides methods for showing messages to the user in a uniform way, as well as prompt for choices, like an _Ok_ or _Cancel_ dialog.

## The ViewModelBase class
The `ViewModelBase` class is the base class for all view models in the extension, containing common properties to display in the ui.



