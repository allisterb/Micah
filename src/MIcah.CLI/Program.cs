using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using CO = Colorful.Console;
using Figgle;
using CommandLine;
using CommandLine.Text;

using Micah.NLU.GoogleHC;
using Micah.NLU.ExpertAI;
using Micah.FHIR;

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
                Debug("Debug mode enabled.");
            }
            else
            {
                SetLogger(new SerilogLogger(console: true, debug: false));
            }
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Console.CancelKeyPress += Console_CancelKeyPress;
            PrintLogo();
            ParserResult<object> result = new Parser().ParseArguments<WitOptions, ASROptions, GoogleHCNLUOptions, ExpertAINLUOptions, FHIROptions>(args);
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
                        help.AddVerbs(typeof(WitOptions), typeof(ASROptions), typeof(GoogleHCNLUOptions), typeof(ExpertAINLUOptions), typeof(FHIROptions));
                    }
                    Info(help);
                    Exit(ExitResult.SUCCESS);
                }
                else if (errors.Any(e => e.Tag == ErrorType.HelpRequestedError))
                {
                    HelpRequestedError error = (HelpRequestedError)errors.First(e => e.Tag == ErrorType.HelpRequestedError);
                    help.AddVerbs(typeof(WitOptions), typeof(ASROptions), typeof(GoogleHCNLUOptions), typeof(ExpertAINLUOptions), typeof(FHIROptions));
                    Info(help);
                    Exit(ExitResult.SUCCESS);
                }
                else if (errors.Any(e => e.Tag == ErrorType.NoVerbSelectedError))
                {
                    help.AddVerbs(typeof(WitOptions), typeof(ASROptions), typeof(GoogleHCNLUOptions), typeof(ExpertAINLUOptions), typeof(FHIROptions));
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
                    help.AddVerbs(typeof(WitOptions), typeof(ASROptions), typeof(GoogleHCNLUOptions), typeof(ExpertAINLUOptions), typeof(FHIROptions));
                    Error("Unknown option: {error}.", error.Token);
                    Info(help);
                    Exit(ExitResult.INVALID_OPTIONS);
                }
                else
                {
                    Error("An error occurred parsing the program options: {errors}.", errors);
                    help.AddVerbs(typeof(WitOptions), typeof(ASROptions), typeof(GoogleHCNLUOptions), typeof(ExpertAINLUOptions), typeof(FHIROptions));
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
            })
            .WithParsed<FHIROptions>(o =>
            {
                FHIR(o);
            });
        }

        #region Methods

        public static void FHIR(FHIROptions o)
        {
            FHIR3Client client;
            if (o.Google)
            {
                client = new GoogleHCFHIR3Client();
            }
            else
            {
                client = new FHIR3Client(o.Endpoint);
            }
            Info("Using FHIR endpoint {0}.", o.Endpoint);
            if (o.CreatePatient == 1)
            {
                
                var pid = Guid.NewGuid().ToString();
                client.CreateDemoPatient1(pid).Wait();
                Info("Created demo patient 1 with id {0}.", pid);
                Exit(ExitResult.SUCCESS);
            }
            else if(!string.IsNullOrEmpty(o.SearchPatients))
            {
                string[] search_params;
                var search = Options.Parse(o.SearchPatients);
                if (search.Count == 0)
                {
                    Error("There was an error parsing the search parameters {0}.", o.SearchPatients);
                    Exit(ExitResult.INVALID_OPTIONS);
                }
                else if (search.Where(o => o.Key == "_ERROR_").Count() > 0)
                {

                    string error_options = search.Where(o => o.Key == "_ERROR_").Select(kv => (string)kv.Value).Aggregate((s1, s2) => s1 + Environment.NewLine + s2);
                    Error("There was an error parsing the following options {0}.", error_options);
                    
                    Exit(ExitResult.INVALID_OPTIONS);
                }
                else
                {
                    search_params = search.Where(o => o.Key != "_ERROR_").Select(kvp => kvp.Key + ":exact=" + kvp.Value).ToArray();
                    var b = client.SearchPatients(search_params).Result;
                    if (o.Json)
                    {
                        WriteInfo(JsonConvert.SerializeObject(b.Entry.Select(e => e.Resource)));
                    }
                    else
                    {
                        foreach (var ec in b.Entry)
                        {
                            var p = (Hl7.Fhir.Model.Patient) ec.Resource;
                            WriteInfo("Id:{0}. Name: {1}.", p.Id, p.Name[0]);
                        }
                    }
                    Exit(ExitResult.SUCCESS);
                }
            }
        }
        public static void Wit(WitOptions o) 
        {
            HttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + Config("WIT2"));
            var r = HttpClient.GetAsync("https://api.wit.ai/message?q=" + o.Text).Result;
            r.EnsureSuccessStatusCode();
            var text = r.Content.ReadAsStringAsync().Result; 
            if (o.Query)
            {
                if (o.Debug)
                {
                    WriteInfo(text);
                }
                dynamic j = JObject.Parse(r.Content.ReadAsStringAsync().Result);
                var intents = j.intents;
                if (intents[0].name == "query")
                {
                    double ic = intents[0].confidence;
                    Info("Query intent confidence: {0}.", ic);
                    dynamic entities = j.entities;
                    foreach(JProperty en in entities)
                    {
                        JArray a = (JArray) en.Value;
                        dynamic e = a[0];
                        if (e.name == "wit$local_search_query")
                        {
                            if (e.role == "query_resource")
                            {
                                string v = e.value;
                                double c = e.confidence;
                                Info("Query resource: {0}. Confidence: {1}.", v, c);
                            }
                        } 

                        if (e.name == "wit$contact")
                        {
                            if (e.role == "query_param_name")
                            {
                                string v = e.value;
                                double c = e.confidence;
                                Info("Query param: {0}. Value: {1}. Confidence: {2}.", "name", v, c);
                            }

                            else if (e.role == "query_param_family")
                            {
                                string v = e.value;
                                double c = e.confidence;
                                Info("Query param: {0}. Value: {1}. Confidence: {2}.", "family_name", v, c);
                            }

                            else
                            {
                                string v = e.value;
                                double c = e.confidence;
                                Info("Query param: {0}. Value: {1}. Confidence: {2}.", "name(inferred)", v, c);
                            }
                        }

                        if (e.name == "wit$number")
                        { 
                        }

                    }
                }
                
            }
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
