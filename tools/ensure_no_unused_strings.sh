#! /usr/bin/env bash
# This script will fail if there are unused strings in the code.

readonly workspace=$(dirname $0)/..
readonly code=${workspace}/GoogleCloudExtension/GoogleCloudExtension/


if [ -n "$(${workspace}/tools/find_unused_strings.py -d ${code} -s ${code}/Resources.resx)" ]; then
    ${workspace}/tools/find_unused_strings.py -d ${code} -s ${code}/Resources.resx
    echo "Found unused strings."
    exit 1
fi
exit 0
