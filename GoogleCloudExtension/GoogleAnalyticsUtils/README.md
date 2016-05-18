# GoogleAnalyticsUtils
The GoogleAnalyticsUtils library implements the [Google Analytics Measurement protocol][1] to report events and screen views to Google Analytics.

The class `AnalyticsReporter` implements the measurement protocol to report events and screen views.

## Instantiation of the class
In order to instantiate the class the following parameters are needed:
* The `propertyId`, which should be obtained from Google Analytics. It is particular to the application being tracked.
* The `appName`, which is the application name being tracked.
* The `clientId`, which is a unique identifier that identifies the current _user_. This identifer should not be tied in any way to the actual identity of the user. A GUID is the ideal one. If no identifier is provided a temporary one, a GUID, is created for the session.
  + If tracking of the same user accross sessions is desired, the GUID should be persisted.
  + The user should explicitly opt-in to be tracked.
* The `appVersion`, which is the version string for the app being tracked. This is optional.
* Whether the tracker is in `debug` mode, which will output extra information to the output window while debugging.

## Reporting events
To report events you use the `ReportEvent()` method. An event is defined by at least two values, the `category` and the `action`, see the [Event Tracking](https://developers.google.com/analytics/devguides/collection/protocol/v1/devguide#event) section in the Measure Protocol for a description of the parameters.

When designing how the events are reported, think about how they should be reported in the Analytics page, and what statistics you want to track.

## Reporting screens
To report a screen, or Windows being opened, use the `ReportScreen()` method. This method just accepts a string with the `name` of the screen. The best name would be the name of the class implementing the window being tracked. Like:
```C#
analytics.ReportScreen(nameof(MyWindow));
```

## Session reporting
The analytics class also have functionality to report the start and end of a new session. This is reported using the `ReportStartSession()` and `ReportEndSession()` methods. These methods don't accept any parameters.


[1]: https://developers.google.com/analytics/devguides/collection/protocol/v1/#getting-started
