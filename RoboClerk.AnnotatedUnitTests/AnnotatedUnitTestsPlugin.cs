using RoboClerk.Core.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using Tomlyn.Model;
using TreeSitter;

namespace RoboClerk.AnnotatedUnitTests
{
    /// <summary>
    /// Annotated Unit Test plugin with **Tree‑sitter only** multi-language support (C#, Java, Python, TypeScript/JavaScript).
    /// One language per project (configured via TOML). Extracts method-level annotations/decorators and their named arguments
    /// using AST captures only (no legacy string scanning or regex fallbacks).
    /// </summary>
    public class AnnotatedUnitTestPlugin : SourceCodeAnalysisPluginBase
    {
        private enum Lang { CSharp, Java, Python, TypeScript, JavaScript }

        // Map TOML fields -> UTInformation
        private readonly Dictionary<string, UTInformation> information = new();

        public AnnotatedUnitTestPlugin(IFileProviderPlugin fileSystem) 
            : base(fileSystem)
        {
            name = "AnnotatedUnitTestPlugin";
            description = "Analyzes a project's source code to extract unit test information for RoboClerk (Tree‑sitter only).";
        }

        public override void InitializePlugin(IConfiguration configuration)
        {
            logger.Info("Initializing the Annotated Unit Tests Plugin (Tree‑sitter only)");
            try
            {
                // Base class parses ALL fields including AnnotationName
                base.InitializePlugin(configuration);

                // Validate that all configurations have required annotation-specific fields
                foreach (var testConfig in TestConfigurations)
                {
                    var annotationName = testConfig.GetValue<string>("AnnotationName");
                    if (string.IsNullOrEmpty(annotationName))
                    {
                        throw new Exception($"AnnotationName is required for test configuration in project '{testConfig.Project}'");
                    }
                }

                // Read other plugin-specific configuration (outside TestConfigurations)
                var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
                PopulateUTInfo("Purpose", config);
                PopulateUTInfo("PostCondition", config);
                PopulateUTInfo("Identifier", config);
                PopulateUTInfo("TraceID", config);
            }
            catch (Exception e)
            {
                logger.Error("Error reading configuration for Annotated Unit Test plugin.");
                logger.Error(e);
                throw new Exception("The Annotated Unit Test plugin could not read its configuration. Aborting...");
            }

            ScanDirectoriesForSourceFiles();
        }

        public override void RefreshItems()
        {
            // Use the optimized approach: iterate through configurations and their associated files
            foreach (var testConfig in TestConfigurations)
            {
                if (testConfig.SourceFiles.Count == 0)
                {
                    logger.Warn($"No source files found for configuration '{testConfig.Project}' (Language: {testConfig.Language})");
                    continue;
                }
                
                logger.Debug($"Processing {testConfig.SourceFiles.Count} files for configuration '{testConfig.Project}' (Language: {testConfig.Language})");
                
                // Load language resources once per configuration
                var selectedLang = ParseLanguage(testConfig.Language);
                var tsLanguageId = GetTreeSitterLanguageId(selectedLang);
                var acceptedAnnotationName = testConfig.GetValue<string>("AnnotationName");
                
                using var language = new Language(tsLanguageId);
                using var parser = new Parser(language);
                
                var queryString = GetQueryForLanguage(selectedLang);
                using var query = new Query(language, queryString);
                
                // Process all files for this configuration with the same language resources
                foreach (var sourceFile in testConfig.SourceFiles)
                {
                    try
                    {
                        var text = fileProvider.ReadAllText(sourceFile);
                        FindAndProcessAnnotations(text, sourceFile, testConfig, selectedLang, acceptedAnnotationName, parser, query);
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Error processing file {sourceFile}: {e.Message}");
                        throw;
                    }
                }
            }
        }

