using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Scripting;
using NLog;

namespace RoboClerk
{
    public static class ItemTemplateFactory
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly ConcurrentDictionary<string, CompiledItemTemplate> _templateCache = new();
        private static readonly ConcurrentDictionary<string, string> _templateSources = new(); // Track template sources for error reporting
        private static readonly object _lockObject = new object();

        /// <summary>
        /// Gets a compiled template from cache or compiles and caches a new one
        /// </summary>
        /// <param name="templateContent">The template content to compile</param>
        /// <returns>A compiled template</returns>
        /// <exception cref="CompilationErrorException">Thrown when template compilation fails</exception>
        public static CompiledItemTemplate GetOrCompile(string templateContent, string templateIdentifier)
        {          
            return _templateCache.GetOrAdd(templateIdentifier, _ => 
            {
                logger.Debug($"Compiling new template (cache key: {templateIdentifier})");
                var template = CompiledItemTemplate.Compile(templateContent);
                _templateSources[templateIdentifier] = templateContent.Length > 500 
                    ? templateContent.Substring(0, 500) + "..." 
                    : templateContent;
                return template;
            });
        }

        /// <summary>
        /// Gets a compiled template from cache or compiles and caches a new one from a stream
        /// </summary>
        /// <param name="templateStream">Stream containing the template content</param>
        /// <param name="templateIdentifier">Identifier for the template (used for caching and error reporting)</param>
        /// <returns>A compiled template</returns>
        /// <exception cref="CompilationErrorException">Thrown when template compilation fails</exception>
        /// <exception cref="IOException">Thrown when template stream cannot be read</exception>
        public static CompiledItemTemplate GetOrCompileFromStream(Stream templateStream, string templateIdentifier)
        {
            return _templateCache.GetOrAdd(templateIdentifier, _ =>
            {
                logger.Debug($"Compiling template from stream: {templateIdentifier}");
                string templateContent;
                using var reader = new StreamReader(templateStream);
                templateContent = reader.ReadToEnd();

                var template = CompiledItemTemplate.Compile(templateContent);
                _templateSources[templateIdentifier] = templateContent.Length > 500
                    ? templateContent.Substring(0, 500) + "..."
                    : templateContent;
                return template;
            });
        }

        /// <summary>
        /// Checks if a compiled template exists in the cache for the given identifier
        /// </summary>
        /// <param name="templateIdentifier">The template identifier to check for in the cache</param>
        /// <returns>True if a compiled template exists in cache for the given identifier, false otherwise</returns>
        public static bool ExistsInCache(string templateIdentifier)
        {
            return _templateCache.ContainsKey(templateIdentifier);
        }

        /// <summary>
        /// Gets a compiled template from cache if it exists
        /// </summary>
        /// <param name="templateIdentifier">The template identifier to retrieve from cache</param>
        /// <returns>The compiled template if found in cache, null otherwise</returns>
        public static CompiledItemTemplate? GetFromCache(string templateIdentifier)
        {
            _templateCache.TryGetValue(templateIdentifier, out var template);
            return template;
        }

        /// <summary>
        /// Clears the template cache. Use this if memory usage becomes a concern.
        /// </summary>
        public static void ClearCache()
        {
            lock (_lockObject)
            {
                logger.Info($"Clearing template cache ({_templateCache.Count} templates)");
                foreach (var template in _templateCache.Values)
                {
                    template.Dispose();
                }
                _templateCache.Clear();
                _templateSources.Clear();
            }
        }

        /// <summary>
        /// Gets the current cache size
        /// </summary>
        public static int CacheSize => _templateCache.Count;

        /// <summary>
        /// Removes a specific template from cache
        /// </summary>
        /// <param name="templateIdentifier">The template identifier to remove</param>
        /// <returns>True if the template was found and removed</returns>
        public static bool RemoveFromCache(string templateIdentifier)
        {
            if (_templateCache.TryRemove(templateIdentifier, out var template))
            {
                logger.Debug($"Removed template from cache: {templateIdentifier}");
                template.Dispose();
                _templateSources.TryRemove(templateIdentifier, out _);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets diagnostic information about cached templates
        /// </summary>
        /// <returns>Dictionary with cache keys and their source information</returns>
        public static Dictionary<string, string> GetCacheDiagnostics()
        {
            return new Dictionary<string, string>(_templateSources);
        }
    }
}