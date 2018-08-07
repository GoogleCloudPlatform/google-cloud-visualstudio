#! /usr/bin/env bash
# Finds strings that should have been localized but are specified as literals in
# the code.
#   $1, the root directory where to start.

readonly workspace=$(dirname $0)/../

readonly exclude_paths=("obj" "ProjectTemplates")
readonly find_files_params="-e .cs -x ${exclude_paths[@]}"

# Look for literal strings on .cs files.
${workspace}/tools/find_files.py -d $1 ${find_files_params} | xargs grep -HnE \
    "caption: \"|message: \"|title: \"|Header = \"|Caption = \""
${workspace}/tools/find_files.py -d $1 ${find_files_params} | xargs grep -HnE \
    "caption: \\$\"|message: \\$\"|title: \"|Header = \\$\"|Caption = \\$\""
${workspace}/tools/find_files.py -d $1 ${find_files_params} | xargs grep -HnE \
    "\\[Category\\(|\\[DisplayName\\(|\\[Description\\("
${workspace}/tools/find_files.py -d $1 ${find_files_params} | xargs grep -HnE \
    "Prompt\\(\""
${workspace}/tools/find_files.py -d $1 ${find_files_params} | xargs grep -HnE \
    "base\\(\""
${workspace}/tools/find_files.py -d $1 ${find_files_params} | xargs grep -HnE \
    "\\.OutputLine\\(\\$\"|\\.OutputLine\\(\""

# Look for literal strings on .xaml files.
${workspace}/tools/find_files.py -d $1 -e .xaml -x ${exclude_paths[@]} | xargs grep -HnE \
    "Header=\"[^{]|Content=\"[^{]|Text=\"[^{]|ToolTip=\"[^{]"
