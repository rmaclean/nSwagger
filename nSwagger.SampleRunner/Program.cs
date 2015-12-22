namespace nSwagger.SampleRunner
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public class Program
    {
        public static void Main()
        {
            Console.Title = "Testing nSwagger";
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..\\..\\..\\tests\\");
            var files = Directory.GetFiles(path);
            var config = new Configuration
            {
                Language = TargetLanguage.csharp,
                SaveSettings = false
            };

            TextCSharp(files, config);
            TestTypeScript(files, config);
        }

        private static void TestTypeScript(string[] files, Configuration config)
        {
            WriteLine("Testing TypeScript Definations: ", ConsoleColor.Cyan);
            var tsc = @"C:\Users\v-robmc\AppData\Roaming\npm\tsc.cmd";
            var testAgainstTypeScriptCompiler = File.Exists(tsc);
            config.Language = TargetLanguage.typescript;
            config.DoNotWriteTargetFile = false;
            foreach (var file in files)
            {
                WriteLine($"\tTesting against {Path.GetFileName(file)}", ConsoleColor.White);
                config.Sources = new[] { file };
                config.Target = Path.GetTempFileName() + ".ts";
                Console.WriteLine($"\tOutput to {config.Target}");
                Engine.Run(config).Wait();
                if (testAgainstTypeScriptCompiler)
                {
                    RunCompiler(config, tsc);
                }

                Console.WriteLine();
            }
        }

        private static void RunCompiler(Configuration config, string command, Func<string,string> errorFilter = null, params string[] args)
        {
            var arguments = "";
            if (args.Any())
            {
                arguments = args.Aggregate((curr, next) => curr + " " + next) + " ";
            }

            arguments += config.Target;

            var processInfo = new ProcessStartInfo(command, arguments);
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;

            using (var process = Process.Start(processInfo))
            {
                var result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                WriteError(result, errorFilter);
            }
        }

        private static string KnownCSharpErrors(string input)
        {
            if (input.Contains("CS0246"))
            {
                if (input.Contains("'Newtonsoft'"))
                {
                    return null;
                }

                if (input.Contains("'HttpClient'"))
                {
                    return null;
                }

                if (input.Contains("'HttpContent'"))
                {
                    return null;
                }

                if (input.Contains("'HttpResponseMessage'"))
                {
                    return null;
                }
            }

            if (input.Contains("CS0234") && input.Contains("'System.Net'") && input.Contains("'Http'"))
            {
                return null;
            }

            return input;
        }

        private static void TextCSharp(string[] files, Configuration config)
        {
            WriteLine("Testing C# Client Libraries: ", ConsoleColor.Green);
            var csc = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"..\..\..\packages\Microsoft.Net.Compilers.1.1.1\tools\csc.exe");
            var testAgainstCSharpCompiler = File.Exists(csc);
            foreach (var file in files)
            {
                WriteLine($"\tTesting against {Path.GetFileName(file)}", ConsoleColor.White);
                config.Sources = new[] { file };
                config.Target = Path.GetTempFileName() + ".cs";
                Console.WriteLine($"\tOutput to {config.Target}");
                Engine.Run(config).Wait();
                if (testAgainstCSharpCompiler)
                {
                    RunCompiler(config, csc, KnownCSharpErrors, "/nologo");
                }

                Console.WriteLine();
            }
        }

        private static void WriteError(string error, Func<string,string> filter = null)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return;
            }

            var preColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            var lines = error.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Select(_ => "\t" + _);
            foreach (var line in lines)
            {
                var result = line;
                if (filter != null)
                {
                    result = filter(line);
                    if (result == null)
                    {
                        continue;
                    }
                }

                Console.WriteLine(result);
            }

            Console.ForegroundColor = preColour;
        }

        private static void WriteLine(string message, ConsoleColor colour)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var preColour = Console.ForegroundColor;
            Console.ForegroundColor = colour;
            Console.WriteLine(message);
            Console.ForegroundColor = preColour;
        }
    }
}