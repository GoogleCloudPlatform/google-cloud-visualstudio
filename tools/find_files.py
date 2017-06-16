#! /usr/bin/env python
"""Script that lists all files that match the given extension

This script takes a directory and will list all of the files
recursively filtering by the given extension.

"""

from __future__ import print_function

import argparse
import os
import sys
import msvcrt

# Parse for the program.
parser = argparse.ArgumentParser()
parser.add_argument('-d', '--directory',
                    help='The root directory',
                    required=True)
parser.add_argument('-e', '--extension',
                    help='The extension to filter by.',
                    required=True)
parser.add_argument('-x', '--exclude',
                    help='Paths to exclude.',
                    nargs='+',
                    dest='excluded_paths',
                    required=False)


def is_excluded(name):
    if params.excluded_paths:
        for path in params.excluded_paths:
            if path in name:
                return True
    return False


def print_all_files(dir):
    for root, _, filenames in os.walk(dir):
        if not is_excluded(root):
            for name in filenames:
                filename, ext = os.path.splitext(name)
                if ext == params.extension:
                    sys.stdout.write(os.path.join(root, name).replace("\\", "/"))
                    sys.stdout.write("\n")


def main():
    msvcrt.setmode(sys.stdout.fileno(), os.O_BINARY)
    print_all_files(params.directory)

# Entrypoint into the script.
if __name__ == "__main__":
    params = parser.parse_args()
    main()

