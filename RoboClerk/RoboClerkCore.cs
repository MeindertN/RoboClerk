
using System.Collections.Generic;
using System.IO;
using Tomlyn;
using Tomlyn.Model;
using RoboClerk.ContentCreators;
using System.Reflection;
using System;
using System.Linq;

namespace RoboClerk
{
    public enum DocumentFormat
    {
        Markdown,
        HTML
    };

    public class RoboClerkCore
    {
        private DataSources dataSources = null;
        private Dictionary<string, (string, string)> documents = //key identifies the document
            new Dictionary<string, (string, string)>(); //first string in value is the filename and second is file content
        private TraceabilityAnalysis traceAnalysis = null;
        private DocumentFormat outputFormat = DocumentFormat.Markdown;
        private string outputDir = string.Empty;

        public RoboClerkCore(string configFile, string projectConfigFile)
        {
            (configFile,projectConfigFile) = LoadConfigFiles(configFile, projectConfigFile);
            dataSources = new DataSources(configFile, projectConfigFile);
            traceAnalysis = new TraceabilityAnalysis(projectConfigFile);
            ProcessConfigs(configFile,projectConfigFile);
        }

        private (string, string) LoadConfigFiles(string configFile, string projectConfigFile)
        {
            string config;
            string projectConfig;
            try
            {
                config = File.ReadAllText(configFile);
            }
            catch(IOException e)
            {
                throw new Exception("Unable to read config file");
            }
            try
            {
                projectConfig = File.ReadAllText(projectConfigFile);
            }
            catch(IOException e)
            {
                throw new Exception("Unable to read project config file");
            }
            return (config, projectConfig);
        }

        private void ProcessConfigs(string config, string projectConfig)
        {
            var toml = Toml.Parse(projectConfig).ToModel();
            foreach (var docloc in (TomlTable)toml["DocumentLocations"])
            {
                TomlArray arr = (TomlArray)docloc.Value;
                if ((string)arr[0] != string.Empty)
                {
                    documents[(string)arr[0]] = ((string)arr[1], File.ReadAllText((string)arr[1]));
                }
            }
            toml = Toml.Parse(config).ToModel();
            if(((string)toml["OutputFormat"]).ToUpper() == "HTML")
            {
                outputFormat = DocumentFormat.HTML;
            }
            outputDir = (string)toml["OutputDirectory"];
        }

        public void GenerateDocs()
        {
            var tempDic = new Dictionary<string, (string, string)>();
            foreach (var doc in documents)
            {
                //load the document from disk into a document structure
                Document document = new Document(doc.Key);
                document.FromMarkDown(doc.Value.Item2);
                //go over the tag list to determine what information should be collected from where
                foreach(var tag in document.RoboClerkTags)
                {
                    if(tag.Source == DataSource.Trace)
                    {
                        //grab all trace tags and add them to the trace analysis
                        traceAnalysis.AddTraceTag(document.Title, tag);
                        continue;
                    }
                    if (tag.Source != DataSource.Info && tag.Source != DataSource.Unknown)
                    {
                        if (tag.Source == DataSource.Config)
                        {
                            tag.Contents = dataSources.GetConfigValue(tag.ContentCreatorID);
                        }
                        else
                        {
                            IContentCreator contentCreator = GetContentObject(tag);
                            if (contentCreator != null)
                            {
                                tag.Contents = contentCreator.GetContent(tag,dataSources,traceAnalysis,document.Title);
                            }
                            else
                            {
                                tag.Contents = "UNABLE TO CREATE CONTENT, ENSURE THAT THE CONTENT CREATOR CLASS IS KNOWN TO ROBOCLERK.\n";
                            }
                        }
                    }
                }
                tempDic[doc.Key] = (doc.Value.Item1,document.ToMarkDown());
            }
            documents = tempDic;
        }

        public void SaveDocumentsToDisk()
        {
            if (outputFormat == DocumentFormat.Markdown)
            {
                foreach (var doc in documents)
                {
                    File.WriteAllText(Path.Combine(outputDir,Path.GetFileName(doc.Value.Item1)), doc.Value.Item2);
                }
            }
            else if (outputFormat == DocumentFormat.HTML)
            {
                foreach (var doc in documents)
                {
                    string htmlFile = Path.GetFileName(Path.ChangeExtension(doc.Value.Item1, ".html"));
                    File.WriteAllText(Path.Combine(outputDir,htmlFile), RoboClerkMarkdown.ConvertMarkdownToHTML(doc.Value.Item2));
                }
            }
        }

        private IContentCreator GetContentObject(RoboClerkTag tag)
        {
            Assembly thisAssembly = Assembly.GetAssembly(this.GetType());
            Type[] contentTypes = thisAssembly
                .GetTypes()
                .Where(t => typeof(IContentCreator).IsAssignableFrom(t) && t.IsClass)
                .ToArray();

            foreach (Type contentType in contentTypes)
            {
                if (contentType.Name == tag.ContentCreatorID)
                {
                    return Activator.CreateInstance(contentType) as IContentCreator;
                }
            }
            return null;
        }
    }
}