        private void FindAndProcessAnnotations(string sourceText, string filename, TestConfiguration testConfig, Lang selectedLang, string acceptedAnnotationName, Parser parser, Query query)
        {
            using var tree = parser.Parse(sourceText);
            var exec = query.Execute(tree.RootNode);

            // Group by method
            var methodGroups = exec.Matches
                .GroupBy(m => m.Captures.FirstOrDefault(c => c.Name == "method_decl")?.Node)
                .Where(g => g.Key != null);

            foreach (var group in methodGroups)
            {
                string methodName = string.Empty;
                int methodLine = 0;

                // Collect attributes/decorators for this method, keyed by the captured @attr node
                var perAttr = new Dictionary<Node, AttrData>(new NodeRefComparer());

                foreach (var match in group)
                {
                    foreach (var cap in match.Captures)
                    {
                        switch (cap.Name)
                        {
                            case "method_name":
                                methodName = cap.Node.Text;
                                methodLine = (int)cap.Node.StartPosition.Row + 1;
                                break;
                            case "attr":
                                if (!perAttr.ContainsKey(cap.Node))
                                    perAttr[cap.Node] = new AttrData { Line = (int)cap.Node.StartPosition.Row + 1 };
                                break;
                        }
                    }

                    var annotationName = match.Captures.FirstOrDefault(c => c.Name == "attr_name")?.Node;
                    if (annotationName != null)
                    {
                        var aNode = NearestAncestor(annotationName, n => n.Type == AttrNodeType(selectedLang));
                        if (aNode == null)
                        {
                            aNode = annotationName;
                        }
                        if (!perAttr.TryGetValue(aNode, out var ad))
                            perAttr[aNode] = ad = new AttrData { Line = (int)aNode.StartPosition.Row + 1 };

                        ad.Name = NormalizeAnnotationName(annotationName.Text, selectedLang);
                    }
                    // If this match is an "argument match" (pattern 2) it will have arg_name/arg_value.
                    var nameCap = match.Captures.FirstOrDefault(c => c.Name == "arg_name");
                    var valueCap = match.Captures.FirstOrDefault(c => c.Name == "arg_value");
                    if (nameCap != null || valueCap != null)
                    {
                        // Use the CAPTURED @attr from THIS match as the key (avoid ancestor identity mismatches)
                        var attrCap = match.Captures.FirstOrDefault(c => c.Name == "attr")?.Node;
                        if (attrCap != null)
                        {
                            if (!perAttr.TryGetValue(attrCap, out var ad))
                                perAttr[attrCap] = ad = new AttrData { Line = (int)attrCap.StartPosition.Row + 1 };

                            // If both sides are present (named arg), add the pair. Otherwise, empty string if only one side.
                            if (nameCap != null)
                            {
                                var cleanKey = Unquote(nameCap.Node.Text);
                                var cleanValue = valueCap != null ? Unquote(valueCap.Node.Text) : string.Empty;
                                ad.Args.Add((cleanKey, cleanValue));
                            }
                        }
                    }

                    // Java path (your existing arg_pair fallback)
                    foreach (var cap in match.Captures.Where(c => c.Name == "arg_pair"))
                    {
                        var attrCap = match.Captures.FirstOrDefault(c => c.Name == "attr")?.Node;
                        if (attrCap == null) continue;

                        if (!perAttr.TryGetValue(attrCap, out var ad))
                            perAttr[attrCap] = ad = new AttrData { Line = (int)attrCap.StartPosition.Row + 1 };

                        var text = cap.Node.Text;
                        var eq = text.IndexOf('=');
                        if (eq > 0)
                        {
                            var key = Unquote(text.Substring(0, eq).Trim());
                            var val = Unquote(text.Substring(eq + 1).Trim());
                            ad.Args.Add((key, val));
                        }
                    }


                    // Also gather explicit arg_name/arg_value pairs from this match (kept separate for clarity)
                    var names = match.Captures.Where(c => c.Name == "arg_name").ToList();
                    foreach (var an in names)
                    {
                        var aNode = NearestAncestor(an.Node, n => n.Type == AttrNodeType(selectedLang));
                        if (aNode == null || !perAttr.TryGetValue(aNode, out var ad)) continue;

                        // Find nearest arg_value that shares the same immediate parent frame (language-specific but pragmatic)
                        var val = match.Captures
                            .Where(c => c.Name == "arg_value" && SharesAttributeFrame(an.Node, c.Node, selectedLang))
                            .Select(c => Unquote(c.Node.Text))
                            .FirstOrDefault();
                        if (val != null) ad.Args.Add((Unquote(an.Node.Text), val));
                    }
                }

                // Process attributes for this method
                foreach (var kv in perAttr)
                {
                    var ad = kv.Value;
                    if (string.IsNullOrEmpty(ad.Name)) continue;
                    if (!acceptedAnnotationName.Equals(ad.Name, StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        // Build parameter map from captured named args
                        var paramMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var (k, v) in ad.Args)
                        {
                            if (string.IsNullOrWhiteSpace(k)) continue;
                            paramMap[k] = v; // Values are already cleaned by Unquote above
                        }

                        // Translate to RoboClerk fields using TOML mapping
                        var translated = new Dictionary<string, string>();
                        foreach (var map in information)
                        {
                            // map.Value.KeyWord is the keyword expected in source code arguments
                            if (paramMap.TryGetValue(map.Value.KeyWord, out var valText))
                                translated[map.Key] = valText; // Don't apply Unquote again
                        }

                        // Validate that all required fields are present
                        ValidateRequiredFields(translated, filename, ad.Line, methodName, acceptedAnnotationName);

                        AddUnitTest(filename, ad.Line, translated, methodName, testConfig.Project);
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Error processing annotation on line {ad.Line} in file {filename}: {e.Message}");
                        throw;
                    }
                }
            }
        }

