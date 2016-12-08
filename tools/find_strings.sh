#! /usr/bin/env bash
# Finds strings that should have been localized but are specified as literals in
# the code.
#   $1, the root directory where to start.

# Look for literal strings on .cs files.
find $1 -name '*.cs' | xargs grep -HnE \
    "caption: \"|message: \"|title: \"|Header = \"|Caption = \""
find $1 -name '*.cs' | xargs grep -HnE \
    "caption: \\$\"|message: \\$\"|title: \"|Header = \\$\"|Caption = \\$\""

# Look for literal strings on .xaml files.
find $1 -name '*.xaml' | xargs grep -HnE \
    "Header=\"[A-Z]|Content=\"[A-Z]|Text=\"[A-Z]"



