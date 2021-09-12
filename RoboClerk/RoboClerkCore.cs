
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
        
        public RoboClerkCore(string configFile, string projectConfigFile)
        {
            dataSources = new DataSources(configFile, projectConfigFile);
            ProcessConfig(projectConfigFile);
        }
        
        private void ProcessConfig(string configFile)
        {
            string config = File.ReadAllText(configFile);
            var toml = Toml.Parse(config).ToModel();
            foreach (var docloc in (TomlTable)toml["DocumentLocations"])
            {
                TomlArray arr = (TomlArray)docloc.Value;
                documents[(string)arr[0]] = ((string)arr[1],File.ReadAllText((string)arr[1]));
            }
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
                    if(tag.Source != DataSource.Info && tag.Source != DataSource.Unknown)
                    {
                        if (tag.Source == DataSource.Config)
                        {
                            tag.Contents = dataSources.GetConfigValue(tag.ID);
                        }
                        else
                        {
                            IContentCreator contentCreator = GetContentObject(tag);
                            if (contentCreator != null)
                            {
                                tag.Contents = contentCreator.GetContent(dataSources);
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

        public void SaveMarkdownDocumentsToDisk(DocumentFormat format)
        {
            if (format == DocumentFormat.Markdown)
            {
                foreach (var doc in documents)
                {
                    File.WriteAllText(doc.Value.Item1, doc.Value.Item2);
                }
            }
            else if (format == DocumentFormat.HTML)
            {
                foreach (var doc in documents)
                {
                    File.WriteAllText(Path.ChangeExtension(doc.Value.Item1, ".html"), RoboClerkMarkdown.ConvertMarkdownToHTML(doc.Value.Item2));
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
                if (contentType.Name == tag.ID)
                {
                    return Activator.CreateInstance(contentType) as IContentCreator;
                }
            }
            return null;
        }
    }
}