        #region Query + language helpers
        private static Lang ParseLanguage(string s) => s.Trim().ToLowerInvariant() switch
        {
            "c#" or "csharp" or "cs" => Lang.CSharp,
            "java" => Lang.Java,
            "python" or "py" => Lang.Python,
            "ts" or "typescript" => Lang.TypeScript,
            "js" or "javascript" => Lang.JavaScript,
            _ => Lang.CSharp
        };

        private static string GetTreeSitterLanguageId(Lang lang) => lang switch
        {
            Lang.CSharp => "C_SHARP",
            Lang.Java => "JAVA",
            Lang.Python => "PYTHON",
            Lang.TypeScript => "TYPESCRIPT",
            Lang.JavaScript => "TYPESCRIPT", // use TS grammar to parse JS with decorators
            _ => "C_SHARP"
        };

        private static string AttrNodeType(Lang lang) => lang switch
        {
            Lang.CSharp => "attribute",
            Lang.Java => "annotation", // marker_annotation also treated as annotation in ancestor search
            Lang.Python => "decorator",
            Lang.TypeScript => "decorator",
            Lang.JavaScript => "decorator",
            _ => "attribute"
        };

        private string GetQueryForLanguage(Lang lang)
        {
            return lang switch
            {
                // C# attributes with optional named args (name_equals) and any expression value
                Lang.CSharp => @"
; pattern 1: method + attribute (no per-arg captures)
(
  (method_declaration
    (attribute_list
      (attribute name: (_) @attr_name) @attr
    )+
    name: (identifier) @method_name
  ) @method_decl
)

; pattern 2: per-argument captures under the same ancestor shape
(
  (method_declaration
    (attribute_list
      (attribute
        name: (_)
        (attribute_argument_list
          (attribute_argument
            (assignment_expression
              left: (identifier) @arg_name
              right: (_) @arg_value
            )
          )
        )
      ) @attr
    )+
  ) @method_decl
)
",

                // Java annotations: normal annotations (with pairs) and marker annotations (no args)
                Lang.Java => @"
(
  (method_declaration
    (modifiers
      (annotation
        name: (identifier) @attr_name
        (annotation_argument_list
          (element_value_pair) @arg_pair
        )?
      ) @attr
    )+
    name: (identifier) @method_name
  ) @method_decl
)
(
  (method_declaration
    (modifiers
      (marker_annotation
        name: (identifier) @attr_name
      ) @attr
    )+
    name: (identifier) @method_name
  ) @method_decl
)
",

                // Python decorators: with args (keyword_argument) and bare
                Lang.Python => @"
(
  (decorated_definition
    (decorator
      (identifier) @attr_name              ; e.g., @test_decorator
    ) @attr
    (function_definition name: (identifier) @method_name) @method_decl
  )
)
; dotted bare form: @pkg.decor
(
  (decorated_definition
    (decorator
      (attribute) @attr_name               ; dotted name node
    ) @attr
    (function_definition name: (identifier) @method_name) @method_decl
  )
)
(
  (decorated_definition
    (decorator
      (call
        function: (_) @attr_name
        arguments: (argument_list)         ; may be empty: @name()
      )
    ) @attr
    (function_definition name: (identifier) @method_name) @method_decl
  )
)
(
  (decorated_definition
    (decorator
      (call
        function: (_)
        arguments: (argument_list
          (keyword_argument
            name: (identifier) @arg_name
            value: (_) @arg_value
          )
        )
      )
    ) @attr
    (function_definition name: (identifier)) @method_decl
  )
)
",

                // TypeScript/JavaScript decorators on class methods. Separated patterns to avoid duplicate methods.
                Lang.TypeScript or Lang.JavaScript => @"
; ---------- D0: nearest call-form decorator ----------
(
  (class_body
    (decorator
      (call_expression
        function: (_) @attr_name
        arguments: (arguments
          (object
            (pair
              key:   (property_identifier) @arg_name
              value: (_)                  @arg_value
            )
          )
        )
      )
    ) @attr .
    (method_definition name: (_) @method_name) @method_decl
  )
)

; ---------- D1: one above the nearest ----------
(
  (class_body
    (decorator
      (call_expression
        function: (_) @attr_name
        arguments: (arguments
          (object
            (pair
              key:   (property_identifier) @arg_name
              value: (_)                  @arg_value
            )
          )
        )
      )
    ) @attr .
    (decorator (call_expression)) .                                   
    (method_definition name: (_) @method_name) @method_decl
  )
)

; ---------- D2: two above the nearest ----------
(
  (class_body
    (decorator
      (call_expression
        function: (_) @attr_name
        arguments: (arguments
          (object
            (pair
              key:   (property_identifier) @arg_name
              value: (_)                  @arg_value
            )
          )
        )
      )
    ) @attr .
    (decorator (call_expression)) .
    (decorator (call_expression)) .                                  
    (method_definition name: (_) @method_name) @method_decl
  )
)
; ---------- D0: nearest call-form decorator — NO ARGS ----------
(
  (class_body
    (decorator
      (call_expression
        function: (_) @attr_name
        arguments: (arguments) @args_empty
      )
    ) @attr .
    (method_definition name: (_) @method_name) @method_decl
  )
)

; ---------- D1: one above the nearest — NO ARGS ----------
(
  (class_body
    (decorator
      (call_expression
        function: (_) @attr_name
        arguments: (arguments) @args_empty
      )
    ) @attr .
    (decorator (call_expression)) .
    (method_definition name: (_) @method_name) @method_decl
  )
)

; ---------- D2: two above the nearest — NO ARGS ----------
(
  (class_body
    (decorator
      (call_expression
        function: (_) @attr_name
        arguments: (arguments) @args_empty
      )
    ) @attr .
    (decorator (call_expression)) .
    (decorator (call_expression)) .
    (method_definition name: (_) @method_name) @method_decl
  )
)
",

                _ => throw new NotSupportedException($"Unsupported language {lang}")
            };
        }

