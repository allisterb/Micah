using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace Micah
{
    public abstract class Runtime
    {
        #region Constructors
        static Runtime()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KUBERNETES_PORT")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENSHIFT_BUILD_NAMESPACE")))
            {
                Configuration = new ConfigurationBuilder()
                    .AddEnvironmentVariables()
                    .Build();
            }
            else if (Assembly.GetEntryAssembly().GetName().Name == "Micah.CLI" && Environment.GetEnvironmentVariable("USERNAME") == "Allister")
            {
                Configuration = new ConfigurationBuilder()
                    .AddUserSecrets("f3ed0dc7-f978-44ae-8add-9e5bfcf8fa8a")
                    .Build();
            }
            else if (Assembly.GetEntryAssembly().GetName().Name == "Micah.CLI" && Environment.GetEnvironmentVariable("USERNAME") != "Allister")
            {
                Configuration = new ConfigurationBuilder()
                .AddJsonFile("config.json", optional: true)
                .Build();
            }
            else if (Assembly.GetEntryAssembly().GetName().Name == "Micah.Web" && Environment.GetEnvironmentVariable("USERNAME") == "Allister")
            {
                Configuration = new ConfigurationBuilder()
                    .AddUserSecrets("f3ed0dc7-f978-44ae-8add-9e5bfcf8fa8a")
                    .Build();
            }
            else
            {
                Configuration = new ConfigurationBuilder()
                .AddJsonFile("config.json", optional: true)
                .Build();
            }

            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Micah/0.1");
        }
        public Runtime(CancellationToken ct)
        {
            if (Logger == null)
            {
                throw new InvalidOperationException("A logger is not assigned.");
            }
            CancellationToken = ct;
            Type = this.GetType();
        }
        public Runtime(): this(Cts.Token) {}

        #endregion

        #region Properties
        public static DirectoryInfo AssemblyDirectory { get; } = new FileInfo(Assembly.GetEntryAssembly().Location).Directory;

        public static Version AssemblyVersion { get; } = Assembly.GetEntryAssembly().GetName().Version;

        public static DirectoryInfo CurrentDirectory { get; } = new DirectoryInfo(Directory.GetCurrentDirectory());

        public static IConfigurationRoot Configuration { get; protected set; }

        public static Logger Logger { get; protected set; }

        public static CancellationTokenSource Cts { get; } = new CancellationTokenSource();

        public static CancellationToken Ct { get; } = Cts.Token;

        public static HttpClient HttpClient { get; } = new HttpClient();

        public static string YY = DateTime.Now.Year.ToString().Substring(2, 2);

        public bool Initialized { get; protected set; }

        public static bool IsAzureFunction { get; set; }

        public CancellationToken CancellationToken { get; protected set; }

        public Type Type { get; }

        #endregion

        #region Methods
        public static void SetLogger(Logger logger)
        {
            Logger = logger;
        }

        public static void SetLoggerIfNone(Logger logger)
        {
            if (Logger == null)
            {
                Logger = logger;
            }
        }

        public static void SetDefaultLoggerIfNone()
        {
            if (Logger == null)
            {
                Logger = new ConsoleLogger();
            }
        }

        public static string Config(string i) => Runtime.Configuration[i];
           
        public static void Info(string messageTemplate, params object[] args) => Logger.Info(messageTemplate, args);

        public static void Debug(string messageTemplate, params object[] args) => Logger.Debug(messageTemplate, args);

        public static void Error(string messageTemplate, params object[] args) => Logger.Error(messageTemplate, args);

        public static void Error(Exception ex, string messageTemplate, params object[] args) => Logger.Error(ex, messageTemplate, args);

        public static Logger.Op Begin(string messageTemplate, params object[] args) => Logger.Begin(messageTemplate, args);

        public static void SetPropsFromDict<T>(T instance, Dictionary<string, object> p)
        {
            foreach (PropertyInfo prop in typeof(T).GetProperties())
            {
                if (p.ContainsKey(prop.Name) && prop.PropertyType == p[prop.Name].GetType())
                {
                    prop.SetValue(instance, p[prop.Name]);
                }
            }
        }

        public string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }
        public void ThrowIfNotInitialized()
        {
            if (!this.Initialized) throw new ApiNotInitializedException(this);
        }
        #endregion
    }
}
