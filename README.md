![buildbadge](https://img.shields.io/github/actions/workflow/status/MeindertN/RoboClerk/build.yml?branch=main) [![Coverage Status](https://coveralls.io/repos/github/MeindertN/RoboClerk/badge.svg?branch=main&kill_cache=1)](https://coveralls.io/github/MeindertN/RoboClerk?branch=main&kill_cache=1) ![Open issues](https://img.shields.io/github/issues-raw/MeindertN/RoboClerk) ![license](https://img.shields.io/github/license/MeindertN/RoboClerk)

# What's RoboClerk?

RoboClerk is a powerful software package that is specifically designed to meet the needs of smaller teams working on medical device software and SaMD. One of the key advantages of using RoboClerk is that it allows teams to work in the same way they normally would, without having to worry about generating the majority of the documentation needed to show compliance with ISO62304. 

# Documentation as Code

RoboClerk follows the "Documentation-as-code" philosophy, which means that documentation is treated as a code artifact and managed in the same way as code. The software is also designed to be run as part of a CI/CD pipeline, which further streamlines the development process and ensures that compliance requirements are met at every stage. RoboClerk retrieves the artifacts that are generated as part of the team's normal development process and uses them to automatically generate the necessary documentation. This not only saves time and reduces the risk of errors, but it also ensures that compliance requirements are met without having to take time away from development activities. 

# Template Based

RoboClerk uses templates to generate the documentation. These are in Asciidoc format to ensure that the generated documentation is consistent and follows a standardized format. These templates can be easily customized to meet the specific needs of each project and, because they are ascii files, are stored with the source code in version control, ensuring precise configuration management of the documentation. This means that teams can easily track changes to the documentation and roll back to earlier versions if necessary, just as they would with their code. 

# Highly Flexible

By using Asciidoc format, RoboClerk makes all the features of asciidoc available for its users. This makes it easy to include diagrams (for example, plantUML etc. through the use of [Kroki](https://kroki.io/)), images, and other visual aids in the generated documentation.

# Getting Started

. Pull the RoboClerk docker container for the release you want to use:

`some command`

. At the command prompt, use the `scaffold demo` command to generate a set of demo directories containing templates and everything you need to run RoboClerk for the first time. The precise command to use depends on what commandline you are using:

`some other command`

To learn more details about RoboClerk please check out the documentation in the [Wiki](https://github.com/MeindertN/RoboClerk/wiki). 
