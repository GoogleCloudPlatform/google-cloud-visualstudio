# Project templates for the Visual Studio extension
This directory contains all of the project templates to be used in the extension.

## Procedure to publish, or update, an project template
To publish, or update, a project template it is recommended to just use the [publish_all_templates.sh](../tools/publish_all_templates.sh) script. This script will enumerate all of the templates in this directory and publish them to
the right location under the extension codebase.

The typical set of steps then is:
* Do your modifications to the template, code, MyTemplate.vstemplate, etc...
* Use the [publish_all_templates.sh](../tools/publish_all_templates.sh) script to publish the changes to the extension codebase.
* Build the extension in Visual Studio and start it.
* Use the experimental Visual Studio instance to create a new project based on your changed template, verify that the template expands to what you expect. Verify that the project produced from the template builds.
* Repeat.

## To add package references to an existing template
The procedure to add a new package reference to an existing template differs between an ASP.NET Core 1.0 app and an ASP.NET 4.x app.

### Adding a package reference to an ASP.NET 4.x app
Adding a package reference to an ASP.NET 4.x project is a very involved process because you need to reproduce what Visual Studio normally does when adding the package. 

The best way to know what changes are needed is to create a new project from the template and, using Visual Studio, add the package. Doing a diff with the unchanged project will show you of the changes you need to port to the template. These changes usually involve:
* Adding a reference to the new package, and the packages it depends on, to the `packages.config` file for the project.
* Adding all the necessary `<Reference>` elements to the `.csproj` file. These should include the full identity of the package and the `<HintPath>`.
* Ensure that you add all of the new `<Error>` entries in the `<Target Target Name="EnsureNuGetPackageBuildImports" BeforeTargets="PrepareForBuild">` entry. These entries add new `msbuild` targets required by the new packages.
  + It is possible that you don't have any new `<Error>` entries if the new package doesn't add new targets, that is fine.

### Adding a package reference to an ASP.NET Core 1.0 app
To add a package reference to an ASP.NET Core 1.0 app you just need to add the reference to the `project.json` in the project's template. Make sure that you never include the `project.lock.json` file in the template to ensure that the project will be restored when opened by Visual Studio.

Once you are done testing the template and it look as you intend, check in all of the changes, including the produced .zip files under the extension's codebase.

