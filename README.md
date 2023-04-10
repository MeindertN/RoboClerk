![buildbadge](https://img.shields.io/github/actions/workflow/status/MeindertN/RoboClerk/build.yml?branch=main) [![Coverage Status](https://coveralls.io/repos/github/MeindertN/RoboClerk/badge.svg?branch=main&kill_cache=1)](https://coveralls.io/github/MeindertN/RoboClerk?branch=main&kill_cache=1) ![Open issues](https://img.shields.io/github/issues-raw/MeindertN/RoboClerk) ![license](https://img.shields.io/github/license/MeindertN/RoboClerk)
# Welcome to RoboClerk

## What's RoboClerk?

RoboClerk is a powerful software package that is specifically designed to meet the needs of smaller teams working on medical device software and SaMD. One of the key advantages of using RoboClerk is that it allows teams to work in the same way they normally would, without having to worry about generating the majority of the documentation needed to show compliance with ISO62304. 

## Documentation as Code

RoboClerk follows the "Documentation-as-code" philosophy, which means that documentation is treated as a code artifact and managed in the same way as code. The software is also designed to be run as part of a CI/CD pipeline, which further streamlines the development process and ensures that compliance requirements are met at every stage. RoboClerk retrieves the artifacts that are generated as part of the team's normal development process and uses them to automatically generate the necessary documentation. This not only saves time and reduces the risk of errors, but it also ensures that compliance requirements are met without having to take time away from development activities. 

## Template Based

RoboClerk uses templates to generate the documentation. These are in Asciidoc format to ensure that the generated documentation is consistent and follows a standardized format. These templates can be easily customized to meet the specific needs of each project and, because they are ascii files, are stored with the source code in version control, ensuring precise configuration management of the documentation. This means that teams can easily track changes to the documentation and roll back to earlier versions if necessary, just as they would with their code. 

## Highly Flexible

RoboClerk utilizes the Asciidoc format, granting users access to its extensive features. This allows for the seamless incorporation of diagrams (e.g., PlantUML through [Kroki](https://kroki.io/)), images, tables, code snippets, cross-references, and more into the generated documentation, enhancing its overall quality and readability.

## Getting Started

1. Pull the RoboClerk docker container for the release you want to use:

```
   docker pull ghcr.io/meindertn/roboclerk:latest
```

2. At the command prompt, in the directory where you want to create a RoboClerk documentation scaffold, use the `scaffold demo` command to generate a set of demo directories containing templates and everything you need to run RoboClerk for the first time. The precise command to use depends on what commandline you are using:

Linux shell:
```
    docker run -v $(pwd):/mnt --rm ghcr.io/meindertn/roboclerk:latest scaffold demo
```
Windows Powershell:
```
    docker run -v ${PWD}:/mnt --rm ghcr.io/meindertn/roboclerk:latest scaffold demo
```
Windows Commandline:
```
    docker run -v %cd%:/mnt --rm ghcr.io/meindertn/roboclerk:latest scaffold demo
```

3. RoboClerk will create two directories for you. `RoboClerk_input` and `RoboClerk_output`. In `RoboClerk_input` you will find a set of templates and various other files. `RoboClerk_output` will contain the finished documentation. 

4. Now generate the documentation by running the following command:

Linux shell:
```
    docker run -v $(pwd):/mnt --rm ghcr.io/meindertn/roboclerk:latest generate
```
Windows Powershell:
```
    docker run -v ${PWD}:/mnt --rm ghcr.io/meindertn/roboclerk:latest generate
```
Windows Commandline:
```
    docker run -v %cd%:/mnt --rm ghcr.io/meindertn/roboclerk:latest generate
```

5. The `RoboClerk_output` directory now has all the output and intermediary files in it. The demo pipeline is set up to produce Microsoft Word documentation. The intermediary files are Asciidoc (`*.adoc`) and Docbook 5 (`*.xml`).

6. Things to try:

* In normal operation, RoboClerk will connect to a software lifecycle management system like Redmine or AzureDevops but for the demo, it uses a JSON file with all the items in it. You can open the JSON file, make changes to some of the items and re-generate the documentation to see the effect.
* Within the RoboClerk_input directory are the templates that define the documents. Open the templates with your favorite text editor, make some changes, re-generate the documents and see the effect. 

7. Once you are done using the JSON file as input, connect RoboClerk to a demo SLMS. First, get the demo Redmine container using the following command:

```
   docker pull ghcr.io/meindertn/redmine-demo:latest
```

8. From this point forward, my assumption is that you are using linux, see the earlier examples on how to run these commands in other commandlines. Scaffold a non-demo instance of the RoboClerk directory structure using:

```
    docker run -v $(pwd):/mnt --rm ghcr.io/meindertn/roboclerk:latest scaffold
```

9. Open `./RoboClerk_input/RoboClerkConfig/RoboClerk.toml` in a text editor and remove all plugins except the `RedmineSLMSPlugin`:

```
   DataSourcePlugin = [ "RedmineSLMSPlugin" ]
```

10. Start the demo instance of Redmine:

```
   docker run -p 3001:3000 -d ghcr.io/meindertn/redmine-demo:latest
```

11. Things to try:

* Log into the demo instance at `http://localhost:3001/` with username `admin` and password `password123`. Make changes to items or create new items in the demo instance of redmine, re-generate the documents using the following command (note the addition of `--network="host"` only needed because we need to connect to localhost from the container):
```
      docker run -v $(pwd):/mnt --rm --network="host" ghcr.io/meindertn/roboclerk:latest generate 
```
* Take a look at the redmine configuration file in `RoboClerk_input/PluginConfig/RedmineSLMSPlugin.toml` to see what configuration options are available.

To learn more details about RoboClerk please check out the documentation in the [Wiki](https://github.com/MeindertN/RoboClerk/wiki). 
