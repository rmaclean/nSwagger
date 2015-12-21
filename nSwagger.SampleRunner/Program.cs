namespace nSwagger.SampleRunner
{
    using System;
    using System.IO;
    using System.Reflection;

    public class Program
    {
        public static void Main()
        {
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..\\..\\..\\tests\\");
            var files = Directory.GetFiles(path);
            var config = new Configuration
            {
                DoNotWriteTargetFile = true,
                Language = TargetLanguage.csharp,
                SaveSettings = false
            };

            foreach (var file in files)
            {
                Console.WriteLine($"[C#] Testing against {Path.GetFileName(file)}");
                config.Sources = new[] { file };
                Engine.Run(config).Wait();
            }

            config.Language = TargetLanguage.typescript;
            foreach (var file in files)
            {
                Console.WriteLine($"[TS] Testing against {Path.GetFileName(file)}");
                config.Sources = new[] { file };
                Engine.Run(config).Wait();
            }
        }
    }
}