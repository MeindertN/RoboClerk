from docx import Document
from docx.oxml.ns import qn
from docx.oxml import OxmlElement
import argparse


def AddTOCToDocument(document, par, num):
    paragraph = par.clear()
    header = paragraph.insert_paragraph_before("Table of Contents\n", "Heading 1")
    document.paragraphs[num + 1].paragraph_format.page_break_before = True
    header.paragraph_format.page_break_before = True
    header.paragraph_format.keep_with_next = True
    run = paragraph.add_run()
    fldChar = OxmlElement('w:fldChar')  # creates a new element
    fldChar.set(qn('w:fldCharType'), 'begin')  # sets attribute on element
    fldChar.set(qn('w:dirty'), 'true')
    fldChar.set(qn('w:fldCharType'), 'begin')

    instrText = OxmlElement('w:instrText')
    instrText.set(qn('xml:space'), 'preserve')  # sets attribute on element
    instrText.text = 'TOC \\o "1-3" \\h \\z \\u'  # change 1-3 depending on heading levels you need

    fldChar2 = OxmlElement('w:fldChar')
    fldChar2.set(qn('w:fldCharType'), 'separate')
    fldChar3 = OxmlElement('w:t')
    fldChar3.text = "Right-click to update field."
    fldChar2.append(fldChar3)

    fldChar4 = OxmlElement('w:fldChar')
    fldChar4.set(qn('w:fldCharType'), 'end')

    r_element = run._r
    r_element.append(fldChar)
    r_element.append(instrText)
    r_element.append(fldChar2)
    r_element.append(fldChar4)
    p_element = paragraph._p


def DeleteLineFromDocument(par):
    p = par._element
    p.getparent().remove(p)
    par._p = par._element = None


def InsertPageBreak(document, par, num):
    paragraph = par.clear()
    if(len(document.paragraphs) <= num + 1 ):
        document.paragraphs[num].paragraph_format.page_break_before = True
    else:
        document.paragraphs[num + 1].paragraph_format.page_break_before = True



parser = argparse.ArgumentParser(description="Processes the docx and replaces postprocessing tags (marked with ~ in the"
                                             " document) with elements of the Docx.")

parser.add_argument('input', type=str, help='the input docx file')
parser.add_argument('output', type=str, help='the output docx file')
args = parser.parse_args()

inputFile = args.input
outputFile = args.output

document = Document(inputFile)

for num, par in enumerate(document.paragraphs):
    if '~' in par.text and par.text[0] == '~':
        if '~TOC' in par.text:
            AddTOCToDocument(document, par, num)
        elif '~REMOVEPARAGRAPH' in par.text:
            DeleteLineFromDocument(par)
        elif '~PAGEBREAK' in par.text:
            InsertPageBreak(document, par, num)
            
document.save(outputFile)
