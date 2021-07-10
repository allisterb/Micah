using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using CO = Colorful.Console;
using Figgle;
using CommandLine;
using CommandLine.Text;

using Micah.NLU.GoogleHC;
using Micah.NLU.ExpertAI;

namespace Micah.CLI
{
    class Program : Runtime
    {
        static void Main(string[] args)
        {
            Args = args;
            if (Args.Contains("--debug"))
            {
                SetLogger(new SerilogLogger(console: true, debug: true));
            }
            else
            {
                SetLogger(new SerilogLogger(console: true, debug: false));
            }
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Console.CancelKeyPress += Console_CancelKeyPress;
            PrintLogo();
            ParserResult<object> result = new Parser().ParseArguments<WitOptions, ASROptions, GoogleHCNLUOptions, ExpertAINLUOptions>(args);
            result.WithNotParsed((IEnumerable<Error> errors) =>
            {
                HelpText help = GetAutoBuiltHelpText(result);
                help.Copyright = string.Empty;
                help.AddPreOptionsLine(string.Empty);

                if (errors.Any(e => e.Tag == ErrorType.VersionRequestedError))
                {
                    Exit(ExitResult.SUCCESS);
                }
                else if (errors.Any(e => e.Tag == ErrorType.HelpVerbRequestedError))
                {
                    HelpVerbRequestedError error = (HelpVerbRequestedError)errors.First(e => e.Tag == ErrorType.HelpVerbRequestedError);
                    if (error.Type != null)
                    {
                        help.AddVerbs(error.Type);
                    }
                    else
                    {
                        help.AddVerbs(typeof(WitOptions), typeof(ASROptions), typeof(GoogleHCNLUOptions), typeof(ExpertAINLUOptions));
                    }
                    Info(help);
                    Exit(ExitResult.SUCCESS);
                }
                else if (errors.Any(e => e.Tag == ErrorType.HelpRequestedError))
                {
                    HelpRequestedError error = (HelpRequestedError)errors.First(e => e.Tag == ErrorType.HelpRequestedError);
                    help.AddVerbs(typeof(WitOptions), typeof(ASROptions), typeof(GoogleHCNLUOptions), typeof(ExpertAINLUOptions));
                    Info(help);
                    Exit(ExitResult.SUCCESS);
                }
                else if (errors.Any(e => e.Tag == ErrorType.NoVerbSelectedError))
                {
                    help.AddVerbs(typeof(WitOptions), typeof(ASROptions), typeof(GoogleHCNLUOptions), typeof(ExpertAINLUOptions));
                    Info(help);
                    Exit(ExitResult.INVALID_OPTIONS);
                }
                else if (errors.Any(e => e.Tag == ErrorType.MissingRequiredOptionError))
                {
                    MissingRequiredOptionError error = (MissingRequiredOptionError)errors.First(e => e.Tag == ErrorType.MissingRequiredOptionError);
                    Error("A required option is missing: {0}.", error.NameInfo.NameText);
                    Info(help);
                    Exit(ExitResult.INVALID_OPTIONS);
                }
                else if (errors.Any(e => e.Tag == ErrorType.UnknownOptionError))
                {
                    UnknownOptionError error = (UnknownOptionError)errors.First(e => e.Tag == ErrorType.UnknownOptionError);
                    help.AddVerbs(typeof(WitOptions), typeof(ASROptions), typeof(GoogleHCNLUOptions), typeof(ExpertAINLUOptions));
                    Error("Unknown option: {error}.", error.Token);
                    Info(help);
                    Exit(ExitResult.INVALID_OPTIONS);
                }
                else
                {
                    Error("An error occurred parsing the program options: {errors}.", errors);
                    help.AddVerbs(typeof(WitOptions), typeof(ASROptions), typeof(GoogleHCNLUOptions), typeof(ExpertAINLUOptions));
                    Info(help);
                    Exit(ExitResult.INVALID_OPTIONS);
                }
            })
            .WithParsed<WitOptions>(o =>
            {
                Wit(o);
            })
            .WithParsed<ASROptions>(o =>
            {
                ASR(o);
            })
            .WithParsed<GoogleHCNLUOptions>(o =>
            {
                GHC(o);
            })
            .WithParsed<ExpertAINLUOptions>(o =>
            {
                EAI(o);
            });
        }

        #region Methods

        public static void Wit(WitOptions o) 
        {
            HttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + Config("WIT2"));
            var r = HttpClient.GetAsync("https://api.wit.ai/message?q=" + o.Text).Result;
            r.EnsureSuccessStatusCode();
            WriteInfo(r.Content.ReadAsStringAsync().Result);
            Exit(ExitResult.SUCCESS);
        }

        public static void GHC(GoogleHCNLUOptions o)
        {
            if (o.Json)
            {
                var s = GoogleHCApi.AnalyzeEntities(o.Text).Result;
                WriteInfo(s);
            }
            else
            {
                var s = GoogleHCApi.AnalyzeEntities2(o.Text).Result;
                WriteInfo("Entities:\n{0}", s.EntityMentions.Select(e => e.ToString()).Aggregate((s1, s2) => s1 + "\n\n" + s2));
            }
            Exit(ExitResult.SUCCESS);
        }

        public static void EAI(ExpertAINLUOptions o)
        {
            var eai = new ExpertAIApi(Config("EXPERTAI_TOKEN2"));
            WriteInfo("{0}", eai.AnalyzeRelations(o.Text).Result.Select(s => JsonConvert.SerializeObject(s)).Aggregate((s1, s2) => s1 + "\n\n" + s2));
            Exit(ExitResult.SUCCESS);
        }

        public static void ASR(ASROptions o) { }
        static void PrintLogo()
        {
            CO.WriteLine(FiggleFonts.Chunky.Render("Micah"), Color.Red);
            CO.WriteLine("v{0}", AssemblyVersion.ToString(3), Color.Red);
        }

        public static void Exit(ExitResult result)
        {

            if (Cts != null && !Cts.Token.CanBeCanceled)
            {
                Cts.Cancel();
                Cts.Dispose();
            }

            Environment.Exit((int)result);
        }

        static HelpText GetAutoBuiltHelpText(ParserResult<object> result)
        {
            return HelpText.AutoBuild(result, h =>
            {
                h.AddOptions(result);
                return h;
            },
            e =>
            {
                return e;
            });
        }

        static void WriteInfo(string template, params object[] args) => CO.WriteLineFormatted(template, Color.AliceBlue, Color.PaleGoldenrod, args);
        #endregion

        #region Properties
        static string[] Args { get; set; }
        #endregion

        #region Event Handlers
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Error((Exception)e.ExceptionObject, "Unhandled error occurred during operation. Micah CLI will now shutdown.");
            Exit(ExitResult.UNHANDLED_EXCEPTION);
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Info("Ctrl-C pressed. Exiting.");
            Cts.Cancel();
            Exit(ExitResult.SUCCESS);
        }
        #endregion

        #region Enums
        public enum ExitResult
        {
            SUCCESS = 0,
            UNHANDLED_EXCEPTION = 1,
            INVALID_OPTIONS = 2,
            UNKNOWN_ERROR = 3,
            NOT_FOUND_OR_SERVER_ERROR = 4
        }
        #endregion
    }
}
