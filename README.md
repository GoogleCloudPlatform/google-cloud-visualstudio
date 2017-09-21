[![Build status](https://ci.appveyor.com/api/projects/status/0s0wo324dmv18uo9/branch/master?svg=true)](https://ci.appveyor.com/project/ivannaranjo/google-cloud-visualstudio-bnnrp/branch/master)
[![Code Coverage](https://codecov.io/gh/GoogleCloudPlatform/google-cloud-visualstudio/branch/master/graph/badge.svg)](https://codecov.io/gh/GoogleCloudPlatform/google-cloud-visualstudio)

# Visual Studio Extension for Google Cloud Platform
This repo contains the source code for the Visual Studio Extension for Google
Cloud Platform.

To build and install the extension you will need to have Visual Studio 2015, with Update 3 as the recommended update level. You will need the the Visual Studio SDK feature installed to be able to develop and build the extension. After that just open the .sln file and build it, that is really it. If you get an error during the restoration of the NuGet packages ensure that you have enabled nuget.org as a
source in the NuGet Package Manager.

## Support for Visual Studio 2017
To support Visual Studio 2017 and Visual Studio 2015 we are going to buid the extension on Visual Studio 2015.

### Build tools
To support VS 2017 we're going to use the
 [Microsoft.VisualStudio.Sdk.BuildTasks.14.0](https://www.nuget.org/packages/Microsoft.VisualStudio.Sdk.BuildTasks.14.0/14.0.23-pre) package, instead of the current package being used [Microsoft.VSSDK.BuildTools](https://www.nuget.org/packages/Microsoft.VSSDK.BuildTools/). The reason is
that the new package supports producing the new VSIX 3 format, which is the format supported by VS 2017.

Because we work exclusively on VS 2015, to debug the extension to VS 2017 you need to just install the extension to it and
debug the shell as it runs.

## Dependencies on Google Cloud SDK
The Visual Studio extension depends on the Cloud SDK for certain functionality, make sure that you have at least version 164.0.0 of the Cloud SDK installed when running the extension.

You can install the Google Cloud SDK from <https://cloud.google.com/sdk/>. You will also need to make sure that you have the "beta" and "kubectl" components.
* The "beta" component is used for the deployment ASP.NET Core apps to App Engine Flex.
* The "kubectl" component is used for interactions with Google Container Engine.

You can make sure that you have all the necessary components installed by running the command:
```bash
gcloud components install kubectl beta
```

## Builds
We use appveyor to build on every push to this branch, the latest release build of the extension for the master branch can be found [here](https://ci.appveyor.com/api/projects/ivannaranjo/google-cloud-visualstudio/artifacts/GoogleCloudExtension/GoogleCloudExtension/bin/Release/GoogleCloudExtension.vsix?branch=master).

## Support
To get help on using the Visual Studio Extension, please log an issue with this
project. While we will eventually be able to offer support on Stack Overflow or
a Google+ community, for now your best bet is to report issues in this Github
project.

Patches are encouraged, and may be submitted by forking this project and
submitting a Pull Request. See [CONTRIBUTING.md](CONTRIBUTING.md) for more
information.

## License

Apache 2.0. See [LICENSE](LICENSE) for more information.
