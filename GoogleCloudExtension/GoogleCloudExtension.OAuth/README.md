# OAuth library
This library implements the [OAUTH 2.0 login flow installed applications](https://developers.google.com/identity/protocols/OAuth2InstalledApp) for the Visual Studio extension. 

When the user credentials are requested the default browser for the user is opened pointing at the Google credentials page. The user can then either enter the credentials or use the cached credentials in the browser to authorize the extension.

The library adds also a HTTP server (using `HttpListener`) that will listen for the result of the OAUTH 2.0 operation from server and redirect the user to either the success or failure page.

The caller can cancel the process via the `CancellationToken` provided when running the flow.

