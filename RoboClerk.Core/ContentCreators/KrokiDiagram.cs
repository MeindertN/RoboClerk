using RoboClerk.Core.Configuration;
using RoboClerk.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RoboClerk.ContentCreators
{
    internal class KrokiDiagram : ContentCreatorBase
    {
        private static readonly HttpClient _httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(30) // Set a reasonable timeout
        };
        private readonly IFileProviderPlugin fileSystem;


        public KrokiDiagram(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf, IFileProviderPlugin fs)
            : base(data, analysis, conf)
        {
            fileSystem = fs;
        }

        public override ContentCreatorMetadata GetMetadata()
        {
            var metadata = new ContentCreatorMetadata("Web", "Kroki Diagram", 
                "Generates diagrams using the Kroki web service (PlantUML, GraphViz, Mermaid, etc.)");
            
            metadata.Category = "Diagrams";

            var krokiTag = new ContentCreatorTag("KrokiDiagram", "Generates a diagram from embedded diagram source code");
            krokiTag.Category = "Diagram Generation";
            krokiTag.Description = "Sends diagram source code to the Kroki web service and embeds the resulting image. " +
                "The diagram source should be placed between the opening and closing tags. " +
                "Supports PlantUML, GraphViz, Mermaid, and many other diagram types.";
            
            krokiTag.Parameters.Add(new ContentCreatorParameter("type", 
                "Type of diagram (e.g., plantuml, graphviz, mermaid, ditaa, etc.)", 
                ParameterValueType.String, required: false, defaultValue: "plantuml")
            {
                AllowedValues = new List<string> { "plantuml", "graphviz", "mermaid", "ditaa", "blockdiag", "seqdiag", "actdiag", "nwdiag", "c4plantuml" },
                ExampleValue = "plantuml"
            });
            
            krokiTag.Parameters.Add(new ContentCreatorParameter("format", 
                "Output image format", 
                ParameterValueType.String, required: false, defaultValue: "png")
            {
                AllowedValues = new List<string> { "png", "svg", "base64", "text", "utext", "pdf" },
                ExampleValue = "png"
            });
            
            krokiTag.Parameters.Add(new ContentCreatorParameter("caption", 
                "Caption text to display with the diagram", 
                ParameterValueType.String, required: false)
            {
                ExampleValue = "System Architecture Diagram"
            });
            
            krokiTag.ExampleUsage = "@@Web:KrokiDiagram(type=plantuml,format=png,caption=Example Diagram)\n@startuml\nAlice -> Bob: Hello\n@enduml\n@@";
            metadata.Tags.Add(krokiTag);

            return metadata;
        }

        public static async Task<byte[]> DownloadImageAsync(string url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL must not be empty", nameof(url));

            try
            {
                logger.Debug($"Attempting to download image from URL: {url}");
                // Fetch the image bytes with timeout handling
                return await _httpClient.GetByteArrayAsync(url, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                logger.Error($"HTTP request failed when downloading from Kroki: {ex.Message}");
                throw new Exception($"Failed to download diagram from Kroki service. The service may be temporarily unavailable. Error: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || cancellationToken.IsCancellationRequested)
            {
                logger.Error($"Request to Kroki service timed out after 30 seconds: {ex.Message}");
                throw new Exception("Request to Kroki service timed out. The service may be temporarily unavailable or experiencing high load.", ex);
            }
            catch (Exception ex)
            {
                logger.Error($"Unexpected error when downloading from Kroki: {ex.Message}");
                throw new Exception($"Unexpected error occurred while downloading diagram from Kroki service: {ex.Message}", ex);
            }
        }

        private string EncodeToKroki(string diagramSource)
        {
            // 1) UTF-8 bytes
            byte[] utf8 = Encoding.UTF8.GetBytes(diagramSource);

            // 2) Compress with zlib wrapper at best compression
            using (var ms = new MemoryStream())
            {
                using (ZLibStream zlib = new(ms, CompressionMode.Compress, true))
                {
                    zlib.Write(utf8);
                }
                var encodedOutput = Convert.ToBase64String(ms.ToArray()).Replace('+', '-').Replace('/', '_');

                return encodedOutput;
            }
        }

        public override string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            //we are hardcoding the kroki URL for now
            string krokiURL = "https://kroki.io";

            //take the tag contents and convert them to base64
            string base64 = EncodeToKroki(tag.Contents);

            string diagramType = tag.GetParameterOrDefault("type", "plantuml");
            string imageFormat = tag.GetParameterOrDefault("format", "png");
            string imageCaption = tag.GetParameterOrDefault("caption", string.Empty);
            logger.Debug($"Retrieving an image from the kroki server \"{krokiURL}\". Diagram type: {diagramType}. Image format: {imageFormat}. With the following caption: \"{imageCaption}\".");
            
            try
            {
                //download the image and save it to the media directory with timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                byte[] imagebytes = DownloadImageAsync($"{krokiURL}/{diagramType}/{imageFormat}/{base64}", cts.Token).Result;

                //determine the file path for the output
                string toplineDir = fileSystem.GetFileName(configuration.MediaDir);
                string targetDir = fileSystem.Combine(configuration.OutputDir, toplineDir);
                string fileName = $"{Guid.NewGuid():N}.{imageFormat}";
                string filePath = $"{targetDir}/{fileName}";
                fileSystem.WriteAllBytes(filePath, imagebytes);

                if (configuration.OutputFormat.ToUpper() == "HTML" || configuration.OutputFormat.ToUpper() == "DOCX" )
                {
                    string imagetag = $"<img src=\"{toplineDir}/{fileName}\" alt=\"{imageCaption}\" />";
                    if( imageCaption != string.Empty)
                    {
                        imagetag = $"{imagetag}\n<figcaption>{imageCaption}</figcaption>";
                    }
                    return imagetag;
                }
                else if(configuration.OutputFormat.ToUpper() == "ASCIIDOC")
                {
                    string imagetag = $"image::{toplineDir}/{fileName}[{imageCaption}]";
                    if (imageCaption != string.Empty)
                    {
                        imagetag = $".{imageCaption}\n{imagetag}";
                    }
                    return imagetag;
                }
                else
                {
                    throw new Exception($"Unknown output format {configuration.OutputFormat}.");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to generate Kroki diagram: {ex.Message}");
                
                // Return a fallback representation instead of throwing
                string fallbackContent = $"[Diagram unavailable: {ex.Message}]";
                
                if (configuration.OutputFormat.ToUpper() == "HTML" || configuration.OutputFormat.ToUpper() == "DOCX")
                {
                    return $"<div class=\"diagram-error\" style=\"border: 1px solid #ff0000; padding: 10px; background-color: #ffe6e6;\">" +
                           $"<strong>Diagram Error:</strong> {fallbackContent}" +
                           (imageCaption != string.Empty ? $"<br><em>Caption: {imageCaption}</em>" : "") +
                           "</div>";
                }
                else if(configuration.OutputFormat.ToUpper() == "ASCIIDOC")
                {
                    string errorBlock = $"[ERROR]\n====\nDiagram Error: {fallbackContent}";
                    if (imageCaption != string.Empty)
                    {
                        errorBlock += $"\nCaption: {imageCaption}";
                    }
                    errorBlock += "\n====";
                    return errorBlock;
                }
                else
                {
                    return fallbackContent;
                }
            }
        }
    }
}
