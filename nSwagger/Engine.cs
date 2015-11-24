namespace nSwagger
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    public static class Engine
    {
        public static async Task<Specification[]> Run(params string[] inputs)
        {
            var result = new List<Specification>();
            var files = await InputsToFiles(inputs);
            foreach (var file in files)
            {
                try
                {
                    var specification = Parser.Parse(File.ReadAllText(file));
                    result.Add(specification);
                }
                catch (Exception ex)
                {
                    Debugger.Break();
                    throw ex;
                }

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
                if (Uri.TryCreate(input, UriKind.Absolute, out uri))
                {
                    //uri
                    var result = await HTTP.HTTP.GetStreamAsync(uri);
                    if (!result.HTTPStatusCode.HasValue || result.HTTPStatusCode.Value != System.Net.HttpStatusCode.OK)
                    {
                        throw new nSwaggerException("Unable to get Swagger defination from URI: " + uri.AbsoluteUri);
                    }

                    var temp = Path.GetTempFileName();
                    using (var fileStream = new FileStream(temp, FileMode.Create, FileAccess.Write))
                    {
                        await result.Data.CopyToAsync(fileStream);
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
    }
}