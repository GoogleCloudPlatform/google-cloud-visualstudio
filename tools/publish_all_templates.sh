#! /usr/bin/env bash
# Publishes all of the project templates in the project.

# Ensure we fail on errors.
set -eu

# Get workspace location.
readonly workspace=$(dirname $0)/..

# List all project templates and publishe them.
for p in $(find "${workspace}/GoogleCloudExtension/ProjectTemplates" -mindepth 1 -maxdepth 1 -type d); do
    echo "Publishing project: $(basename ${p})"
    ${workspace}/tools/publish_template.sh "${p}" "Google Cloud Platform"
done

echo Done.
