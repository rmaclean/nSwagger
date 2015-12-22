namespace nSwagger.SampleRunner
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public class Program
    {
        private static string SolutionRoot;

        public static void Main()
        {
            Console.Title = "Testing nSwagger";
            SolutionRoot = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"..\..\..\");
            var path = Path.Combine(SolutionRoot, "examples");
            var files = Directory.GetFiles(path);
            var config = new Configuration
            {
                Language = TargetLanguage.csharp,
                AllowOverride = true
            };

            TestCSharp(files, config);
            TestTypeScript(files, config);
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

        private static void RunCompiler(Configuration config, string command, Func<string, string> errorFilter = null, params string[] args)
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

        private static void TestAgainst(TestAgainstConfig config)
        {
            var fullExamplePath = Path.Combine(SolutionRoot + config.ExamplesDirectory);
            WriteLine(config.Title, ConsoleColor.Green);
            var testAgainstCompiler = File.Exists(config.CompilerPath);
            foreach (var file in config.Files)
            {
                WriteLine($"\tTesting against {Path.GetFileName(file)}", ConsoleColor.White);
                config.Config.Sources = new[] { file };
                config.Config.Target = Path.Combine(fullExamplePath + Path.GetFileNameWithoutExtension(file) + config.Extension);
                config.Config.Language = config.Language;
                Console.WriteLine($"\tOutput to {config.Config.Target}");
                Engine.Run(config.Config).Wait();
                if (testAgainstCompiler)
                {
                    RunCompiler(config.Config, config.CompilerPath, config.ErrorFilter, config.CompilerArguments);
                }

                Console.WriteLine();
            }
        }

        private static void TestCSharp(string[] files, Configuration config)
        {
            var testConfig = new TestAgainstConfig
            {
                Files = files,
                Config = config,
                CompilerArguments = new[] { "/nologo" },
                CompilerPath = Path.Combine(SolutionRoot, @"packages\Microsoft.Net.Compilers.1.1.1\tools\csc.exe"),
                ErrorFilter = KnownCSharpErrors,
                ExamplesDirectory = @"examples\C#-Examples\",
                Extension = ".cs",
                Language = TargetLanguage.csharp,
                Title = "Testing C# Client Libraries: "
            };

            TestAgainst(testConfig);
        }

        private static void TestTypeScript(string[] files, Configuration config)
        {
            var testConfig = new TestAgainstConfig
            {
                Files = files,
                Config = config,
                CompilerArguments = new[] { "--noEmit" },
                CompilerPath = @"C:\Users\v-robmc\AppData\Roaming\npm\tsc.cmd",
                ExamplesDirectory = @"examples\TS-Examples\",
                Extension = ".ts",
                Language = TargetLanguage.typescript,
                Title = "Testing TypeScript Definations: "
            };

            TestAgainst(testConfig);
        }

        private static void WriteError(string error, Func<string, string> filter = null)
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

    internal class TestAgainstConfig
    {
        public string[] CompilerArguments { get; set; }

        public string CompilerPath { get; set; }

        public Configuration Config { get; set; }

        public Func<string, string> ErrorFilter { get; set; }

        public string ExamplesDirectory { get; set; }

        public string Extension { get; set; }

        public string[] Files { get; set; }

        public TargetLanguage Language { get; set; }

        public string Title { get; set; }
    }
}