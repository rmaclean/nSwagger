namespace nSwagger
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;
    using System.IO;
    using System.Reflection;

    [Flags]
    public enum TargetLanguage
    {
        csharp = 1,
        typescript = 2
    }

    public class Configuration
    {
        public Configuration()
        {
            AddRefactoringEssentialsPartialClassSupression = true;
            HTTPCSPath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
        }

        public bool AddRefactoringEssentialsPartialClassSupression { get; set; }

        public bool AllowOverride { get; set; }

        public bool DoNotWriteTargetFile { get; set; }

        public string HTTPCSPath { get; set; }

        public TimeSpan HTTPTimeout { get; set; } = TimeSpan.FromSeconds(30);

        [JsonConverter(typeof(StringEnumConverter))]
        public TargetLanguage Language { get; set; }

        public string Namespace { get; set; } = "nSwagger";

        public string nSwaggerVersion { get; } = "0.0.1";

        public bool SaveSettings { get; set; }

        public string[] Sources { get; set; }

        public string Target { get; set; }

        public bool IncludeHTTPClientForCSharp { get; set; } = true;
    }
}