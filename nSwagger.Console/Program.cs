namespace nSwagger.Console
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    internal class Program
    {
        [Flags]
        private enum ExitCodes
        {
            Success = 0,
            NoSourceFiles = 1,
            NoTargetFile = 2,
            TargetFileExists = 4,
            Help = 8,
            Unknown = 16,
            InvalidLanguage = 32
        }

        private static int Main(string[] args)
        {            
            Console.Title = "nSwagger.Console";
            Console.CursorVisible = false;
            Console.WriteLine("nSwagger.Console");
            Console.WriteLine();
            if (args.Length < 2)
            {
                ShowHelp();
                return (int)ExitCodes.Help;
            }

            var consoleConfig = new ConsoleConfig
            {
                Target = args.Last()
            };

            for (var count = 0; count < args.Length - 1; count++)
            {
                var argument = args[count];
                if (argument.Equals("/F", StringComparison.OrdinalIgnoreCase))
                {
                    consoleConfig.AllowOverride = true;
                    continue;
                }

                if (argument.Equals("/B", StringComparison.OrdinalIgnoreCase))
                {
                    consoleConfig.BeepWhenDone = true;
                    continue;
                }

                if (argument.Equals("/NAMESPACE", StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                    consoleConfig.CustomNamespace = args[count];
                    continue;
                }

                if (argument.Equals("/O", StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                    consoleConfig.Language = args[count];
                    continue;
                }

                if (argument.Equals("/T", StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                    consoleConfig.HTTPTimeout = Convert.ToInt32(args[count]);
                    continue;
                }

                consoleConfig.Sources.Add(argument);
            }

            var validate = Validate(consoleConfig);
            if (validate != 0)
            {
                return validate;
            }

            try
            {
                var engine = Engine.Run(consoleConfig.Sources.ToArray());
                engine.Wait();

                var swaggerConfig = new Configuration
                {
                    HTTPTimeout = TimeSpan.FromSeconds(consoleConfig.HTTPTimeout)
                };

                if (!string.IsNullOrWhiteSpace(consoleConfig.CustomNamespace))
                {
                    swaggerConfig.Namespace = consoleConfig.CustomNamespace;
                }

                if (consoleConfig.Language.Equals("c#", StringComparison.OrdinalIgnoreCase))
                {
                    CSharpProcess(engine.Result, swaggerConfig, consoleConfig.Target);
                }

                if (consoleConfig.Language.Equals("typescript", StringComparison.OrdinalIgnoreCase))
                {
                    var generator = new TypeScriptGenerator();
                    generator.Run(engine.Result, swaggerConfig, consoleConfig.Target);
                }

                if (consoleConfig.BeepWhenDone)
                {
                    Console.Beep();
                }
            }
            catch (nSwaggerException ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
                return (int)ExitCodes.Unknown;
            }

            Console.WriteLine("Finished producing code: " + consoleConfig.Target);
            Console.CursorVisible = true;
            return (int)ExitCodes.Success;
        }

        private static void CSharpProcess(Specification[] specifications, Configuration swaggerConfig, string target)
        {
            var ns = CSharpGenerator.Begin(swaggerConfig);
            foreach (var spec in specifications)
            {
                ns = CSharpGenerator.Go(ns, swaggerConfig, spec);
            }

            CSharpGenerator.End(swaggerConfig, ns, target);
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Takes one or more Swagger specifications and produces a C# output.");
            Console.WriteLine();
            Console.WriteLine("nSwagger.Console.exe [/O language] [/F] [/B] [/NAMESPACE namespace] [/T httptimeout] source target");
            Console.WriteLine();
            var messages = new Dictionary<string, string>
            {
                { "/O language", "Select the type output to generate. `language` can be `C#` (default) or `TypeScript` to generate TypeScript interfaces and definations." },
                { "/F", "Force override of the target if it already exists." },
                { "/B", "Beep when done." },
                { "/NAMESPACE", "For C# this is the namespace for the target file to use. For TypeScript this is the module name. Detault is nSwagger" },
                { "/T", "The HTTP timeout to use in API calls in seconds. Only used with C#. Default is 30 secs" },
                { "source", "One or more, space seperated, paths to Swagger definations. The paths can exist on disk or be a URL." },
                { "target", "The target to write the C# output to. NOTE: This MUST be last." }
            };

            WriteAlignedMessages(messages);

            Console.WriteLine("  Examples:");
            Console.WriteLine();
            var examples = new Dictionary<string, string>
            {
                {"\"nSwagger.Console http://demo.demo/docs/v1 demo.cs\"","This will take the Swagger specification from http://demo.demo/docs/v1 and produce a C# file named demo in the current folder." },
                {"\"nSwagger.Console swagger.json demo.cs\"","This will take the Swagger specification from the local swagger.json file and produce a C# file named demo in the current folder." }
            };

            WriteAlignedMessages(examples, 4);
        }

        private static int Validate(ConsoleConfig config)
        {
            if (config.Sources.Count == 0)
            {
                Console.WriteLine("ERROR: No source files found.");
                return (int)ExitCodes.NoSourceFiles;
            }

            if (!config.Language.Equals("c#", StringComparison.OrdinalIgnoreCase) && !config.Language.Equals("typescript", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("ERROR: Invalid language choosen. It must be 'c#' or 'TypeScript'.");
                return (int)ExitCodes.InvalidLanguage;
            }

            if (string.IsNullOrWhiteSpace(config.Target))
            {
                Console.WriteLine("ERROR: No target file found. Note, it must be the last parameter");
                return (int)ExitCodes.NoTargetFile;
            }

            if (File.Exists(config.Target))
            {
                if (!config.AllowOverride)
                {
                    Console.WriteLine("ERROR: Target file already exists and cannot be overwritten.");
                    return (int)ExitCodes.TargetFileExists;
                }

                File.Delete(config.Target);
            }

            return 0;
        }

        private static void WriteAlignedMessages(Dictionary<string, string> messages, int keyOffset = 2)
        {
            var valueOffSet = messages.OrderByDescending(_ => _.Key.Length).First().Key.Length + 2 + keyOffset;
            var maxMessageLength = Console.WindowWidth - valueOffSet - 2;
            foreach (var message in messages)
            {
                Console.CursorLeft = 0;
                Console.Write(message.Key.PadLeft(keyOffset + message.Key.Length, ' '));
                Console.CursorLeft = valueOffSet;

                if (message.Value.Length > maxMessageLength)
                {
                    var print = "";
                    var words = message.Value.Split(' ');
                    for (var counter = 0; counter < words.Length; counter++)
                    {
                        if (print.Length + 1 + words[counter].Length > maxMessageLength)
                        {
                            Console.CursorLeft = valueOffSet;
                            Console.WriteLine(print);
                            print = "";
                        }

                        print += words[counter] + " ";
                    }

                    Console.CursorLeft = valueOffSet;
                    Console.WriteLine(print);
                }
                else
                {
                    Console.WriteLine(message.Value);
                }
            }

            Console.CursorLeft = 0;
        }

        private class ConsoleConfig
        {
            public bool AllowOverride { get; set; }

            public bool BeepWhenDone { get; set; }

            public string Language { get; set; } = "c#";

            public int HTTPTimeout { get; set; } = 30;

            public List<string> Sources { get; } = new List<string>();

            public string Target { get; set; }
            public string CustomNamespace { get; internal set; }
        }
    }
}