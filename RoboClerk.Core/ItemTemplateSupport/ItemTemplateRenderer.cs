using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;
using System.Text;

namespace RoboClerk
{
    public class ItemTemplateRenderer
    {
        private CompiledItemTemplate compiledTemplate;

        private ItemTemplateRenderer() 
        {
        }

        /// <summary>
        /// Creates an ItemTemplateRenderer from a string template content
        /// </summary>
        /// <param name="templateContent">string containing the template content</param>
        /// <param name="templateIdentifier">Identifier for the template (used for caching and error reporting)</param>
        /// <returns>A new ItemTemplateRenderer instance</returns>
        public static ItemTemplateRenderer FromString(string templateContent, string templateIdentifier)
        {
            var compiledTemplate = ItemTemplateFactory.GetOrCompile(templateContent, templateIdentifier);
            return new ItemTemplateRenderer(compiledTemplate);
        }

        /// <summary>
        /// Creates an ItemTemplateRenderer from a template stream
        /// </summary>
        /// <param name="templateStream">Stream containing the template content</param>
        /// <param name="templateIdentifier">Identifier for the template (used for caching and error reporting)</param>
        /// <returns>A new ItemTemplateRenderer instance</returns>
        public static ItemTemplateRenderer FromStream(Stream templateStream, string templateIdentifier)
        {
            var compiledTemplate = ItemTemplateFactory.GetOrCompileFromStream(templateStream, templateIdentifier);
            return new ItemTemplateRenderer(compiledTemplate);
        }

        /// <summary>
        /// Creates an ItemTemplateRenderer by loading an existing compiled template from cache using the file identifier
        /// </summary>
        /// <param name="fileIdentifier">The file identifier used as cache key</param>
        /// <returns>A new ItemTemplateRenderer instance if template exists in cache</returns>
        /// <exception cref="InvalidOperationException">Thrown when no compiled template exists in cache for the given identifier</exception>
        public static ItemTemplateRenderer FromCachedTemplate(string fileIdentifier)
        {
            var compiledTemplate = ItemTemplateFactory.GetFromCache(fileIdentifier);
            if (compiledTemplate == null)
            {
                throw new InvalidOperationException($"No compiled template found in cache for identifier: {fileIdentifier}");
            }
            return new ItemTemplateRenderer(compiledTemplate);
        }

        /// <summary>
        /// Checks if a compiled template exists in the cache based on the file identifier
        /// </summary>
        /// <param name="fileIdentifier">The file identifier to check for in the cache</param>
        /// <returns>True if a compiled template exists in cache for the given identifier, false otherwise</returns>
        public static bool ExistsInCache(string fileIdentifier)
        {
            return ItemTemplateFactory.ExistsInCache(fileIdentifier);
        }

        /// <summary>
        /// Internal constructor for creating renderer with pre-compiled template
        /// </summary>
        /// <param name="compiledTemplate">Pre-compiled template</param>
        private ItemTemplateRenderer(CompiledItemTemplate compiledTemplate)
        {
            this.compiledTemplate = compiledTemplate;
        }

        public string RenderItemTemplate<T>(ScriptingBridge<T> bridge) where T : Item
        {
            return compiledTemplate.Render(bridge);
        }
    }
}
