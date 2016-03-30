# DNX support library
This library contains all of the code that deals with DNX projects, which is the project system for ASP.NET Core (used to be known as ASP.NET 5).

This library provides classes to manage a solution that contains DNX projects, the projects themselves, the DNX runtime and more importantly how to deploy ASP.NET 5 apps to AppEngine.

Most of this functionality will be changing once the RC2 version of ASP.NET Core ships, as the DNX system is getting a big makeover.

## DnxSolution and DnxProject classes.
The the `DnxSolution` and `DnxProject` clasess help dealing with a solution that contains DNX projects.

### The DnxSolution.
The `DnxSolution` provides the following methods:
* `GetProjects()` which list the DNX projects contained in the solution.
* `GetProjectFromName()` which returns the `DnxProject` instance given the name of the project.

### The DnxProject.
The `DnxProject` provides information about an ASP.NET DNX project such as:
* The runtime targetted by the project, be it CLR, CoreCLR or Mono.
* Whether the project is an ASP.NET project or not.

## DnxEnvironment class.
This class provides a way to inspect the environment and find out what version of the DNX runtime is installed. The most important use is the method `ValidateDnxInstallation()` which is will detect whether the DNX installation is what is expected, in the current code RC1-final.

## DnxDeployment class.
This class allows the user to deploy an ASP.NET RC1-final app, the main method is `DeployApplicationAsync()` which takes the name of the project to deploy and the gcloud `Context` to use to deploy the app.

The deployment process is quite complex, TODO: document how the deployment happen.

Note that the deployment process is due to change with the RC2 release.



