using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Tomlyn.Model;
using TreeSitter;

namespace RoboClerk
{
    public abstract class SourceCodeAnalysisPluginBase : DataSourcePluginBase
    {
        protected bool subDir = false;
        protected List<string> directories = new List<string>();
        protected List<string> fileMasks = new List<string>();
        protected List<string> sourceFiles = new List<string>();
        protected GitRepository gitRepo = null;

        private static bool _treeSitterInitialized = false;
        private static readonly object _treeSitterInitLock = new object();

        // Language caching for performance
        private static readonly ConcurrentDictionary<string, Lazy<Language>> _languages = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CSharp"] = new(() => new Language("CSharp"), true),
            ["CPP"] = new(() => new Language("CPP"), true),
            ["C"] = new(() => new Language("C"), true),
            ["Python"] = new(() => new Language("Python"), true),
            ["JavaScript"] = new(() => new Language("JavaScript"), true),
            ["TypeScript"] = new(() => new Language("TypeScript"), true),
            ["Java"] = new(() => new Language("Java"), true),
            ["Go"] = new(() => new Language("Go"), true),
            ["Rust"] = new(() => new Language("Rust"), true),
        };

        // Parser caching for performance (thread-local for thread safety)
        private static readonly ConcurrentDictionary<string, ThreadLocal<Parser>> _parsers =
            new(StringComparer.OrdinalIgnoreCase);

        public SourceCodeAnalysisPluginBase(IFileSystem fileSystem)
            : base(fileSystem)
        {
            // Initialize TreeSitter for all source code analysis plugins
            EnsureTreeSitterInitialized();
        }

        public override void InitializePlugin(IConfiguration configuration)
        {
            var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
            subDir = (bool)config["SubDirs"];
            foreach (var obj in (TomlArray)config["TestDirectories"])
            {
                directories.Add((string)obj);
            }

            foreach (var obj in (TomlArray)config["FileMasks"])
            {
                fileMasks.Add((string)obj);
            }

            try
            {
                if (config.ContainsKey("UseGit") && (bool)config["UseGit"])
                {
                    gitRepo = new GitRepository(configuration);
                }
            }
            catch (Exception)
            {
                logger.Error($"Error opening git repo at project root \"{configuration.ProjectRoot}\" even though the {name}.toml configuration file UseGit setting was set to true.");
                throw;
            }
        }

        protected void ScanDirectoriesForSourceFiles()
        {
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var testDirectory in directories)
            {
                try
                {
                    foreach (var fileMask in fileMasks)
                    {
                        try
                        {
                            var paths = fileSystem.Directory.EnumerateFiles(
                                testDirectory,
                                fileMask,
                                subDir ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
                            );

                            foreach (var p in paths)
                                found.Add(fileSystem.Path.GetFullPath(p));
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, $"Error processing file mask '{fileMask}' in directory '{testDirectory}'");
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Error reading directory {testDirectory}");
                    throw;
                }
            }

            sourceFiles.Clear();
            sourceFiles.AddRange(found);
        }


        #region TreeSitter Infrastructure

        private static void EnsureTreeSitterInitialized()
        {
            if (_treeSitterInitialized) return;

            lock (_treeSitterInitLock)
            {
                if (_treeSitterInitialized) return;

                try
                {
                    // Set up native library resolver for TreeSitter assembly
                    // SetDllImportResolver can only be set once per assembly
                    NativeLibrary.SetDllImportResolver(typeof(Language).Assembly, ResolveTreeSitterNativeLibrary);
                    _treeSitterInitialized = true;

                    logger.Debug("TreeSitter native library resolver initialized successfully for source code analysis plugins");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to initialize TreeSitter native library resolver");
                    throw new InvalidOperationException("Failed to initialize TreeSitter native library resolver", ex);
                }
            }
        }

        private static IntPtr ResolveTreeSitterNativeLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            try
            {
                var rid = GetRuntimeIdentifier();
                var ext = GetNativeLibraryExtension();
                var names = new List<string> { libraryName + ext };

                // Non-Windows: also try lib-prefixed name (e.g., libtree-sitter-csharp.so)
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    names.Add("lib" + libraryName + ext);

                // Hyphenation fallback for C#
                if (libraryName.Equals("tree-sitter-csharp", StringComparison.OrdinalIgnoreCase))
                {
                    names.Add("tree-sitter-c-sharp" + ext);
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        names.Add("libtree-sitter-c-sharp" + ext);
                }

                foreach (var file in names)
                {
                    var p1 = Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", file);
                    if (File.Exists(p1)) return NativeLibrary.Load(p1, assembly, searchPath);

                    var asmDir = Path.GetDirectoryName(assembly.Location);
                    if (!string.IsNullOrEmpty(asmDir))
                    {
                        var p2 = Path.Combine(asmDir, "runtimes", rid, "native", file);
                        if (File.Exists(p2)) return NativeLibrary.Load(p2, assembly, searchPath);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Error resolving native library '{libraryName}', falling back to default resolution");
            }
            return IntPtr.Zero;
        }

        private static string GetRuntimeIdentifier()
        {
            bool x64 = RuntimeInformation.ProcessArchitecture == Architecture.X64;
            bool x86 = RuntimeInformation.ProcessArchitecture == Architecture.X86;
            bool arm64 = RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return arm64 ? "win-arm64" : x64 ? "win-x64" : "win-x86";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return arm64 ? "linux-arm64" : "linux-x64";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return arm64 ? "osx-arm64" : "osx-x64";

            throw new PlatformNotSupportedException($"Unsupported platform: {RuntimeInformation.OSDescription}");
        }

        private static string GetNativeLibraryExtension()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return ".dll";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return ".so";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return ".dylib";

            return ".so"; // Default fallback
        }

        #endregion

        #region TreeSitter Helper Methods

        protected Language CreateLanguage(string languageName)
        {
            var lazy = _languages.GetOrAdd(
                languageName,
                name => new Lazy<Language>(() => new Language(name), isThreadSafe: true)
                );
            return lazy.Value;
        }

        protected Parser CreateParser(string languageName)
        {
            var lang = CreateLanguage(languageName);
            var tl = _parsers.GetOrAdd(languageName, _ => new ThreadLocal<Parser>(() => new Parser(lang)));
            return tl.Value!;
        }

        protected Query CreateQuery(Language language, string queryText)
        {
            return new Query(language, queryText);
        }

        protected Tree ParseSourceCode(string language, string sourceCode)
        {
            var parser = CreateParser(language);
            return parser.Parse(sourceCode);
        }

        protected IEnumerable<QueryMatch> ExecuteQuery(Language language, Tree tree, string queryText)
        {
            using var query = CreateQuery(language, queryText);
            // Force evaluation while 'query' is alive to avoid disposal issues
            return query.Execute(tree.RootNode).Matches.ToArray();
        }

        protected IEnumerable<QueryCapture> ExecuteQueryCaptures(Language language, Tree tree, string queryText)
        {
            using var query = CreateQuery(language, queryText);
            // Force evaluation while 'query' is alive to avoid disposal issues
            return query.Execute(tree.RootNode).Captures.ToArray();
        }

        protected string GetNodeText(Node node, string sourceCode = null)
        {
            // Try using node.Text if available, otherwise fall back to manual extraction
            try
            {
                return node.Text;
            }
            catch
            {
                // Fallback for older TreeSitter versions or different bindings
                if (sourceCode != null)
                {
                    return sourceCode.Substring(node.StartIndex, node.EndIndex - node.StartIndex);
                }
                throw new ArgumentException("sourceCode parameter required when node.Text is not available", nameof(sourceCode));
            }
        }

        protected int GetNodeLineNumber(Node node, string sourceCode)
        {
            if (sourceCode==null)
                throw new ArgumentNullException(nameof(sourceCode), "sourceCode is required for line number calculation");

            return node.StartPosition.Row + 1;            
        }

        #endregion
    }
}
