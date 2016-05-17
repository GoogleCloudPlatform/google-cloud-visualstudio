# Analytics for the extension
This module contains the classes that implement the analytics reporting for the extension.

## The ExtensionAnalytics class
The `ExtensionAnalytics` class provides helpers to report commands, events and screen analytics to Google Analytics. It is centralized so all of the extension can report in a uniform way.

This class also provides the `EnsureAnalyticsOptIn()` method which is to be called from all of the _entrypoints_ into the extension to ensure that the opt-in dialog is shown, only once, to the user. The user can then choose to opt-in to allow analytics to be send to Google Analytics or not.

### Common reporting methods
The extension reports analytics in the following categories:
* Commands, using the `ReportCommand()` method, the extension reports of each command invocation and how the command was invoked, a button, the "Tools" menu, etc...
* Events, using the `ReportEvent()` method, the extension reports on _interesting_ events happening, like starting an OAUTH flow, the cancellation of said flow, etc... 
* Session, using the `ReportStartSession()` and `ReportEndSession()` methods, the extension reports on start and end of the session.
