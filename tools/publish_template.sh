#! /usr/bin/env bash
# Releases the given template into the extension.
#   $1, the directory with the template to release.
#   $2, the sub-directory where to publish the template.

# Ensure we fail on errors.
set -eu

# Get workspace location.
readonly workspace=$(dirname $0)/..

# Expand parameters.
readonly template_dir=${1:-""}
readonly dest_dir=${2:-""}

# Check mandatory parameters.
if [ -z ${template_dir} ]; then
    echo "Need to specify the project and template location."
    exit -1
fi

# Gather information about the source and targe directories.
readonly template_name="$(basename $1)"
readonly extension_dir="${workspace}/GoogleCloudExtension/GoogleCloudExtension"

# Determine the extension templates directory to use.
if [ -n "${dest_dir}" ]; then
    readonly extension_templates="${extension_dir}/ProjectTemplates/${dest_dir}"
else
    readonly extension_templates="${extension_dir}/ProjectTemplates"
fi

# The final path for the template zip.
readonly released_template_zip="${extension_templates}/${template_name}.zip"

# Ensure the extension's ProjectTemplate exists.
echo "Creating the ProjectTemplates directory: ${extension_templates}"
mkdir -p "${extension_templates}"

# Delete the existing .zip file and compress again. 
echo "Deleting existing zip: ${released_template_zip}"
rm "${released_template_zip}" || true

# Compress and release the extention.
echo "Compressing template: ${template_name}"
true || find "$1" -name '*~' | xargs rm
${workspace}/tools/build_zip.py -o "${released_template_zip}" -d "$1"

echo "Done."
