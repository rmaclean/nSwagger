namespace SwaggerParser
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class Program
    {
        public static void Main(string[] args)
        {
            args = new[] {
                @"C:\Users\v-robmc\Desktop\New folder\auth.json",
                @"C:\Users\v-robmc\Desktop\New folder\code.cs",
            };

            var output = args.Last();
            var input = new string[args.Length - 1];
            for (int index = 0; index < input.Length; index++)
            {
                input[index] = args[index];
            }

            var definations = input.Select(file => SwaggerDefination2.FromNode(JsonConvert.DeserializeObject<JObject>(File.ReadAllText(file)))).ToArray();

            Debugger.Break();
        }

        public static void Main1(string[] args)
        {
            args = new[] 
                {
                    @"C:\Users\v-robmc\Desktop\New folder\auth.json",
                    @"C:\Users\v-robmc\Desktop\New folder\calendar.json",
                    @"C:\Users\v-robmc\Desktop\New folder\group.json",
                    @"C:\Users\v-robmc\Desktop\New folder\message.json",
                    @"C:\Users\v-robmc\Desktop\New folder\code.cs",
                };

            var output = args.Last();
            var input = new string[args.Length - 1];
            for (int index = 0; index < input.Length; index++)
            {
                input[index] = args[index];
            }

            var definations = input.Select(file => ParseSwagger(File.ReadAllText(file))).ToList();
            var code = new StringBuilder();
            var classes = new Dictionary<string, IEnumerable<SwaggerPropertry>>();

            code.AppendLine("namespace API");
            code.AppendLine("{");
            code.AppendLine("    using System;");
            code.AppendLine("    using System.Threading.Tasks;");
            code.AppendLine("    using Newtonsoft.Json;");
            code.AppendLine();

            foreach (var defination in definations)
            {
                var apiClassName = defination.Name.Replace(" ", "");
                code.AppendLine("    /// <summary>");
                code.AppendLine("    /// " + defination.Description);
                code.AppendLine("    /// </summary>");
                if (defination.Warnings.Any())
                {
                    code.AppendLine("    /// <remarks>");
                    code.AppendLine("    /// The following issues were found in the swagger file");
                    foreach (var warning in defination.Warnings)
                    {
                        code.AppendLine("    /// - " + warning);
                    }

                    code.AppendLine("    /// </remarks>");
                }

                code.AppendLine("    public class " + apiClassName);
                code.AppendLine("    {");
                code.AppendLine("        private readonly string baseUri;");
                code.AppendLine();

                code.AppendLine("        public " + apiClassName + "(string baseUri = \"" + defination.BaseUri.AbsoluteUri + "\")");
                code.AppendLine("        {");
                code.AppendLine("            this.baseUri = baseUri;");
                code.AppendLine("        }");

                foreach (var method in defination.Methods)
                {
                    var methodName = method.Path.IndexOf('/', 1) >= 0 ? method.Path.Substring(1, method.Path.IndexOf('/', 1) - 1) : method.Path.Substring(1);
                    code.AppendLine();
                    code.AppendLine("        /// <summary>");
                    code.AppendLine("        /// " + method.Description);
                    code.AppendLine("        /// </summary>");

                    var parameters = "";

                    if (method.TokenAuth)
                    {
                        code.AppendLine("        /// <param name=\"token\">The OAuth security token</param>");
                        parameters += "string token";
                    }

                    foreach (var parameter in method.Parameters)
                    {
                        code.AppendLine("        /// <param name=\"" + parameter.Name + "\">" + parameter.Description + "</param>");
                        if (parameters.Length > 0)
                        {
                            parameters += ", ";
                        }

                        if (parameter.Type == null)
                        {
                            var @class = BuildClass(parameter.Name, parameter.Properties, methodName, "in");
                            classes.Add(@class.Key, @class.Value);
                            parameters += @class.Key + " " + parameter.Name;

                            var newClass = new SwaggerClass
                            {
                                Name = @class.Key,
                            };

                            newClass.Properties.AddRange(@class.Value);
                            defination.Classes.Add(newClass);
                        }
                        else
                        {
                            if (parameter.CustomType)
                            {
                                parameters += parameter.Type + " " + parameter.Name;
                            }
                            else
                            {
                                parameters += ClassMapper(parameter.Type) + " " + parameter.Name;
                            }
                        }
                    }

                    var successResponse = method.Responses.First(_ => _.Code == 200);
                    var responseClass = "";
                    code.AppendLine("        /// <returns>" + successResponse.Description + "</returns>");

                    var hasResponse = false;
                    var responseType = "";

                    if (successResponse.Properties.Count < 1)
                    {
                        responseClass = "Task";
                        if (!string.IsNullOrWhiteSpace(successResponse.Type))
                        {
                            hasResponse = true;
                            responseType = successResponse.Type;
                            responseClass += "<" + successResponse.Type;
                            if (successResponse.IsArray)
                            {
                                responseClass += "[]";
                                responseType += "[]";
                            }

                            responseClass += ">";
                        }
                    }

                    if (successResponse.Properties.Count == 1)
                    {
                        var successResponseProperty = successResponse.Properties[0];
                        var @class = ClassName(successResponseProperty);
                        responseClass = "Task<" + @class + ">";
                        responseType = @class;
                        hasResponse = true;
                    }

                    if (successResponse.Properties.Count > 1)
                    {
                        var @class = BuildClass("", successResponse.Properties, methodName, "out");
                        classes.Add(@class.Key, @class.Value);
                        responseClass = "Task<" + @class.Key + ">";
                        var newClass = new SwaggerClass
                        {
                            Name = @class.Key,
                        };

                        newClass.Properties.AddRange(@class.Value);
                        defination.Classes.Add(newClass);
                        responseType = @class.Key;
                        hasResponse = true;
                    }

                    if (method.Responses.Any(_ => _.Code != 200))
                    {
                        code.AppendLine("        /// <remarks>");
                        code.AppendLine("        /// Other response codes");
                        foreach (var otherResponse in method.Responses.Where(_ => _.Code != 200))
                        {
                            code.AppendLine("        /// " + otherResponse.Code + ": " + otherResponse.Description);
                        }

                        code.AppendLine("        /// </remarks>");
                    }

                    code.AppendLine("        public async " + responseClass + " " + method.HTTPAction + "_" + methodName + "(" + parameters + ")");
                    code.AppendLine("        {");
                    code.AppendLine("            var http = new HTTP();");
                    var actionLine = "";
                    if (hasResponse)
                    {
                        actionLine += "var response = ";
                    }

                    var methodPath = method.Path;
                    var pathParams = method.Parameters.Where(_ => _.Location.Equals("path", StringComparison.OrdinalIgnoreCase));
                    foreach (var pathParam in pathParams)
                    {
                        methodPath = methodPath.Replace("{" + pathParam.Name + "}", "\" + " + pathParam.Name + " + \"");
                    }

                    var queryParams = method.Parameters.Where(_ => _.Location.Equals("query", StringComparison.OrdinalIgnoreCase));
                    var firstQueryParam = true;
                    foreach (var queryParam in queryParams)
                    {
                        if (firstQueryParam)
                        {
                            firstQueryParam = false;
                            methodPath += "?";
                        }
                        else
                        {
                            methodPath += "&";
                        }

                        methodPath += queryParam.Name + "=\" + " + queryParam.Name + " + \"";
                    }

                    if (method.HTTPAction.Equals("post", StringComparison.OrdinalIgnoreCase))
                    {
                        var bodyParam = method.Parameters.FirstOrDefault(_ => _.Location.Equals("body", StringComparison.OrdinalIgnoreCase));
                        if (bodyParam != null)
                        {
                            code.AppendLine("            var bodyJson = JsonConvert.SerializeObject(" + bodyParam.Name + ");");
                        }

                        actionLine += (hasResponse ? "await http.PostString" : "var success = await http.PostBool") + "(new Uri(baseUri + \"" + methodPath + "\", UriKind.Absolute)";

                        if (bodyParam != null)
                        {
                            actionLine += ", bodyJson";
                        }
                    }

                    if (method.HTTPAction.Equals("get", StringComparison.OrdinalIgnoreCase))
                    {
                        actionLine += "await http.GetString(new Uri(baseUri + \"" + methodPath + "\", UriKind.Absolute)";
                    }

                    if (method.TokenAuth)
                    {
                        actionLine += ", token: token";
                    }

                    actionLine += ");";

                    actionLine = actionLine.Replace(" + \"\", UriKind.Absolute)", ", UriKind.Absolute)");

                    code.AppendLine("            " + actionLine);

                    if (hasResponse)
                    {
                        code.AppendLine("            if (string.IsNullOrWhiteSpace(response))");
                        code.AppendLine("            {");
                        code.AppendLine("                return null;");
                        code.AppendLine("            }");
                        code.AppendLine();
                        code.AppendLine("            return JsonConvert.DeserializeObject<" + responseType + ">(response);");
                    }
                    else
                    {
                        if (method.HTTPAction.Equals("post", StringComparison.OrdinalIgnoreCase))
                        {
                            code.AppendLine("            if (!success)");
                            code.AppendLine("            {");
                            code.AppendLine("                throw new Exception();");
                            code.AppendLine("            }");
                        }
                    }

                    code.AppendLine("        }");
                }

                code.AppendLine("    }");
                code.AppendLine();

                foreach (var @class in defination.Classes)
                {
                    var className = @class.Name.Replace(" ", "");
                    code.AppendLine("    public class " + className);
                    code.AppendLine("    {");
                    foreach (var classProperty in @class.Properties)
                    {
                        code.AppendLine("        public " + ClassName(classProperty) + " " + classProperty.Name + " { get; set; }");
                        code.AppendLine();
                    }
                    code.AppendLine("    }");
                    code.AppendLine();
                }
            }
            code.AppendLine("}");

            File.WriteAllText(output, code.ToString());
            //Console.WriteLine(code.ToString());
            //Debugger.Break();
        }

        private static KeyValuePair<string, IEnumerable<SwaggerPropertry>> BuildClass(string parameterName, IEnumerable<SwaggerPropertry> properties, string methodName, string direction)
        {
            var className = methodName + "_" + direction;
            if (!string.IsNullOrWhiteSpace(parameterName) && !parameterName.Equals("body", StringComparison.OrdinalIgnoreCase))
            {
                className += "_" + parameterName;
            }

            return new KeyValuePair<string, IEnumerable<SwaggerPropertry>>(className, properties);
        }

        private static string ClassMapper(string javascriptClass)
        {
            switch (javascriptClass.ToUpperInvariant())
            {
                case "STRING":
                    {
                        return "string";
                    }
                case "ARRAY":
                    {
                        return "";
                    }
                case "NUMBER":
                    {
                        return "double";
                    }
                case "BOOLEAN":
                    {
                        return "bool";
                    }
                default:
                    {
                        throw new Exception();
                    }
            }
        }

        private static string ClassName(SwaggerPropertry property)
        {
            var className = "";
            if (property.CustomType)
            {
                className = property.Type;
            }
            else
            {
                className = ClassMapper(property.Type);
            }

            if (property.IsArray)
            {
                className += "[]";
            }

            return className;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns>
        /// </returns>
        private static IEnumerable<SwaggerPropertry> GetProperties(JToken node)
        {
            if (node == null)
            {
                return new List<SwaggerPropertry>(0);
            }

            var result = new List<SwaggerPropertry>();

            var properties = node.Children();
            foreach (var propertyParent in properties)
            {
                var property = propertyParent.First();

                var customType = false;
                var propertyType = "";
                if (property["type"] != null)
                {
                    propertyType = property["type"].Value<string>();
                }
                else
                {
                    if (property["$ref"] != null)
                    {
                        propertyType = property["$ref"].Value<string>().Substring(14);
                        customType = true;
                    }
                }

                var isArray = propertyType.Equals("array", StringComparison.OrdinalIgnoreCase);
                if (isArray)
                {
                    propertyType = property["items"]["$ref"].Value<string>().Substring(14);
                    customType = true;
                }

                if (string.IsNullOrWhiteSpace(propertyType))
                {
                    throw new Exception();
                }

                var swaggerProperty = new SwaggerPropertry
                {
                    Name = (propertyParent as JProperty).Name,
                    Type = propertyType,
                    Description = property["description"]?.Value<string>(),
                    IsArray = isArray,
                    CustomType = customType
                };

                result.Add(swaggerProperty);
            }

            return result;
        }

        private static SwaggerDefination ParseSwagger(string json)
        {
            var defination = new SwaggerDefination();
            var parsedJson = JsonConvert.DeserializeObject<JObject>(json);

            if (parsedJson["info"] != null)
            {
                defination.Name = parsedJson["info"]["title"].Value<string>();
                defination.Description = parsedJson["info"]["description"].Value<string>();
            }

            var url = "";
            if (parsedJson["schemes"] != null)
            {
                var schemes = parsedJson["schemes"].AsJEnumerable().Values<string>().ToArray();
                if (schemes.Length == 1)
                {
                    url = schemes.First();
                }
                else
                {
                    url = schemes.Any(_ => _.Equals("https", StringComparison.OrdinalIgnoreCase)) ? "https" : schemes.First();
                }
            }
            else
            {
                url = "http";
                defination.Warnings.Add("Missing scheme");
            }

            url += "://";
            url += parsedJson.Value<string>("host");
            url += parsedJson.Value<string>("basePath");
            defination.BaseUri = new Uri(url, UriKind.Absolute);

            if (parsedJson["definitions"] != null)
            {
                var classes = parsedJson["definitions"].AsJEnumerable();
                foreach (var classParent in classes)
                {
                    var @class = classParent.First();
                    var swaggerClass = new SwaggerClass
                    {
                        Name = (classParent as JProperty).Name,
                    };

                    swaggerClass.Properties.AddRange(GetProperties(@class["properties"]));

                    defination.Classes.Add(swaggerClass);
                }
            }

            var paths = parsedJson["paths"].AsJEnumerable();
            foreach (var path in paths)
            {
                var methods = path.First().Children();
                foreach (var methodParent in methods)
                {
                    var method = methodParent.First();

                    var swaggerMethod = new SwaggerMethod
                    {
                        Path = (path as JProperty).Name,
                        HTTPAction = (methodParent as JProperty).Name,
                        Description = method["description"].Value<string>()
                    };

                    if (method["security"] != null)
                    {
                        var securityMethods = method["security"].AsEnumerable();
                        foreach (var securityMethodParent in securityMethods)
                        {
                            var securityMethod = securityMethodParent.First();
                            var name = (securityMethod as JProperty).Name;
                            if (name.Equals("token", StringComparison.OrdinalIgnoreCase))
                            {
                                swaggerMethod.TokenAuth = true;
                            }
                        }
                    }

                    if (method["parameters"] != null)
                    {
                        var parameters = method["parameters"].AsJEnumerable();
                        foreach (var parameter in parameters)
                        {
                            var swaggerParameter = new SwaggerParameter
                            {
                                Description = parameter["description"]?.Value<string>(),
                                Location = parameter["in"].Value<string>(),
                                Name = parameter["name"].Value<string>(),
                                Required = parameter["required"].Value<bool>(),
                                Type = parameter["type"]?.Value<string>(),
                                CustomType = false
                            };

                            if (parameter["schema"] != null)
                            {
                                var schemaType = parameter["schema"]?["type"]?.Value<string>();
                                if (schemaType == null || schemaType.Equals("object", StringComparison.OrdinalIgnoreCase))
                                {
                                    var properties = parameter["schema"]["properties"];
                                    if (properties == null)
                                    {
                                        if (parameter["schema"]["$ref"] != null)
                                        {
                                            swaggerParameter.Type = parameter["schema"]["$ref"].Value<string>().Substring(14);
                                            swaggerParameter.CustomType = true;
                                        }
                                    }
                                    else
                                    {
                                        swaggerParameter.Properties.AddRange(GetProperties(properties));
                                    }
                                }
                                else
                                {
                                    swaggerParameter.Type = schemaType;
                                    swaggerParameter.Description += " - " + parameter["schema"]["description"].Value<string>();
                                }
                            }

                            swaggerMethod.Parameters.Add(swaggerParameter);
                        }
                    }

                    var responses = method["responses"].Children();
                    foreach (var responseParent in responses)
                    {
                        var response = responseParent.First();
                        var swaggerResponse = new SwaggerResponse
                        {
                            Code = Convert.ToInt32((responseParent as JProperty).Name),
                            Description = response["description"].Value<string>(),
                            IsArray = false,
                            Type = null,
                        };

                        if (response["schema"] != null)
                        {
                            var schemaType = response["schema"]["type"].Value<string>();
                            if (schemaType.Equals("object", StringComparison.OrdinalIgnoreCase))
                            {
                                swaggerResponse.Properties.AddRange(GetProperties(response["schema"]["properties"]));
                            }

                            if (schemaType.Equals("array", StringComparison.OrdinalIgnoreCase))
                            {
                                swaggerResponse.IsArray = true;
                                swaggerResponse.Type = response["schema"]["items"]["$ref"].Value<string>().Substring(14);
                            }
                        }

                        swaggerMethod.Responses.Add(swaggerResponse);
                    }

                    defination.Methods.Add(swaggerMethod);
                }
            }

            return defination;
        }
    }

    internal class SwaggerClass
    {
        public string Name { get; set; }

        public List<SwaggerPropertry> Properties { get; } = new List<SwaggerPropertry>();
    }

    internal class SwaggerDefination
    {
        public Uri BaseUri { get; set; }

        public List<SwaggerClass> Classes { get; } = new List<SwaggerClass>();

        public string Description { get; internal set; }

        public List<SwaggerMethod> Methods { get; } = new List<SwaggerMethod>();

        public string Name { get; set; }

        public List<string> Warnings { get; } = new List<string>();
    }

    internal class SwaggerMethod
    {
        public string Description { get; set; }

        public string HTTPAction { get; set; }

        public List<SwaggerParameter> Parameters { get; } = new List<SwaggerParameter>();

        public string Path { get; set; }

        public List<SwaggerResponse> Responses { get; } = new List<SwaggerResponse>();

        public bool TokenAuth { get; set; }
    }

    internal class SwaggerParameter
    {
        public bool CustomType { get; internal set; }

        public string Description { get; set; }

        public string Location { get; set; }

        public string Name { get; set; }

        public List<SwaggerPropertry> Properties { get; } = new List<SwaggerPropertry>();

        public bool Required { get; set; }

        public string Type { get; internal set; }
    }

    internal class SwaggerPropertry
    {
        public bool CustomType { get; internal set; }

        public string Description { get; set; }

        public bool IsArray { get; internal set; }

        public string Name { get; set; }

        public string Type { get; set; }
    }

    internal class SwaggerResponse
    {
        public int Code { get; set; }

        public string Description { get; set; }

        public bool IsArray { get; internal set; }

        public List<SwaggerPropertry> Properties { get; } = new List<SwaggerPropertry>();

        public string Type { get; internal set; }
    }
}