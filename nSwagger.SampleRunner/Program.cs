namespace nSwagger.SampleRunner
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public class Program
    {
        private static void WriteError(string error)
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
                Console.WriteLine(line);
            }
            Console.ForegroundColor = preColour;
        }

        public static void Main()
        {
            var tsc = @"C:\Users\v-robmc\AppData\Roaming\npm\tsc.cmd";
            var testAgainstTypeScriptCompiler = File.Exists(tsc);
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..\\..\\..\\tests\\");
            var files = Directory.GetFiles(path);
            var config = new Configuration
            {
                DoNotWriteTargetFile = true,
                Language = TargetLanguage.csharp,
                SaveSettings = false
            };

            Console.WriteLine("Testing C# Client Libraries: ");

            foreach (var file in files)
            {
                Console.WriteLine($"\tTesting against {Path.GetFileName(file)}");
                config.Sources = new[] { file };
                Engine.Run(config).Wait();
                Console.WriteLine();
            }

            Console.WriteLine("Testing TypeScript Definations: ");
            config.Language = TargetLanguage.typescript;
            config.DoNotWriteTargetFile = false;
            foreach (var file in files)
            {
                Console.WriteLine($"\tTesting against {Path.GetFileName(file)}");
                config.Sources = new[] { file };
                config.Target = Path.GetTempFileName() + ".ts";
                Console.WriteLine($"\tOutput to {config.Target}");
                Engine.Run(config).Wait();
                if (testAgainstTypeScriptCompiler)
                {
                    var processInfo = new ProcessStartInfo(tsc, config.Target);
                    processInfo.UseShellExecute = false;
                    processInfo.RedirectStandardOutput = true;

                    using (var process = Process.Start(processInfo))
                    {
                        var result = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        WriteError(result);
                    }
                }

                Console.WriteLine();
            }
        }   
    }
}