using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using CommandLine;
using CommandLine.Text;

namespace Micah.CLI
{
    public class Options
    {
        [Option('d', "debug", Required = false, HelpText = "Enable debug mode.")]
        public bool Debug { get; set; }

        public static Dictionary<string, object> Parse(string o)
        {
            Dictionary<string, object> options = new Dictionary<string, object>();
            Regex re = new Regex(@"(\w+)\=([^\,]+)", RegexOptions.Compiled);
            string[] pairs = o.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in pairs)
            {
                Match m = re.Match(s);
                if (!m.Success)
                {
                    options.Add("_ERROR_", s);
                }
                else if (options.ContainsKey(m.Groups[1].Value))
                {
                    options[m.Groups[1].Value] = m.Groups[2].Value;
                }
                else
                {
                    options.Add(m.Groups[1].Value, m.Groups[2].Value);
                }
            }
            return options;
        }
    }

    [Verb("wit", HelpText = "Use Wit.ai NLU on a sentence.")]
    class WitOptions : Options
    {
        [Option('t', "text", Required = true, HelpText = "The text to understand.")]
        public string Text { get; set; }
    }

    [Verb("asr", HelpText = "Test the speech recognition feature of Micah with the default mic as the input source.")]
    class ASROptions : Options
    {
        [Option('t', "text", Required = true, HelpText = "The text to understand.")]
        public string Text { get; set; }
    }

    [Verb("ghc", HelpText = "Use Google HealthCare NLU on text.")]
    class GoogleHCNLUOptions : Options
    {
        [Option('t', "text", Required = true, HelpText = "The text to understand.")]
        public string Text { get; set; }

        [Option('j', "json", Required = false, HelpText = "Print raw JSON output.", Default = false)]
        public bool Json { get; set; }
    }

    [Verb("eai", HelpText = "Use expert.ai NLU on text.")]
    class ExpertAINLUOptions : Options
    {
        [Option('t', "text", Required = true, HelpText = "The text to understand.")]
        public string Text { get; set; }

        [Option('j', "json", Required = false, HelpText = "Print raw JSON output.", Default = false)]
        public bool Json { get; set; }
    }

    [Verb("fhir", HelpText = "Query and create FHIR resources.")]
    class FHIROptions : Options
    {
        [Option('e', "endpoint", Required = false, HelpText = "FHIR endpoint URL.", Default = "https://stu3.test.pyrohealth.net/fhir")]
        public string Endpoint { get; set; }

        [Option('g', "google", Required = false, HelpText = "Use Google FHIR server.", Default = false)]
        public bool Google { get; set; }

        [Option('c', "create-patient", Required = false, HelpText = "Create a demo patient.", Default = -1)]
        public int CreatePatient { get; set; }

        [Option('p', "search-patients", Required = false, HelpText = "Search for patients with the specified params.")]
        public string SearchPatients { get; set; }

        [Option('j', "json", Required = false, HelpText = "Print raw JSON output.", Default = false)]
        public bool Json { get; set; }
    }
}
