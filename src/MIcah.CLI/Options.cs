using System;
using System.Collections.Generic;
using System.Text;

using CommandLine;
using CommandLine.Text;

namespace Micah.CLI
{
    public class Options
    {
        [Option('d', "debug", Required = false, HelpText = "Enable debug mode.")]
        public bool Debug { get; set; }
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
}
