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



}
