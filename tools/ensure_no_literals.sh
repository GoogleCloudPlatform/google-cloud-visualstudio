#! /usr/bin/env bash
# This script will fail if there are string literals left in the code.

readonly workspace=$(dirname $0)/..

failed=0
if [ -n "$(${workspace}/tools/find_strings.sh ${workspace}/GoogleCloudExtension/GoogleCloudExtension)" ]; then
    echo "Found missing strings in $i"
    ${workspace}/tools/find_strings.sh $i
    failed=1
fi

if [ "$failed" -eq "1" ]; then
    echo "Found strings that are not translated."
    exit -1
fi
exit 0
8
    
