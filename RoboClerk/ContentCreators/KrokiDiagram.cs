using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RoboClerk.ContentCreators
{
    internal class KrokiDiagram : ContentCreatorBase
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly IFileProviderPlugin fileSystem;

        public KrokiDiagram(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf, IFileProviderPlugin fs)
            : base(data, analysis, conf)
        {
            fileSystem = fs;
        }

        public static byte[] DownloadImage(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL must not be empty", nameof(url));
           
            // Fetch the image bytes
            return _httpClient.GetByteArrayAsync(url).Result;
        }

        public override string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            //we are hardcoding the kroki URL for now
            string krokiURL = "https://kroki.io";

            //take the tag contents and convert them to base64
            byte[] bytes = Encoding.UTF8.GetBytes(tag.Contents);
            string base64 = Convert.ToBase64String(bytes);

            string diagramType = tag.GetParameterOrDefault("type", "plantuml");
            string imageFormat = tag.GetParameterOrDefault("format", "png");
            string imageCaption = tag.GetParameterOrDefault("caption", string.Empty);
            logger.Debug($"Retrieving an image from the kroki server \"{krokiURL}\". Diagram type: {diagramType}. Image format: {imageFormat}. With the following caption: \"{imageCaption}\".");
            
            //download the image and save it to the media directory
            byte[] imagebytes = DownloadImage($"{krokiURL}/{diagramType}/{imageFormat}/{base64}");

            //determine the file path for the output
            string toplineDir = fileSystem.GetFileName(configuration.MediaDir);
            string targetDir = fileSystem.Combine(configuration.OutputDir, toplineDir);
            string fileName = $"{Guid.NewGuid():N}.{imageFormat}";
            string filePath = $"{targetDir}/{fileName}";
            fileSystem.WriteAllBytes(filePath, imagebytes);

            if (configuration.OutputFormat.ToUpper() == "HTML")
            {
                string imagetag = $"<img src=\"{toplineDir}/{fileName}\" alt=\"{imageCaption}\" />";
                if( imageCaption != string.Empty)
                {
                    imagetag = $"{imagetag}\n<figcaption>{imageCaption}<figcaption>";
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
    }
}
