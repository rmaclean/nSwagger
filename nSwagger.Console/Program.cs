namespace nSwagger.Console
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

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
            InvalidLanguage = 32,
            InvalidConfig = 64
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

            var consoleConfig = new ConsoleConfig();
            var swaggerConfig = new Configuration();

            if (args.Length == 2 && args[0].Equals("/L"))
            {
                var settingsFile = args[1];
                swaggerConfig = Configuration.LoadFromFile(settingsFile);
                if (swaggerConfig == null)
                {
                    Console.WriteLine("ERROR: Configuration file provided not found or invalid");
                    return (int)ExitCodes.InvalidConfig;
                }
            }
            else
            {
                swaggerConfig.Target = args.Last();
                var sources = new List<string>();
                for (var count = 0; count < args.Length - 1; count++)
                {
                    var argument = args[count];
                    if (argument.Equals("/F", StringComparison.OrdinalIgnoreCase))
                    {
                        swaggerConfig.AllowOverride = true;
                        continue;
                    }

                    if (argument.Equals("/B", StringComparison.OrdinalIgnoreCase))
                    {
                        consoleConfig.BeepWhenDone = true;
                        continue;
                    }

                    if (argument.Equals("/S", StringComparison.OrdinalIgnoreCase))
                    {
                        swaggerConfig.SaveSettings = true;
                        continue;
                    }

                    if (argument.Equals("/NAMESPACE", StringComparison.OrdinalIgnoreCase))
                    {
                        count++;
                        swaggerConfig.Namespace = args[count];
                        continue;
                    }

                    if (argument.Equals("/O", StringComparison.OrdinalIgnoreCase))
                    {
                        count++;
                        var language = TargetLanguage.csharp;
                        if (args[count].Equals("c#", StringComparison.OrdinalIgnoreCase))
                        {
                            language = TargetLanguage.csharp;
                        }

                        if (args[count].Equals("typescript", StringComparison.OrdinalIgnoreCase))
                        {
                            language = TargetLanguage.typescript;
                        }

                        swaggerConfig.Language = language;
                        continue;
                    }

                    if (argument.Equals("/T", StringComparison.OrdinalIgnoreCase))
                    {
                        count++;
                        var timeoutInSeconds = 30;
                        if (int.TryParse(args[count], out timeoutInSeconds))
                        {
                            swaggerConfig.HTTPTimeout = TimeSpan.FromSeconds(timeoutInSeconds);
                        }

                        continue;
                    }

                    sources.Add(argument);
                }

                swaggerConfig.Sources = sources.ToArray();
            }

            try
            {
                var engine = Engine.Run(swaggerConfig);
                engine.Wait();

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

            Console.WriteLine("Finished producing code: " + swaggerConfig.Target);
            Console.CursorVisible = true;
            return (int)ExitCodes.Success;
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Takes one or more Swagger specifications and produces a C# output.");
            Console.WriteLine();
            Console.WriteLine("nSwagger.Console.exe [/O language] [/F] [/B] [/NAMESPACE namespace] [/T httptimeout] [/S] [/L settings] source target");
            Console.WriteLine();
            var messages = new Dictionary<string, string>
            {
                { "/O language", "Select the type output to generate. `language` can be `C#` (default) or `TypeScript` to generate TypeScript interfaces and definations." },
                { "/F", "Force override of the target if it already exists." },
                { "/B", "Beep when done." },
                { "/NAMESPACE", "For C# this is the namespace for the target file to use. For TypeScript this is the module name. Detault is nSwagger" },
                { "/T", "The HTTP timeout to use in API calls in seconds. Only used with C#. Default is 30 secs" },
                { "/S", "Save the settings to a file named the same as target with .json appended" },
                { "/L settings", "Load the settings from a file. This switch must be can be run by itself." },
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
            public bool BeepWhenDone { get; set; }
        }
    }
}