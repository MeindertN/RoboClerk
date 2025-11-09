using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;
using System.Text;
using NLog;
using System.Collections.Immutable;

namespace RoboClerk
{
    public class CompiledItemTemplate : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly Script<object> startScript;
        private readonly List<(Script<string> script, int start, int end)> segmentScripts;
        private readonly string originalContent;
        private readonly (string code, int start, int end) startSegment;
        private bool disposed = false;

        private CompiledItemTemplate(string content, Script<object> start, 
            List<(Script<string>, int, int)> segments, (string, int, int) startSeg)
        {
            originalContent = content;
            startScript = start;
            segmentScripts = segments;
            startSegment = startSeg;
        }

        public static CompiledItemTemplate Compile(string templateContent)
        {
            var parser = new ItemTemplateParser(templateContent);
            
            if (parser.StartSegment.Item2 < 0 || parser.StartSegment.Item3 < 0)
            {
                // Return a template that just returns the original content
                var options = ScriptOptions.Default
                    .WithReferences(Assembly.GetExecutingAssembly())
                    .WithImports("System", "System.Linq", "System.Collections.Generic");
                
                var dummyScript = CSharpScript.Create<object>("null", options);
                return new CompiledItemTemplate(templateContent, dummyScript, new List<(Script<string>, int, int)>(), ("", -1, -1));
            }

            var scriptOptions = ScriptOptions.Default
                .WithReferences(Assembly.GetExecutingAssembly())
                .WithImports("System", "System.Linq", "System.Collections.Generic");

            // Create start script with ScriptingBridge as globals type so template scripts can access bridge members
            var startScript = CSharpScript.Create<object>(parser.StartSegment.Item1, scriptOptions, globalsType: typeof(ScriptingBridge));
            
            // Pre-compile and validate the start script
            try
            {
                var compilation = startScript.GetCompilation();
                var diagnostics = compilation.GetDiagnostics();
                var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
                
                if (errors.Any())
                {
                    var errorMessages = errors.Select(e => $"Line {e.Location.GetLineSpan().StartLinePosition.Line + 1}: {e.GetMessage()}");
                    var errorMessage = $"Compilation errors in template start segment:\n{string.Join("\n", errorMessages)}\n\nStart segment code:\n{parser.StartSegment.Item1}";
                    
                    throw new CompilationErrorException(errorMessage, errors.ToImmutableArray());
                }
            }
            catch (CompilationErrorException)
            {
                throw; 
            }
            catch (Exception ex)
            {
                var errorMessage = $"Unexpected error compiling template start segment: {ex.Message}\n\nStart segment code:\n{parser.StartSegment.Item1}";
                logger.Error(errorMessage);
                throw;
            }

            var segmentScripts = new List<(Script<string>, int, int)>();
            int i = 0;
            foreach (var segment in parser.Segments )
            {
                try
                {
                    // Continue from start script to maintain globals context
                    var script = startScript.ContinueWith<string>(segment.Item1);
                    
                    // Pre-compile and validate segment scripts
                    var compilation = script.GetCompilation();
                    var diagnostics = compilation.GetDiagnostics();
                    var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
                    
                    if (errors.Any())
                    {
                        var errorMessages = errors.Select(e => $"Line {e.Location.GetLineSpan().StartLinePosition.Line + 1}: {e.GetMessage()}");
                        var errorMessage = $"Compilation errors in template segment {i + 1}:\n{string.Join("\n", errorMessages)}\n\nSegment code:\n{segment.Item1}";
                        
                        throw new CompilationErrorException(errorMessage,errors.ToImmutableArray());
                    }
                    
                    segmentScripts.Add((script, segment.Item2, segment.Item3));
                    i++;
                }
                catch (CompilationErrorException)
                {
                    throw; 
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Unexpected error compiling template segment {i + 1}: {ex.Message}\n\nSegment code:\n{segment.Item1}";
                    logger.Error(errorMessage);
                    throw;
                }
            }

            logger.Debug($"Successfully compiled template with {segmentScripts.Count} segments");
            return new CompiledItemTemplate(templateContent, startScript, segmentScripts, parser.StartSegment);
        }

        public string Render<T>(ScriptingBridge<T> bridge) where T : Item
        {
            // If no valid segments, return original content
            if (startSegment.start < 0 || startSegment.end < 0)
            {
                return originalContent;
            }

            // Pass the bridge as globals to the scripting engine - this is critical for template scripts
            var beginState = startScript.RunAsync(bridge).Result;
            StringBuilder sb = new StringBuilder(originalContent);
            
            // Process segments in reverse order to avoid index recalculation
            var sortedSegments = segmentScripts.OrderByDescending(s => s.start).ToArray();
            
            foreach (var (script, start, end) in sortedSegments)
            {
                // Continue with the same bridge globals context
                var state = script.RunFromAsync(beginState).Result;
                string result = state.ReturnValue ?? string.Empty;
                
                sb.Remove(start, end - start);
                sb.Insert(start, result);
            }
            
            // Remove start segment
            sb.Remove(startSegment.start, startSegment.end - startSegment.start);
            return sb.ToString();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                disposed = true;
            }
        }
    }
}