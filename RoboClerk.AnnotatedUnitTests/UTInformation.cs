using Tomlyn.Model;

namespace RoboClerk.AnnotatedUnitTests
{
    internal class UTInformation
    {
        public string KeyWord { get; set; }

        public bool Optional { get; set; }

        public void FromToml(TomlTable input)
        {
            if(!input.ContainsKey("Keyword") || !input.ContainsKey("Optional"))
            {
                throw new System.Exception($"AnnotatedUnitTestPlugin: Configuration file does not contain \"KeyWord\" and/or \"Optional\" for item ");
            }
            KeyWord = (string)input["Keyword"];
            Optional = (bool)input["Optional"];
        }
    }
}