        private static string NormalizeAnnotationName(string raw, Lang lang)
        {
            var s = raw.Trim();
            if (lang is Lang.Java or Lang.TypeScript or Lang.JavaScript or Lang.Python)
            {
                if (s.StartsWith("@")) s = s[1..];
            }
            int lastDot = s.LastIndexOf('.');
            if (lastDot >= 0 && lastDot < s.Length - 1) s = s[(lastDot + 1)..];
            return s;
        }

        private static Node? NearestAncestor(Node node, Func<Node, bool> pred)
        {
            var cur = node;
            while (cur.Parent != null)
            {
                cur = cur.Parent;
                if (pred(cur)) return cur;
            }
            return null;
        }

        private static bool SharesAttributeFrame(Node a, Node b, Lang lang)
        {
            var A = NearestAncestor(a, n => n.Type == AttrNodeType(lang));
            var B = NearestAncestor(b, n => n.Type == AttrNodeType(lang));
            return A != null && B != null && ReferenceEquals(A, B);
        }
        #endregion

        private void ValidateRequiredFields(Dictionary<string, string> translated, string filename, int lineNumber, string methodName, string acceptedAnnotationName)
        {
            var missingFields = new List<string>();
            
            foreach (var info in information)
            {
                // Check if this field is required (not optional)
                if (!info.Value.Optional)
                {
                    // Check if the field is missing or empty
                    if (!translated.TryGetValue(info.Key, out var value) || string.IsNullOrWhiteSpace(value))
                    {
                        missingFields.Add(info.Value.KeyWord);
                    }
                }
            }

            if (missingFields.Count > 0)
            {
                var fieldList = string.Join(", ", missingFields);
                throw new Exception($"Required field(s) missing from {acceptedAnnotationName} attribute for method '{methodName}' in {Path.GetFileName(filename)} at line {lineNumber}: {fieldList}");
            }
        }

