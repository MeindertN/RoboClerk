using RoboClerk.Configuration;
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
            using var ms = new MemoryStream();
            using (var zlib = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            {
                zlib.Write(utf8, 0, utf8.Length);
            }

            // 3) Base64
            string base64 = Convert.ToBase64String(ms.ToArray());

            // 4) URL-safe alphabet
            return base64
                .Replace('+', '-')
                .Replace('/', '_');
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
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                byte[] imagebytes = DownloadImageAsync($"{krokiURL}/{diagramType}/{imageFormat}/{base64}", cts.Token).Result;

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
                
                if (configuration.OutputFormat.ToUpper() == "HTML")
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
