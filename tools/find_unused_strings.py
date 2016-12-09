#! /usr/bin/env python
"""Script that lists all files that match the given extension

This script takes a directory and will list all of the files
recursively filtering by the given extension.

"""

import argparse
import os
import sys

# Parse for the program.
parser = argparse.ArgumentParser()
parser.add_argument('-d', '--directory',
                    help='The root directory',
                    required=True)
parser.add_argument('-s', '--strings',
                    help='The strings (.resx) file to check.',
                    required=True)

string_prefix="Resources."

def list_all_files(dir):
    result = []
    for root, _, filenames in os.walk(dir):
        for name in filenames:
            filename, ext = os.path.splitext(name)
            if ext == ".cs" or ext == ".xaml":
                result.append(os.path.join(root, name))
    return result


def is_valid_char(src):
    return src.isalpha()


def is_valid_string_name(src):
    return src[0].isupper()


def find_string(line, idx):
    begin=idx + len(string_prefix)
    end=-1
    for i in range(begin, len(line)):
        if not is_valid_char(line[i]):
            end=i
            break
    return line[begin:end]


def find_strings(file):
    result = set()
    with open(file, 'r') as src:
        for line in src.readlines():
            idx = line.find(string_prefix)
            if idx != -1:
                name = find_string(line, idx)
                if is_valid_string_name(name):
                    result.add(name)
    return result
    

def main(params):
    files = list_all_files(params.directory)
    for file in files:
        strings = find_strings(file)
        for str in strings:
            print(str)


# Entrypoint into the script.
if __name__ == "__main__":
    main(parser.parse_args())
