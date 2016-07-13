# Tools for the repo
This directory contains scripts to manage the source code for the extension, as well as perform operations on ASP.NET 4.x apps.

## deploy_app.cmd
This batch script will deploy an ASP.NET 4.x application to the given Windows Server VM given the following parameters:
* The path to the .csproj, necessary to build and publish the app.
* The directory where the app is to be staged. Microsoft calls this _publishing_ the app.
* The path to the .publishsettings file for the server to which to deploy.

## ensure_license.py
This python script ensures that all .cs files under the directory given contains a license comment block at the very begining of the file.

If no license is found then the license text file provided is used as the license for the source file. 

The script can also replace the existing license with the given license file.

This script should be used before submitting a PR to ensure that all source code contains the license preamble.

The file [copyright_notice.txt](tools/copyright_notice.txt) is the current preamble to use of all source code.

## format_files.cmd
This batch script uses the .NET [codeformatter](https://github.com/dotnet/codeformatter) to format the code. The script defines the set of formatting rules to apply to the source code.

This script should be used before submitting a PR to ensure uniform code formatting and coding style.

## publish_template.sh
This shell script publishes the given Visual Studio template project to the right directory under the [GoogleCloudExtension](../GoogleCloudExtension/GoogleCloudExtension) directory. This makes it possible to pick up the template when building the extension.

This script is a shell script to be run from a bash shell, like the Git bash shell.

## publish_all_templates.sh
This shell script lists all of the templates under the [ProjectTemplates](../GoogleCloudExtension/ProjectTemplates) directory and publish them to the right directory under the [GoogleCloudExtension](../GoogleCloudExtension/GoogleCloudExtension) directory in the main directory for the extension.

This script is a shell script to be run from a bash shell, like the Git bash shell.
