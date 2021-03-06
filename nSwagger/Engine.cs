﻿namespace nSwagger
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

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

    public class EngineRunResult
    {
        public IEnumerable<Specification> Errors { get; set; }
    }

    public static class Engine
    {
        public static async Task<EngineRunResult> Run(Configuration config)
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
                foreach (var spec in specifications.Where(_ => !_.Error))
                {
                    CSharpGenerator.Run(config, spec);
                }
            }

            if (config.Language.HasFlag(TargetLanguage.typescript))
            {
                var generator = new TypeScriptGenerator();
                generator.Run(config, specifications);
            }

            return new EngineRunResult
            {
                Errors = specifications.Where(_ => _.Error)
            };
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
                var settingsFile = $"{originalTarget}.nSwagger";
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
                        throw new nSwaggerException($"Unable to get Swagger defination from URI: {uri.AbsoluteUri}");
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
                        throw new nSwaggerException($"Unable to get Swagger defination from file: {input}");
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

                var targetFileVersionIdentifier = File.ReadLines(config.Target).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(targetFileVersionIdentifier) && targetFileVersionIdentifier.IndexOf(Messages.VersionIdentifierPrefix, StringComparison.OrdinalIgnoreCase) > -1)
                {
                    var versionString = targetFileVersionIdentifier.Substring(targetFileVersionIdentifier.IndexOf(':') + 1);
                    var existingVersion = default(Version);
                    if (Version.TryParse(versionString, out existingVersion))
                    {
                        var currentVersion = Version.Parse(Configuration.nSwaggerVersion);
                        if (currentVersion < existingVersion)
                        {
                            throw new nSwaggerException($"You are attempting to update a nSwagger file using an old version of this tool. You must be on a version of {existingVersion} or later.");
                        }
                    }
                }

                File.Delete(config.Target);
            }
        }
    }
}