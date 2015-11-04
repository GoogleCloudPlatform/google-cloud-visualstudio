# Visual Studio Extension for Google Cloud Platform

This repo contains the source code for the Visual Studio Extension for Google
Cloud Platform.

The extension is approaching an alpha release so while the UI is still in flux
the functionality is more or less there. The extension will allow the the user
to deploy ASP.NET v5 apps to AppEngine, generating the necessary app.yaml,
Dockerfile, etc... when doing so.

The extension also allows tentatively for some resource management, for
AppEngine and GCE instances, but it is not clear if that is the way to go yet.

To build and install the extension you will need to have Visual Studio 2015
installed with the Visual Studio SDK feature installed. After that just open the
.sln file and build it, that is really it.

Of course in order to run the Google Cloud SDK needs to be installed, in
particular the necessary components for gcloud need to be installed with the
command line:
```bash
gcloud components update app alpha preview 
```

Since all of the docker builds are done remotely, no need to install Docker
unless you want to play with the container yourself.

## Support

To get help on using the Visual Studio Extension, please log an issue with this
project. While we will eventually be able to offer support on Stack Overflow or
a Google+ community, for now your best bet is to contact the dev team directly.

Patches are encouraged, and may be submitted by forking this project and
submitting a Pull Request. See [CONTRIBUTING.md](CONTRIBUTING.md) for more
information.

## License

Apache 2.0. See [LICENSE](LICENSE) for more information.