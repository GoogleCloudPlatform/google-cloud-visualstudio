#! /usr/bin/env bash
# Finds strings that should have been localized but are specified as literals in
# the code.
#   $1, the root directory where to start.

echo Arg: $1

readonly workspace=$(dirname $0)/../

# Look for literal strings on .cs files.
${workspace}/tools/find_files.py -d $1 -e .cs | xargs grep -HnE \
    "caption: \"|message: \"|title: \"|Header = \"|Caption = \""
${workspace}/tools/find_files.py -d $1 -e .cs | xargs grep -HnE \
    "caption: \\$\"|message: \\$\"|title: \"|Header = \\$\"|Caption = \\$\""

# Look for literal strings on .xaml files.
${workspace}/tools/find_files.py -d $1 -e .xaml | xargs grep -HnE \
    "Header=\"[A-Z]|Content=\"[A-Z]|Text=\"[A-Z]"
