using Microsoft.Extensions.DependencyInjection;
using RoboClerk.AISystem;
using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;

namespace RoboClerk.ContentCreators
{
    /// <summary>
    /// Factory implementation that uses dependency injection to create content creators.
    /// This eliminates the need for large if/else chains and manual instantiation.
    /// </summary>
    public class ContentCreatorFactory : IContentCreatorFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<DataSource, Type> _sourceTypeMap;
        private readonly Dictionary<string, Type> _dynamicTypeMap;
        private readonly ITraceabilityAnalysis _traceAnalysis;

        public ContentCreatorFactory(IServiceProvider serviceProvider, ITraceabilityAnalysis traceAnalysis)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _sourceTypeMap = new Dictionary<DataSource, Type>();
            _dynamicTypeMap = new Dictionary<string, Type>();
            _traceAnalysis = traceAnalysis;

            // Map source types to their corresponding content creator types
            _sourceTypeMap[DataSource.Trace] = typeof(Trace);
            _sourceTypeMap[DataSource.Config] = typeof(ConfigurationValue);
            _sourceTypeMap[DataSource.Post] = typeof(PostLayout);
            _sourceTypeMap[DataSource.Reference] = typeof(Reference);
            _sourceTypeMap[DataSource.Document] = typeof(Document);
            _sourceTypeMap[DataSource.AI] = typeof(AIContentCreator);

            // Build dynamic type map for content creators that are resolved by name
            BuildDynamicTypeMap();
        }

        private void BuildDynamicTypeMap()
        {
            // Get all types that implement IContentCreator in the current assembly
            var assembly = Assembly.GetAssembly(typeof(IContentCreator));
            if (assembly == null)
            {
                throw new InvalidOperationException("Could not get the assembly for IContentCreator.");
            }
            var contentCreatorTypes = assembly.GetTypes()
                .Where(t => typeof(IContentCreator).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                .Where(t => !_sourceTypeMap.Values.Contains(t)); // Exclude already mapped types

            foreach (var type in contentCreatorTypes)
            {
                _dynamicTypeMap[type.Name.ToUpper()] = type;
            }
        }

        public IContentCreator CreateContentCreator(DataSource source, string? contentCreatorId = null)
        {
            // Handle special case for Comment source
            if (source == DataSource.Comment)
            {
                return new CommentContentCreator();
            }

            // Try to get the type from the source map first
            if (_sourceTypeMap.TryGetValue(source, out Type sourceType))
            {
                return (IContentCreator)_serviceProvider.GetRequiredService(sourceType);
            }

            var te = _traceAnalysis.GetTraceEntityForAnyProperty(contentCreatorId);

            var resolveName = (te == default(TraceEntity) ? contentCreatorId : te.ID);

            // For dynamic resolution (when contentCreatorId is provided)
            if (!string.IsNullOrEmpty(resolveName))
            {
                if (_dynamicTypeMap.TryGetValue(resolveName.ToUpper(), out Type dynamicType))
                {
                    return (IContentCreator)_serviceProvider.GetRequiredService(dynamicType);
                }
            }

            throw new InvalidOperationException($"No content creator found for source '{source}' and content creator ID '{contentCreatorId}'");
        }
    }

    /// <summary>
    /// Simple content creator for Comment source that returns empty string.
    /// </summary>
    internal class CommentContentCreator : IContentCreator
    {
        public string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            return string.Empty;
        }
    }
} 