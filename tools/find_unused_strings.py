#! /usr/bin/env python
"""Script that lists all files that match the given extension

This script takes a directory and will list all of the files
recursively filtering by the given extension.

"""

import argparse
import os
import sys
import xml.etree.ElementTree as ET

# Parse for the program.
parser = argparse.ArgumentParser()
parser.add_argument('-d', '--directory',
                    help='The root directory',
                    required=True)
parser.add_argument('-s', '--strings',
                    help='The strings (.resx) file to check.',
                    required=True)


# All string references are of the form: Resource.<string name>
resource_string_prefix='Resources.'


def list_all_files(dir):
    """Lists all of the source files (.cs and .xaml) under the given directory"""

    result = []
    for root, _, filenames in os.walk(dir):
        for name in filenames:
            filename, ext = os.path.splitext(name)
            if ext == '.cs' or ext == '.xaml':
                result.append(os.path.join(root, name))
    return result


def is_valid_char(src):
    """Determins if the given char is valid as an identifier char."""

    return src.isalnum()


def is_valid_string_name(src):
    """All string names start with an uppercase char."""

    return src[0].isupper()


def extract_string(line, idx, result):
    """Extracts the first string reference on, or after, idx in line."""

    begin = line.find(resource_string_prefix, idx)
    if begin == -1:
        return -1
    
    begin = begin + len(resource_string_prefix)
    end = -1
    for i in range(begin, len(line)):
        if not is_valid_char(line[i]):
            end = i
            break

    result.add(line[begin:end])
    return end
        

def find_strings(line, result):
    """Finds all of the string references in the given line."""

    idx = 0
    while idx != -1:
        idx = extract_string(line, idx, result)


def get_used_strings(file):
    """Returns all of the strings being used by the given file."""

    result = set()
    with open(file, 'r') as src:
        for line in src.readlines():
            find_strings(line, result)
    return result


def load_strings(src):
    """Loads the .resx file and extracts all of the string names."""

    tree = ET.parse(src)
    root = tree.getroot()
    result = set()
    for child in root.findall('data'):
        result.add(child.attrib['name'])
    return result


def main(params):
    strings = load_strings(params.strings)
    files = list_all_files(params.directory)
    for file in files:
        used = get_used_strings(file)
        strings = strings - used
    if len(strings) > 0:
        print('Strings that are not used:')
        for s in strings:
            print(s)


# Entrypoint into the script.
if __name__ == '__main__':
    main(parser.parse_args())
