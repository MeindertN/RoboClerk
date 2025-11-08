using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;
using System.Text;

namespace RoboClerk
{
    public class ItemTemplateRenderer : IDisposable
    {
        private ItemTemplateParser parser = null!;
        private string fileContent = string.Empty;
        private ScriptOptions? scriptOptions;
        private bool disposed = false;

        public ItemTemplateRenderer(string templateContent) 
        {
            fileContent = templateContent;
            parser = new ItemTemplateParser(templateContent);
            scriptOptions = ScriptOptions.Default.WithReferences(Assembly.GetExecutingAssembly());
        }

        public string RenderItemTemplate<T>(ScriptingBridge<T> bridge) where T : Item
        {
            StringBuilder sb = new StringBuilder(fileContent);
            if (parser.StartSegment.Item2 < 0 || parser.StartSegment.Item3 < 0)
            {
                return sb.ToString();
            }
            ScriptState<object> beginState = CSharpScript.RunAsync(parser.StartSegment.Item1, scriptOptions, globals: bridge).Result;
            foreach (var segment in parser.Segments)
            {
                var state = beginState.ContinueWithAsync<string>(segment.Item1).Result;
                string result = state.ReturnValue;
                sb.Remove(segment.Item2, segment.Item3 - segment.Item2);
                sb.Insert(segment.Item2, result);
            }
            //remove start segment
            sb.Remove(parser.StartSegment.Item2, parser.StartSegment.Item3 - parser.StartSegment.Item2);
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
                scriptOptions = null;
                parser = null!;
                fileContent = string.Empty;
                disposed = true;
            }
        }
    }
}
