#! /usr/bin/env python
"""Script that cleans up source code.

This script takes a source code file, and cleans it. The implemented rules
are:
  * No empty <returns> tag.

"""

from __future__ import print_function

import argparse
import os
import sys

# Parse for the program.
parser = argparse.ArgumentParser()
parser.add_argument('-s', '--source',
                    help='The source file to read',
                    required=True)


def load_file(path):
    """Loads the given file and returns its lines."""
    lines = []
    with open(path, 'r') as src:
        lines = src.readlines()
    return lines


def is_valid_line(line):
    """Filters lines that contains an empty returns tag."""
    return '/// <returns></returns>' not in line


def cleanup_file(lines):
    """Returns only the valid lines from the source file."""
    return [x for x in lines if is_valid_line(x)]


def save_file(path, lines):
    """Saves the lines into the given file."""
    with open(path, 'wt') as dest:
        for line in lines:
            dest.write(line)

        
def main():
    print('Processing file %s' % params.source)
    lines = load_file(params.source)
    lines = cleanup_file(lines)
    save_file(params.source, lines)


# Entrypoint into the script.
if __name__ == "__main__":
    params = parser.parse_args()
    main()
