#! /usr/bin/env python
"""Script to ensure source code has license preamble.

This script ensures that any .cs files under the root directory given
will contain a license preamble. If one is found then the file is left
alone, if not the given license file file is added.

A license preamble is defined as a comment block at the very begining
of the file.

If the original source code had a BOM mark then the output of the
script will also have one.

"""

from __future__ import print_function

import argparse
import fnmatch
import os

# Parser for the arguments for the program.
parser = argparse.ArgumentParser()
parser.add_argument('-d', '--directory',
                    help='The directory where the source lives.',
                    required=True)
parser.add_argument('-l', '--license',
                    help='The license to use.',
                    required=True)
parser.add_argument('-f', '--force',
                    help='Update license even if one is found.',
                    required=False,
                    dest='force',
                    action='store_true')
parser.add_argument('-v', '--verbose',
                    help='Show verbose output.',
                    required=False,
                    dest='verbose',
                    action='store_true')


# The bom at the begining of the file.
BOM = '\xef\xbb\xbf'
COMMENT_START = '//'


class SourceFile(object):
    def __init__(self, path):
        with open(path, 'r') as src:
            self.contents = src.readlines()
        self.path = path
        self.has_bom = False
        self.has_license = False
        self._process_contents()

    def _process_contents(self):
        # Removes the BOM mark at the begining of the file. They will
        # be added later when serializing the file.
        if self.contents[0].startswith(BOM):
            self.has_bom = True
            self.contents[0] = self.contents[0][len(BOM):]

        # Assume that the comment at the begining of the file are
        # licenses.
        first_line = self.contents[0]
        if first_line.startswith(COMMENT_START):
            self.has_license = True
        else:
            return
        for i in range(1, len(self.contents)):
            line = self.contents[i]
            if not line.startswith(COMMENT_START):
                self.license_end = i
                return

    def replace_license(self, new_license, force):
        if self.has_license and not force:
            print ("Skipping file %s, already has license" % self.path)
            return
        if self.has_license:
            source_code = self.contents[self.license_end:]
        else:
            source_code = self.contents

        # Ensure that there is an empty line between the license text
        # and the first line of text.
        if source_code[0] != "\n":
            source_code = ["\n"] + source_code

        self.contents = new_license + source_code

    def print(self):
        print ("Path: %s" % self.path)
        print ("Has Bom: %s" % self.has_bom)
        if self.has_license:
            print ("License:")
            for i in range(0, self.license_end):
                print ("  %s" % self.contents[i], end="")
            print ("End license")
        else:
            print ("No license")
        print ("")

    def update_file(self):
        print ("Updating file: %s" % self.path)
        with open(self.path, 'wt') as dest:
            if self.has_bom:
                dest.write(BOM)
            for line in self.contents:
                dest.write(line)


def load_all_files(dir):
    """Returns all of the csharp source files."""
    result = []
    for root, dirnames, filenames in os.walk(dir):
        if 'obj\\' not in root and 'bin\\' not in root:
            for name in fnmatch.filter(filenames, '*.cs'):
                result.append(SourceFile(os.path.join(root, name)))
    return result


def main():
    files = load_all_files(params.directory)
    with open(params.license, 'r') as src:
        new_license = src.readlines()
    for file in files:
        if params.verbose:
            file.print()
        file.replace_license(new_license, force=params.force)
        file.update_file()


if __name__ == "__main__":
    params = parser.parse_args()
    main()
