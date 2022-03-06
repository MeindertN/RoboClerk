
# Software Design Specification

for
@@Config:SoftwareName()@@ @@Config:SoftwareVersion()@@  
  
Authors:
@@@Config:authors()
NOT FOUND
@@@

# Introduction
## Purpose
The purpose of this document is to specify the architecture and system design of 
@@Config:SoftwareName()@@ @@Config:SoftwareVersion()@@ developed by @@Config:CompanyName()@@. 
It tracks the information required to define architecture and system design, in order to give the development 
team guidance on architecture of the system to be developed. The intended audience is the project manager, 
project team, and development team. Some portions of this document such as the user interface (UI) may, on occasion, 
be shared with the client/user and other stakeholders whose UI input/approval is needed. Traceability information is 
contained in the document by listing the software requirement ID with the design implementation in the text. 

## Definitions
*SOFTWARE ITEM* – Any identifiable part of a computer program. All levels of composition, including the top and bottom
levels, can be called software items. (See IEC62304.)

*SOFTWARE UNIT* – The lowest level of software item. This level is not further decomposed.

# Testing Section
Here we will test a variety of in document trace tags referring to software requirements.

@@Trace:SR(id=9)@@ The text that has the trace link
@@Trace:SR(Id=1234321)@@ the parameter names are case insensitive
@@Trace:SR(id=10)@@ And the second trace linke
Just a line of text.
@@Trace:SR(id=11)@@ @@Trace:SR(id=12)@@ Two more trace links

@@Trace:PR(id=7)@@ An a random product requirement trace link, is not supposed to show up in the document

@@Trace:SR(id=89)@@ non existent software trace

@@@SLMS:SYS(requirementID=7)

@@@
The above just tests to see if we can include a single product requirement

@@@Comment:general()
Things that are in comment tags will be removed when the document is processed. These comment blocks, when placed
in templates, can help authors with using hte template in the right way.
@@@

Here is a reference to a document @@Ref:RandomTestDocument()@@

Check out the @@Ref:SystemLevelTestPlan()@@ (@@ref:SystemLevelTestPlan(short=true)@@)