# OAUTH library design
This library implements the interactions with the Google OAUTH servers as defined by the [Using OAUTH 2.0 for installed applications document](https://developers.google.com/identity/protocols/OAuth2InstalledApp).

Because the extenstion credentials model revolves arround the refresh tokens the library only concerns itself with fetching refresh tokens, no implementation is provided for fetchin access tokens.

## The OAuthManager class
This is a static class that implements the limited set of the OAUTH protocol implemented by this library.

### GetInitialOAuthUrl()
This method will return the URL to use to start the OAUTH flow for a user, this URL is composed with:
* The client_id for the application.
* The list of scopes that is requested.

The method will compose the URL as expected by Google's OAUTH servers and return it. It is up to the app to navigate to this URL in the _right_ way.

### EndOAuthFlow()
This method will exchange the access_code returned from the OAUTH servers for the refresh token.
