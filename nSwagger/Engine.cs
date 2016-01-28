namespace nSwagger
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    public enum HTTPAction
    {
        Put,
        Get,
        Post,
        Delete,
        Head,
        Options,
        Patch
    }

    public static class Engine
    {
        public static async Task Run(Configuration config)
        {
            Validation(config);
            var specifications = await GetSpecifications(config);
            var targetPath = Path.GetDirectoryName(config.Target);
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            if (config.Language.HasFlag(TargetLanguage.csharp))
            {
                foreach (var spec in specifications)
                {
                    CSharpGenerator.Run(config, spec);
                }
            }

            if (config.Language.HasFlag(TargetLanguage.typescript))
            {
                var generator = new TypeScriptGenerator();
                generator.Run(config, specifications);
            }
        }

        private static async Task<Specification[]> GetSpecifications(Configuration config)
        {
            var result = new List<Specification>();
            var files = await InputsToFiles(config.Sources);
            foreach (var file in files)
            {
                var specification = Parser.Parse(File.ReadAllText(file));
                result.Add(specification);
            }

            if (config.SaveSettings)
            {
                var originalTarget = config.Target;
                config.Target = Path.GetFileName(originalTarget);
                var serialisedSettings = JsonConvert.SerializeObject(config);                
                var settingsFile = originalTarget + ".nSwagger";
                if (File.Exists(settingsFile))
                {
                    File.Delete(settingsFile);
                }

                File.WriteAllText(settingsFile, serialisedSettings);
                config.Target = originalTarget;
            }

            return result.ToArray();
        }

        private static async Task<IEnumerable<string>> InputsToFiles(string[] inputs)
        {
            var files = new string[inputs.Length];
            var position = 0;
            foreach (var input in inputs)
            {
                var path = "";
                var uri = default(Uri);
                var parseAsURL = false;
                if (Uri.TryCreate(input, UriKind.Absolute, out uri))
                {
                    parseAsURL = (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) || uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase));
                }

                if (parseAsURL)
                {
                    //uri
                    var result = await HTTP.HTTP.HTTPCallAsync("GET", uri, new HTTP.HTTPOptions(TimeSpan.FromMinutes(2)));
                    if (result == null)
                    {
                        throw new nSwaggerException("Unable to get Swagger defination from URI: " + uri.AbsoluteUri);
                    }

                    var temp = Path.GetTempFileName();
                    using (var fileStream = new FileStream(temp, FileMode.Create, FileAccess.Write))
                    {
                        var resultStream = await result.Content.ReadAsStreamAsync();
                        await resultStream.CopyToAsync(fileStream);
                    }

                    path = temp;
                }
                else
                {
                    //file
                    if (!File.Exists(input))
                    {
                        throw new nSwaggerException("Unable to get Swagger defination from file: " + input);
                    }

                    path = input;
                }

                files[position] = path;
                position++;
            }

            return files;
        }

        private static void Validation(Configuration config)
        {
            if (config.Sources.Length == 0)
            {
                throw new nSwaggerException("No source files found.");
            }

            if (!config.DoNotWriteTargetFile && string.IsNullOrWhiteSpace(config.Target))
            {
                throw new nSwaggerException("No target file found. Note, it must be the last parameter");
            }

            if (!config.DoNotWriteTargetFile && File.Exists(config.Target))
            {
                if (!config.AllowOverride)
                {
                    throw new nSwaggerException("Target file already exists and cannot be overwritten.");
                }

                File.Delete(config.Target);
            }
        }
    }
}