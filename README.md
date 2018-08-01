[![Build status][BuildBadge]][Build]
[![Code Coverage][CoverageBadge]][Coverage]

# Visual Studio Extension for Google Cloud Platform

This repo contains the source code for Cloud Tools for Visual Studio, an extension
for integrating Google Cloud Platform into Visual Studio.

## Install

You can install the extension from the [Visual Studio Marketplace][Marketplace].
The current and previous vsix packages are available on the [releases page][Releases].
The latest development vsix package is avalable on [appveyor][BuildArtifact].

## Dependencies on Google Cloud SDK

The Visual Studio extension depends on the Cloud SDK for certain functionality, make
sure that you have at least version 174.0.0 of the Cloud SDK installed when running the extension.

You can install the Google Cloud SDK from <https://cloud.google.com/sdk/>. You will
also need to make sure that you have the **kubectl** component. It is used for interactions
with Google Kubernetes Engine.

You can make sure that you have all the necessary components installed by running the command:
```bash
gcloud components install kubectl
```

## Support

To get help on using the Visual Studio Extension, please log an issue with this
project. While we will eventually be able to offer support on Stack Overflow or
a Google+ community, for now your best bet is to report issues in this Github
project.

## Build

To build the extension you will need to have Visual Studio 2017. You will need the
the Visual Studio extension development toolset installed to be able to develop and build the extension.
You can then open the main solution file: *GoogleCloudExtension\GoogleCloudExtension.sln*.
If you get a build/project load error, try restoring NuGet packages first, as the
project files reference files in one of the packages. If you get an error during
the restoration of the NuGet packages, ensure that you have enabled nuget.org as a
source in the NuGet Package Manager.

## Build tools

We use Microsoft.VSSDK.BuildTools. This NuGet package provides MSBuild *.props* and
*.targets* files we use to build the extension.

## Continuous Building/Testing

We use appveyor to build on every push. The latest vsix package built from the master
branch can be found [here][BuildArtifact].

## Contributing

Patches are encouraged, and may be submitted by forking this project and
submitting a Pull Request. See [CONTRIBUTING.md](CONTRIBUTING.md) for more
information.

## License

Apache 2.0. See [LICENSE](LICENSE) for more information.

[Releases]: https://github.com/GoogleCloudPlatform/google-cloud-visualstudio/releases
[Marketplace]: https://marketplace.visualstudio.com/items?itemName=GoogleCloudTools.GoogleCloudPlatformExtensionforVisualStudio
[BuildBadge]: https://ci.appveyor.com/api/projects/status/yof6v5tlchyqcnhl?svg=true
[Build]: https://ci.appveyor.com/project/GoogleCloudPlatform/google-cloud-visualstudio
[BuildArtifact]: https://ci.appveyor.com/api/projects/GoogleCloudPlatform/google-cloud-visualstudio/artifacts/GoogleCloudExtension/GoogleCloudExtension/bin/Release/GoogleCloudExtension.vsix?branch=master
[CoverageBadge]: https://codecov.io/gh/GoogleCloudPlatform/google-cloud-visualstudio/branch/master/graph/badge.svg
[Coverage]: https://codecov.io/gh/GoogleCloudPlatform/google-cloud-visualstudio
