@@@title:Info
# Product Requirements Specification



@@@
for
@@RoboClerk(SoftwareName:Config)@@ @@0.1(SoftwareVersion:Config)@@  
  
Authors:
@@@authors:Config
NOT FOUND
@@@

# Introduction
## Purpose
The purpose of this document is to **specify** the product requirements of @@RoboClerk(SoftwareName:Config)@@ @@0.1(SoftwareVersion:Config)@@ developed by @@Acme Inc.(CompanyName:Config)@@. 

## Document Conventions
This document contains the following types of requirements:
- Functional Product Requirements - requirements specifying how the product behaves and functions
- Risk Control Product Requirements - requirements specifying how a risk is controlled using the product
- Transfer To Production Product Requirements - requirements specifying how product that is used to transfer the software to production (e.g. devOps software) behaves and functions
- Documentation Requirements - requirements specifying contents of "external" documentation (e.g. user manual, transfer to production work order etc.)
 
## Intended Audience
This document is intended for the software developers at @@Acme Inc.(CompanyName:Config)@@. It is also prepared as a record of the product level requirements. This document is also intended to be reviewed by regulatory agencies and auditors.

# Requirements

## Functional Product Requirements
@@@ProductRequirements:SLMS
+----------------------+--------------------------------------------------------------------------------+
| Requirement ID:      | 2
+----------------------+--------------------------------------------------------------------------------+
| Requirement Revision:| 4
+----------------------+--------------------------------------------------------------------------------+
| Requirement Category:| Epic
+----------------------+--------------------------------------------------------------------------------+
| Parent ID:           | 
+----------------------+--------------------------------------------------------------------------------+
| Title:               | As a user I want to be able to use documentation templates to start my documentation project
+----------------------+--------------------------------------------------------------------------------+
| Description:         | <div>The basic software structure should be based on a set of document templates. These templates will have placeholders to indicate areas that need to be filled in by roboclerk.</div>

+----------------------+--------------------------------------------------------------------------------+
| Requirement ID:      | 3
+----------------------+--------------------------------------------------------------------------------+
| Requirement Revision:| 4
+----------------------+--------------------------------------------------------------------------------+
| Requirement Category:| Epic
+----------------------+--------------------------------------------------------------------------------+
| Parent ID:           | 
+----------------------+--------------------------------------------------------------------------------+
| Title:               | As a user I want to be able to have information from the SLMS auto-populate my documents so I can save time.
+----------------------+--------------------------------------------------------------------------------+
| Description:         | <div>The software should be able to recognize what information is needed to fill in the blanks in the template and retrieve this information from an SLMS</div>

+----------------------+--------------------------------------------------------------------------------+
| Requirement ID:      | 4
+----------------------+--------------------------------------------------------------------------------+
| Requirement Revision:| 5
+----------------------+--------------------------------------------------------------------------------+
| Requirement Category:| Epic
+----------------------+--------------------------------------------------------------------------------+
| Parent ID:           | 
+----------------------+--------------------------------------------------------------------------------+
| Title:               | As a document manager I want to be able to define the trace relationship between items to customize the documentation project
+----------------------+--------------------------------------------------------------------------------+
| Description:         | <div>The relationship between traceable items that end up in the documents should be configurable so that differences between projects can be accommodated for.</div>

+----------------------+--------------------------------------------------------------------------------+
| Requirement ID:      | 5
+----------------------+--------------------------------------------------------------------------------+
| Requirement Revision:| 5
+----------------------+--------------------------------------------------------------------------------+
| Requirement Category:| Epic
+----------------------+--------------------------------------------------------------------------------+
| Parent ID:           | 
+----------------------+--------------------------------------------------------------------------------+
| Title:               | As a user I want to be able to retrieve documentation elements stored with the source code to create my documentation
+----------------------+--------------------------------------------------------------------------------+
| Description:         | <div>Some documentation are populated from information kept with the source code.&nbsp;</div>

+----------------------+--------------------------------------------------------------------------------+
| Requirement ID:      | 6
+----------------------+--------------------------------------------------------------------------------+
| Requirement Revision:| 5
+----------------------+--------------------------------------------------------------------------------+
| Requirement Category:| Epic
+----------------------+--------------------------------------------------------------------------------+
| Parent ID:           | 
+----------------------+--------------------------------------------------------------------------------+
| Title:               | As a user I want to be able to auto generate OTS documents based on the version of the binaries that are included in the software to save time
+----------------------+--------------------------------------------------------------------------------+
| Description:         | <div>A lot of information for OTS can be pulled from blob versioning systems like Nexus or Artifactory.&nbsp;</div>

+----------------------+--------------------------------------------------------------------------------+
| Requirement ID:      | 7
+----------------------+--------------------------------------------------------------------------------+
| Requirement Revision:| 5
+----------------------+--------------------------------------------------------------------------------+
| Requirement Category:| Epic
+----------------------+--------------------------------------------------------------------------------+
| Parent ID:           | 
+----------------------+--------------------------------------------------------------------------------+
| Title:               | As a user I want to be able to make edits in the current version of the documentation and have those edits propagate back to the SLMS to ensure both are synced
+----------------------+--------------------------------------------------------------------------------+
| Description:         | <div>Edits in items that were sourced from the SLMS should be propagated back to the SLMS when prompted by the user</div>

+----------------------+--------------------------------------------------------------------------------+
| Requirement ID:      | 8
+----------------------+--------------------------------------------------------------------------------+
| Requirement Revision:| 5
+----------------------+--------------------------------------------------------------------------------+
| Requirement Category:| Epic
+----------------------+--------------------------------------------------------------------------------+
| Parent ID:           | 
+----------------------+--------------------------------------------------------------------------------+
| Title:               | As a user I want to be able to export documentation to a variety of industry standard formats to facilitate external storage and review
+----------------------+--------------------------------------------------------------------------------+
| Description:         | <div>Users should be able to export the documents so they can be stored in a part 11 compliant storage system and can be shared with external stakeholders</div>


@@@
## Risk Control Product Requirements
@@@RiskControlProductReq:SLMS
UNABLE TO CREATE CONTENT, ENSURE THAT THE CONTENT CREATOR CLASS IS KNOWN TO ROBOCLERK.

@@@
## Transfer to Production Product Requirements
@@@TransferToProductionProductReq:SLMS
UNABLE TO CREATE CONTENT, ENSURE THAT THE CONTENT CREATOR CLASS IS KNOWN TO ROBOCLERK.

@@@
## Documentation Product Requirements
@@@DocumentationProductReq:SLMS
UNABLE TO CREATE CONTENT, ENSURE THAT THE CONTENT CREATOR CLASS IS KNOWN TO ROBOCLERK.

@@@