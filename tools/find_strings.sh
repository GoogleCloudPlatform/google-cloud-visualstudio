#! /usr/bin/env bash
# Finds strings that should have been localized but are specified as literals in
# the code.
#   $1, the root directory where to start.

readonly workspace=$(dirname $0)/../

# Look for literal strings on .cs files.
${workspace}/tools/find_files.py -d $1 -e .cs | xargs grep -HnE \
    "caption: \"|message: \"|title: \"|Header = \"|Caption = \""
${workspace}/tools/find_files.py -d $1 -e .cs | xargs grep -HnE \
    "caption: \\$\"|message: \\$\"|title: \"|Header = \\$\"|Caption = \\$\""
${workspace}/tools/find_files.py -d $1 -e .cs | xargs grep -HnE \
    "\\[Category\\(|\\[DisplayName\\(|\\[Description\\("
${workspace}/tools/find_files.py -d $1 -e .cs | xargs grep -HnE \
    "Prompt\\(\""
${workspace}/tools/find_files.py -d $1 -e .cs | xargs grep -HnE \
    "base\\(\""
${workspace}/tools/find_files.py -d $1 -e .cs | xargs grep -HnE \
    "\\.OutputLine\\(\\$\"|\\.OutputLine\\(\""

# Look for literal strings on .xaml files.
${workspace}/tools/find_files.py -d $1 -e .xaml | xargs grep -HnE \
    "Header=\"[A-Z]|Content=\"[A-Z]|Text=\"[A-Z]"
