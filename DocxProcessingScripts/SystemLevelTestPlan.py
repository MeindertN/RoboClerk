from docx import Document
from docx.shared import Inches
import argparse

parser = argparse.ArgumentParser(description="Processes the tables in the System Level Test Plan to look good in a "
                                             "docx document. Expects the docx as produced by Pandoc.")

parser.add_argument('input', type=str, help='the input docx file')
parser.add_argument('output', type=str, help='the output docx file')
args = parser.parse_args()

inputFile = args.input
outputFile = args.output

document = Document(inputFile)

def SetColumnWidths(table, column, width):
    table.columns[column].width = Inches(width)
    for cell in table.columns[column].cells:
        cell.width = Inches(width)

for table in document.tables:
    table.autofit = False
    table.allow_autofit = False
    if "Test Case ID:" in table.columns[0].cells[0].text:
        SetColumnWidths(table, 0, 1.39)
        SetColumnWidths(table, 1, 5.26)
        table.style = 'RoboClerk Standard'
    elif "Step" in table.columns[0].cells[0].text:
        if len(table.columns) == 3:
            SetColumnWidths(table, 0, 0.45)
            SetColumnWidths(table, 1, 3.15)
            SetColumnWidths(table, 2, 3.05)
            table.style = 'RoboClerk Standard'
        elif len(table.columns) == 5:
            SetColumnWidths(table, 0, 0.45)
            SetColumnWidths(table, 1, 1.96)
            SetColumnWidths(table, 2, 1.84)
            SetColumnWidths(table, 3, 1.84)
            SetColumnWidths(table, 4, 0.56)
            table.style = 'RoboClerk Standard'
    elif "Initial:" in table.columns[0].cells[0].text:
        table.rows[0].height = Inches(0.5)
        table.columns[0].width = Inches(3.33)
        table.columns[1].width = Inches(3.32)
        table.columns[0].cells[0].width = Inches(3.33)
        table.columns[1].cells[0].width = Inches(3.32)
        table.style = 'RoboClerk Standard'

document.save(outputFile)