#! /usr/bin/env bash
# This script will fail if there are string literals left in the code.

readonly workspace=$(dirname $0)/..

if [ -n "$(${workspace}/tools/find_strings.sh ${workspace}/GoogleCloudExtension/GoogleCloudExtension)" ]; then
    ${workspace}/tools/find_strings.sh ${workspace}/GoogleCloudExtension/GoogleCloudExtension
    echo "Found strings that are not extracted."
    exit -1
fi
exit 0


    