        private void AddUnitTest(string fileName, int lineNumber, Dictionary<string, string> parameterValues, string methodName, string projectName = "")
        {
            var unitTest = new UnitTestItem
            {
                UnitTestFunctionName = methodName,
                UnitTestFileName = Path.GetFileName(fileName)
            };
            bool identified = false;

            foreach (var info in information)
            {
                if (!parameterValues.TryGetValue(info.Key, out var value)) continue;

                // Value is already cleaned by Unquote in the parsing pipeline
                switch (info.Key)
                {
                    case "Purpose": unitTest.UnitTestPurpose = value; break;
                    case "PostCondition": unitTest.UnitTestAcceptanceCriteria = value; break;
                    case "Identifier": unitTest.ItemID = value; identified = true; break;
                    case "TraceID": unitTest.AddLinkedItem(new ItemLink(value, ItemLinkType.UnitTests)); break;
                    default: throw new Exception($"Unknown annotation identifier: {info.Key}");
                }
            }

            if (!identified)
            {
                var projectPrefix = !string.IsNullOrEmpty(projectName) ? $"{projectName}:" : "";
                unitTest.ItemID = $"{projectPrefix}{unitTest.UnitTestFileName}:{lineNumber}";
            }

            if (gitRepo != null && !gitRepo.GetFileLocallyUpdated(fileName))
            {
                unitTest.ItemLastUpdated = gitRepo.GetFileLastUpdated(fileName);
                unitTest.ItemRevision = gitRepo.GetFileVersion(fileName);
            }
            else
            {
                unitTest.ItemLastUpdated = File.GetLastWriteTime(fileName);
                unitTest.ItemRevision = File.GetLastWriteTime(fileName).ToString("yyyy/MM/dd HH:mm:ss");
            }

            if (unitTests.Exists(x => x.ItemID == unitTest.ItemID))
                throw new Exception($"Duplicate unit test identifier detected in {unitTest.UnitTestFileName} in the annotation starting on line {lineNumber}. Ensure all unit tests have a unique identifier.");

            unitTests.Add(unitTest);
            
            if (!string.IsNullOrEmpty(projectName))
            {
                logger.Debug($"Added unit test '{unitTest.ItemID}' from project '{projectName}' (Language: {GetConfigurationForFile(fileName)?.Language ?? "unknown"})");
            }
        }

        private static string Unquote(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            s = s.Trim();

            // First, handle string literal prefixes for quoted strings
            // This needs to happen before quote checking for cases like @"..." or $"..."
            var originalString = s;
            s = RemoveStringLiteralPrefixes(s);
            
            // Python triple-quoted strings: """...""" or '''...'''
            if (s.Length >= 6 &&
                ((s.StartsWith("\"\"\"") && s.EndsWith("\"\"\"")) ||
                 (s.StartsWith("'''") && s.EndsWith("'''"))))
            {
                var inner = s.Substring(3, s.Length - 6);
                return UnescapeCommon(inner);
            }

            // Regular single-, double-quoted, or template literal strings
            if (s.Length >= 2 && 
                ((s[0] == '"' && s[^1] == '"') || 
                 (s[0] == '\'' && s[^1] == '\'') || 
                 (s[0] == '`' && s[^1] == '`')))
            {
                var inner = s.Substring(1, s.Length - 2);
                return UnescapeCommon(inner);
            }

            // If no quotes were found after prefix removal, but the original had prefixes,
            // then this was likely not a string literal - return the original
            if (!ReferenceEquals(originalString, s))
            {
                return originalString;
            }

            // If it's not a quoted string, return as-is
            // This handles parameter names like "test_id" which should not be modified
            return s;
        }

