using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;
using System.Text;

namespace RoboClerk
{
    public class ItemTemplateRenderer
    {
        private ItemTemplateParser parser = null;
        private string fileContent = string.Empty;

        public ItemTemplateRenderer(string templateContent)
        {
            fileContent = templateContent;
            parser = new ItemTemplateParser(templateContent);
        }

        public string RenderItemTemplate(ScriptingBridge bridge)
        {
            StringBuilder sb = new StringBuilder(fileContent);
            if (parser.StartSegment.Item2 < 0 || parser.StartSegment.Item3 < 0)
            {
                return sb.ToString();
            }
            ScriptState<object> beginState = CSharpScript.RunAsync(parser.StartSegment.Item1, ScriptOptions.Default.WithReferences(Assembly.GetExecutingAssembly()), globals: bridge).Result;
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
    }
}
