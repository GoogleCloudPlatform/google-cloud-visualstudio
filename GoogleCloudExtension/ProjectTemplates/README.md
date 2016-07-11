# Project templates for the Visual Studio extension
This directory contains all of the project templates to be used in the extension.

## Procedure to publish, or update, an project template.
To publish, or update, a project template it is recommended to just use the [publish_all_templates.sh](../tools/publish_all_templates.sh) script. This script will enumerate all of the templates in this directory and publish them to
the right location under the extension codebase.

The typical set of steps then is:
* Do your modifications to the template, code, MyTemplate.vstemplate, etc...
* Use the [publish_all_templates.sh](../tools/publish_all_templates.sh) script to publish the changes to the extension codebase.
* Build the extension in Visual Studio and start it.
* Use the experimental Visual Studio instance to create a new project based on your changed template, verify that the template expands to what you expect. Verify that the project produced from the template builds.
* Repeat.

Once you are done testing the template and it look as you intend, check in all of the changes, including the produced .zip files under the extension's codebase.