        private static string RemoveStringLiteralPrefixes(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            // Handle various string literal prefixes across languages:
            // C#: @"...", $"...", @$"...", $@"...
            // Python: r"...", u"...", f"...", b"...", fr"...", rf"..., etc.
            // JavaScript/TypeScript: `...` (template literals don't have prefixes, but we handle them)
            
            int prefixEnd = 0;
            int length = s.Length;
            bool foundQuote = false;

            // First, scan to see if there's actually a quote character in the string
            for (int i = 0; i < length; i++)
            {
                if (s[i] == '"' || s[i] == '\'' || s[i] == '`')
                {
                    foundQuote = true;
                    break;
                }
            }

            // If no quote found, this is not a string literal, return as-is
            if (!foundQuote)
            {
                return s;
            }

            // Find the end of any prefix characters before the quote
            while (prefixEnd < length)
            {
                char c = s[prefixEnd];
                if (c == '"' || c == '\'' || c == '`')
                {
                    // Found the start of the actual string
                    break;
                }
                else if (char.IsLetter(c) || c == '@' || c == '$')
                {
                    // This is likely a string prefix character
                    prefixEnd++;
                }
                else
                {
                    // Not a recognized prefix character, stop looking
                    break;
                }
            }

            // If we found any prefix characters, remove them
            if (prefixEnd > 0 && prefixEnd < length)
            {
                return s.Substring(prefixEnd);
            }

            return s;
        }

        private static string UnescapeCommon(string inner)
        {
            if (string.IsNullOrEmpty(inner)) return inner;
            
            var result = new StringBuilder(inner.Length);
            
            for (int i = 0; i < inner.Length; i++)
            {
                if (inner[i] == '\\' && i + 1 < inner.Length)
                {
                    char next = inner[i + 1];
                    switch (next)
                    {
                        case '\\':
                            result.Append('\\');
                            i++; // Skip the next character
                            break;
                        case '"':
                            result.Append('"');
                            i++; // Skip the next character
                            break;
                        case '\'':
                            result.Append('\'');
                            i++; // Skip the next character
                            break;
                        case 'n':
                            result.Append('\n');
                            i++; // Skip the next character
                            break;
                        case 'r':
                            result.Append('\r');
                            i++; // Skip the next character
                            break;
                        case 't':
                            result.Append('\t');
                            i++; // Skip the next character
                            break;
                        default:
                            // For unknown escape sequences, keep the backslash
                            result.Append('\\');
                            break;
                    }
                }
                else
                {
                    result.Append(inner[i]);
                }
            }
            
            return result.ToString();
        }

        // Helper types
        private sealed class AttrData
        {
            public string Name = string.Empty;
            public int Line;
            public List<(string Key, string Value)> Args = new();
        }

        private sealed class NodeRefComparer : IEqualityComparer<Node>
        {
            //public bool Equals(Node? x, Node? y) => ReferenceEquals(x, y);
            public bool Equals(Node? x, Node? y) => x.Id == y.Id;
            //public int GetHashCode(Node obj) => obj.GetHashCode();
            public int GetHashCode(Node obj) => obj.Id.GetHashCode();
        }

        #region Config helpers
        private void PopulateUTInfo(string tableName, TomlTable root)
        {
            if (!root.ContainsKey(tableName))
                throw new Exception($"A required table \"{tableName}\" is missing from the {name}.toml configuration file. Cannot continue.");

            var table = (TomlTable)root[tableName];
            var info = new UTInformation();
            try { info.FromToml(table); }
            catch (Exception e) { throw new Exception($"{e.Message}\"{tableName}\""); }
            information[tableName] = info;
        }
        #endregion
    }
}
