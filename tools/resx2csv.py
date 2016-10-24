#! /usr/bin/env python
"""Script to convert the .resx file into a .csv file.

This script will take any .resx file and extract all of the strings to a .csv file
easily importable into spread sheets.

Example of usage:
resx2csv.py --resx GoogleCloudExtension\Resources.resx -o strings.csv

"""

from __future__ import print_function

import argparse
import xml.etree.ElementTree as ET


# Parser for the arguments for the program.
parser = argparse.ArgumentParser()
parser.add_argument('-r', '--resx',
                    help='The path to the .resx file.',
                    required=True)
parser.add_argument('-o', '--output',
                    help='The path where to save the .csv file.',
                    required=True)


class StringDefinition(object):
    """Contains the string entry extracted from the Xml node."""

    def __init__(self, root):
        self.name = root.attrib['name']
        self.value = root.find('value').text
        self.comment = root.find('comment').text


def convert_to_csv(root, output):
    """Takes the root of the Xml file and converts it into a .csv file."""

    with open(output, 'wt') as out:
        out.write('name,value,comment\n')
        for child in root.findall('data'):
            str = StringDefinition(child)
            line = '\"{}\",\"{}\",\"{}\"\n'.format(str.name, str.value, str.comment)
            out.write(line)


def main(params):
    tree = ET.parse(params.resx)
    root = tree.getroot()
    convert_to_csv(root, params.output)


if __name__ == '__main__':
    params = parser.parse_args()
    main(params)

