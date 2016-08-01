#! /usr/bin/env python
"""Script that build a .zip file for a project template.

This script will take a directory containing a project templat and
produces the .zip file for it.

"""

from __future__ import print_function

import argparse
import os
import zipfile

# Parser for the arguments for the program.
parser = argparse.ArgumentParser()
parser.add_argument('-d', '--directory',
                    help='The directory where the project tempalte lives.',
                    required=True)
parser.add_argument('-o', '--output',
                    help='The path of the zip to produce.',
                    required=True)
parser.add_argument('-v', '--verbose',
                    help='Show verbose output.',
                    required=False,
                    dest='verbose',
                    action='store_true')

def enumerate_all_files(dir):
    result = []
    for root, dirnames, filenames in os.walk(dir):
        for name in filenames:
            result.append(os.path.join(root, name))
    return result


def validate_template(files):
    manifest_found = False
    for f in files:
        filename, ext = os.path.splitext(f)
        if ext == '.vstemplate':
            manifest_found = True
            break
    if not manifest_found:
        raise Exception('Manifest not found')


def get_compress_name(file):
    return file[len(params.directory):]


def compress_template(files):
    print('Creating zipfile %s' % params.output)
    with zipfile.ZipFile(params.output, 'w') as archive:
        for file in files:
            compress_name = get_compress_name(file)
            print('Compressing file %s, with name %s' % (file, compress_name))
            archive.write(file, compress_name)


def main():
    template = enumerate_all_files(params.directory)
    validate_template(template)
    compress_template(template)


# Entrypoint into the script.
if __name__ == "__main__":
    params = parser.parse_args()
    main()
