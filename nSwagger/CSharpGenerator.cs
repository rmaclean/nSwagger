namespace nSwagger
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public static class CSharpGenerator
    {
        private static Regex arrayClassCleaner = new Regex("(\\[(?<class>\\w+)])");
        private static string httpCode;
        private static string[] nonnullableTypes = { "int", "bool" };

        public static void Run(Configuration config, Specification spec)
        {
            var syntax = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(config.Namespace));
            syntax = syntax.WithLeadingTrivia(syntax.GetLeadingTrivia()
                .Add(SyntaxFactory.Comment($"//{Messages.VersionIdentifierPrefix}:{Configuration.nSwaggerVersion}"))
                .Add(SyntaxFactory.Comment($"// {Messages.Notice}"))
                .Add(SyntaxFactory.Comment($"// {Messages.LastGenerated} {DateTime.UtcNow:o}")));

            syntax = syntax.AddUsings(Using("System"),
                                      Using("System.Collections.Generic"),
                                      Using("System.Diagnostics"),
                                      Using("System.IO"),
                                      Using("System.Linq"),
                                      Using("System.Net"),
                                      Using("System.Net.Http"),
                                      Using("System.Net.Http.Headers"),
                                      Using("System.Threading"),
                                      Using("System.Threading.Tasks"),
                                      Using("Newtonsoft.Json"));

            if (config.IncludeHTTPClientForCSharp)
            {
                httpCode = File.ReadAllText(Path.Combine(config.HTTPCSPath, "HTTPClient.cs"));
            }

            syntax = Go(syntax, config, spec);
            End(config, syntax);
        }

        private static ClassDeclarationSyntax AddClass(ClassDeclarationSyntax baseClass, string name, Property[] properties, Configuration config)
        {
            if (name.Equals("object", StringComparison.OrdinalIgnoreCase))
            {
                return baseClass;
            }

            var className = ClassNameNormaliser(name);

            var @class = SyntaxFactory.ClassDeclaration(className)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.PartialKeyword));

            if (config.AddRefactoringEssentialsPartialClassSupression)
            {
                @class = @class.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(new[] {
                    SyntaxFactory.Attribute(SyntaxFactory.ParseName("System.Diagnostics.CodeAnalysis.SuppressMessage"))
                    .AddArgumentListArguments(SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression("\"\"")),
                                              SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression("\"RECS0001:Class is declared partial but has only one part\"")),
                                              SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression("Justification = \"This is partial to allow the file to extended in a seperate file if needed. Changes to this file would be lost when the code is regenerated and so supporting a seperate file for this is ideal.\"")))
                })));
            }

            if (properties != null)
            {
                foreach (var property in properties)
                {
                    var propertyType = "";
                    if (!string.IsNullOrWhiteSpace(property.Type))
                    {
                        switch (property.Type.ToUpperInvariant())
                        {
                            case "ARRAY":
                                {
                                    var arrayClass = property.ArrayItemType;
                                    if (arrayClass.StartsWith("#/definitions", StringComparison.OrdinalIgnoreCase))
                                    {
                                        arrayClass = RefToClass(arrayClass);
                                    }
                                    else
                                    {
                                        arrayClass = JsonSchemaToDotNetType(className, property.Name, property);
                                    }

                                    propertyType = arrayClass + "[]";
                                    break;
                                }
                            case "STRING":
                                {
                                    propertyType = JsonSchemaToDotNetType(className, property.Name, property);
                                    if (IsJsonSchemaEnum(property))
                                    {
                                        @class = AddEnum(@class, propertyType, property.Enum);
                                    }

                                    break;
                                }
                            default:
                                {
                                    propertyType = JsonSchemaToDotNetType(className, property.Name, property);
                                    break;
                                }
                        }
                    }
                    else
                    {
                        propertyType = RefToClass(property.Ref);
                    }

                    @class = @class.AddMembers(Property(property.Name, propertyType));
                }
            }

            return baseClass.AddMembers(@class);
        }

        private static ClassDeclarationSyntax AddDefination(ClassDeclarationSyntax baseClass, Defination defination, Configuration config) => AddClass(baseClass, defination.Name, defination.Properties, config);

        private static ClassDeclarationSyntax AddEnum(ClassDeclarationSyntax @class, string enumName, string[] enumValues)
        {
            var @enum = SyntaxFactory.EnumDeclaration(enumName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddMembers(enumValues.Select(_ => SyntaxFactory.EnumMemberDeclaration(_)).ToArray());

            return @class.AddMembers(@enum);
        }

        private static ClassDeclarationSyntax AddOperation(ClassDeclarationSyntax @class, OperationConfig config, Configuration swaggerConfig)
        {
            if (config.Operation == null)
            {
                return @class;
            }

            var responseClass = "object";
            var authedCall = false;
            var hasBodyParam = false;
            var parameters = new List<SimplifiedParameter>();
            if (config.Operation.Security?[0].Name?.Equals("oauth", StringComparison.OrdinalIgnoreCase) != null)
            {
                authedCall = true;
                parameters.Add(new SimplifiedParameter
                {
                    Name = "oauthToken",
                    Type = "string",
                    Required = true
                });
            }

            var operationName = config.Operation.OperationId;
            if (string.IsNullOrWhiteSpace(operationName))
            {
                var end = config.Path.IndexOf("/", 2, StringComparison.OrdinalIgnoreCase) - 1;
                if (end < 0)
                {
                    end = config.Path.Length - 1;
                }

                operationName = config.HTTPAction + config.Path.Substring(1, end);
            }

            var methodName = $"{operationName.Replace(" ", "").Trim()}Async";

            if (config.Operation.Parameters != null)
            {
                foreach (var @param in config.Operation.Parameters)
                {
                    var name = @param.Name;
                    var type = "";
                    var typeFormat = "";
                    var @default = "";
                    if (@param.GetType() == typeof(BodyParameter))
                    {
                        hasBodyParam = true;
                        var bodyParam = @param as BodyParameter;
                        if (!string.IsNullOrWhiteSpace(bodyParam.Schema.Ref))
                        {
                            type = RefToClass(bodyParam.Schema.Ref);
                        }
                        else
                        {
                            type = bodyParam.Schema.Type;
                            typeFormat = bodyParam.Schema.Format;
                        }

                        @default = bodyParam.Schema.Default;
                    }
                    else
                    {
                        var otherParam = @param as OtherParameter;
                        type = otherParam.Type;
                        typeFormat = otherParam.Format;
                        @default = otherParam.Default;
                    }

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        parameters.Add(new SimplifiedParameter
                        {
                            Default = @default,
                            Name = name,
                            Type = type.Equals("array", StringComparison.OrdinalIgnoreCase) ? "object[]" : JsonSchemaToDotNetType(type, typeFormat),
                            Location = @param.In,
                            Description = @param.Description,
                            Required = @param.Required
                        });
                    }
                }
            }

            var successResponse = config.Operation.Responses.Where(_ => _.HttpStatusCode >= 200 && _.HttpStatusCode <= 299).OrderBy(_ => _.HttpStatusCode).FirstOrDefault();
            if (successResponse == null)
            {
                return @class;
            }

            if (successResponse.Schema != null)
            {
                if (!string.IsNullOrWhiteSpace(successResponse.Schema.Ref))
                {
                    responseClass = RefToClass(successResponse.Schema.Ref);
                }
                else
                {
                    switch (successResponse.Schema.Type.ToUpperInvariant())
                    {
                        case "OBJECT":
                            {
                                responseClass = ClassNameNormaliser($"{operationName}Out");
                                @class = AddClass(@class, responseClass, successResponse.Schema.Properties, swaggerConfig);
                                break;
                            }
                        case "ARRAY":
                            {
                                var arrayClass = successResponse.Schema.Items[0];
                                var resultClass = "";
                                if (arrayClass.Ref.StartsWith("#/definitions", StringComparison.OrdinalIgnoreCase))
                                {
                                    resultClass = RefToClass(arrayClass.Ref);
                                }
                                else
                                {
                                    resultClass = JsonSchemaToDotNetType(operationName, "Out", arrayClass);
                                }

                                responseClass = resultClass + "[]";
                                break;
                            }
                        default:
                            {
                                responseClass = ClassNameNormaliser(successResponse.Schema.Type);
                                break;
                            }
                    }
                }
            }

            responseClass = ClassNameNormaliser(responseClass);

            var method = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<APIResponse<{responseClass}>>"), methodName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword));

            if (parameters.Count > 0)
            {
                method = method.AddParameterListParameters(parameters.Select(_ => Parameter(_)).ToArray());
            }

            var httpMethod = "await ";
            switch (config.HTTPAction)
            {
                case HTTPAction.Put:
                    {
                        httpMethod += "httpClient.PutAsync";
                        break;
                    }
                case HTTPAction.Get:
                    {
                        httpMethod += "httpClient.GetAsync";
                        break;
                    }
                case HTTPAction.Post:
                    {
                        httpMethod += "httpClient.PostAsync";
                        break;
                    }
                case HTTPAction.Delete:
                    {
                        httpMethod += "httpClient.DeleteAsync";
                        break;
                    }
                case HTTPAction.Head:
                    {
                        httpMethod += "httpClient.HeadAsync";
                        break;
                    }
                case HTTPAction.Options:
                    {
                        httpMethod += "httpClient.OptionsAsync";
                        break;
                    }
                case HTTPAction.Patch:
                    {
                        httpMethod += "httpClient.PatchAsync";
                        break;
                    }
            }

            var urlPath = config.Path;
            var queryParameterCode = "";
            var addQueryParameterCode = false;
            if (parameters != null)
            {
                var operationParameters = parameters.Where(_ => _.Location != null && (_.Location.Equals("PATH", StringComparison.OrdinalIgnoreCase) || _.Location.Equals("QUERY", StringComparison.OrdinalIgnoreCase))).ToArray();
                if (operationParameters.Any())
                {
                    queryParameterCode = $"var queryParameters = new Dictionary<string,object>({operationParameters.Length});";
                    foreach (var urlParam in operationParameters)
                    {
                        var target = $"{{{urlParam.Name}}}";
                        var value = $"\"+{urlParam.Name}+\"";
                        if (urlPath.Contains(target))
                        {
                            urlPath = urlPath.Replace(target, value);
                        }
                        else
                        {
                            addQueryParameterCode = true;

                            if (urlParam.Nullable)
                            {
                                queryParameterCode += $"if ({urlParam.Name}.HasValue) {{";
                                queryParameterCode += $"queryParameters.Add(\"{urlParam.Name}\", {urlParam.Name}.Value);";
                                queryParameterCode += "}";
                            }
                            else
                            {
                                queryParameterCode += $"queryParameters.Add(\"{urlParam.Name}\",{urlParam.Name});";
                            }
                        }
                    }
                }
            }

            var querySuffix = "";
            if (addQueryParameterCode)
            {
                querySuffix = "+\"?\" + queryParameters.Aggregate(\"\", (curr, next) => (curr.Length > 0 ? \"?\" : \"\") + next.Key + \"=\" + next.Value)";
            }

            httpMethod += $"(new Uri(url + \"{urlPath}\"{querySuffix}, UriKind.Absolute), new SwaggerHTTPClientOptions(TimeSpan.FromSeconds({swaggerConfig.HTTPTimeout.TotalSeconds}))";
            if (hasBodyParam)
            {
                var bodyParam = parameters.SingleOrDefault(_ => _.Location != null && (_.Location.Equals("body", StringComparison.OrdinalIgnoreCase)));
                if (bodyParam != null)
                {
                    httpMethod += $", new StringContent(JsonConvert.SerializeObject({bodyParam.Name}))";
                }

                var formDataParams = parameters.Where(_ => _.Location != null && (_.Location.Equals("formdata", StringComparison.OrdinalIgnoreCase)));
                if (formDataParams.Any())
                {
                    var formDataValue = formDataParams.Aggregate("", (curr, next) => curr + (curr.Length > 0 ? ", " : "") + next.Name);
                    httpMethod += $@", new StringContent(JsonConvert.SerializeObject(new {{{formDataValue}}}))";
                }
            }

            if (authedCall)
            {
                httpMethod += ", token: oauthToken";
            }

            httpMethod += ");";

            var methodBody = $@"
{{
{(addQueryParameterCode ? queryParameterCode : "")}
var response = {httpMethod}
if (response == null)
{{
return new APIResponse<{responseClass}>(false);
}}

 switch ((int)response.StatusCode)
 {{";

            var successId = config.Operation.Responses.First(_ => _.HttpStatusCode >= 200 && _.HttpStatusCode <= 299).HttpStatusCode;
            foreach (var response in config.Operation.Responses)
            {
                methodBody += $@"case {response.HttpStatusCode}:
{{
";
                if (response.HttpStatusCode == successId)
                {
                    if (response.Schema == null || response.Schema.Type == "object")
                    {
                        methodBody += $"return new APIResponse<{responseClass}>(response.StatusCode);";
                    }
                    else
                    {
                        methodBody += $@"var data = JsonConvert.DeserializeObject<{responseClass}>(await response.Content.ReadAsStringAsync());
return new APIResponse<{responseClass}>(data, response.StatusCode);";
                    }
                }
                else
                {
                    if (response.Schema == null || response.Schema.Type == "object")
                    {
                        methodBody += $"return new APIResponse<{responseClass}>(response.StatusCode);";
                    }
                    else
                    {
                        var specialData = string.IsNullOrWhiteSpace(response.Schema.Type) ? RefToClass(response.Schema.Ref) : ClassNameNormaliser(response.Schema.Type);
                        methodBody += $@"var data = JsonConvert.DeserializeObject<{ClassNameNormaliser(specialData)}>(await response.Content.ReadAsStringAsync());
return new APIResponse<{responseClass}>(data, response.StatusCode);";
                    }
                }

                methodBody += "}";
            }

            methodBody += $@"default:
         {{
             return new APIResponse<{responseClass}>(response.StatusCode);
         }}
 }}
}}";

            var xmlComments = new List<SyntaxTrivia>();

            if (!string.IsNullOrWhiteSpace(config.Operation.Summary))
            {
                xmlComments.AddRange(AddXmlComment("summary", CleanXMLComment(config.Operation.Summary)));
            }

            if (!string.IsNullOrWhiteSpace(config.Operation.Description))
            {
                xmlComments.AddRange(AddXmlComment("remarks", CleanXMLComment(config.Operation.Description)));
            }

            if (!string.IsNullOrWhiteSpace(successResponse.Description))
            {
                xmlComments.AddRange(AddXmlComment("returns", CleanXMLComment(successResponse.Description)));
            }

            xmlComments.AddRange(parameters.Select(_ => AddXmlParamComment(_.Name, _.Description)));
            method = method
                .AddBodyStatements(SyntaxFactory.ParseStatement(methodBody))
                .WithLeadingTrivia(SyntaxExtensions.ToSyntaxTriviaList(xmlComments));

            return @class.AddMembers(method);
        }

        private static SyntaxTrivia[] AddXmlComment(string tag, string text) => new[] {
                    SyntaxFactory.Comment($"//<{tag}>"),
                    SyntaxFactory.Comment($"// {text}"),
                    SyntaxFactory.Comment($"//</{tag}>")};

        private static SyntaxTrivia AddXmlParamComment(string paramName, string text) => SyntaxFactory.Comment($"//<param name=\"{paramName}\">{text}</param>");

        private static string ClassNameNormaliser(string className)
        {
            var result = className.Replace(" ", "");
            while (arrayClassCleaner.IsMatch(result))
            {
                result = arrayClassCleaner.Replace(result, "Of${class}");
            }

            if (result.Equals("object", StringComparison.OrdinalIgnoreCase))
            {
                return "object";
            }

            return JsonSchemaToDotNetType(result, null);
        }

        private static string CleanXMLComment(string description)
        {
            var lines = description.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 1)
            {
                return description;
            }

            return lines.Aggregate("", (curr, next) => curr + (curr.Length > 0 ? Environment.NewLine + "        //" : "") + next.Trim());
        }

        private static ConstructorDeclarationSyntax Constructor(string className, string body, params SimplifiedParameter[] parameters)
        {
            var constructor = SyntaxFactory.ConstructorDeclaration(className)
               .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
               .AddParameterListParameters(parameters.Select(_ => Parameter(_)).ToArray())
               .AddBodyStatements(body.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Select(_ => SyntaxFactory.ParseStatement(_)).ToArray());

            return constructor;
        }

        private static void End(Configuration config, NamespaceDeclarationSyntax ns)
        {
            var cu = SyntaxFactory.CompilationUnit();
            cu = cu.AddMembers(ns);
            cu = cu.NormalizeWhitespace();

            if (!config.DoNotWriteTargetFile)
            {
                using (var writer = new StringWriter())
                {
                    cu.WriteTo(writer);
                    File.WriteAllText(config.Target, writer.ToString());
                }
            }
        }

        private static PropertyDeclarationSyntax Field(string name, string type) => SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(type), name).AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        private static NamespaceDeclarationSyntax Go(NamespaceDeclarationSyntax ns, Configuration config, Specification spec)
        {
            var className = ClassNameNormaliser(spec.Info.Title);

            var constructorCode = $@"if (!string.IsNullOrWhiteSpace(url)){{ this.url = url;}} else {{ this.url = ""{spec.Schemes[0]}://{spec.Host}""; }}";
            if (config.IncludeHTTPClientForCSharp)
            {
                constructorCode += Environment.NewLine + " if (httpClient == null){ this.httpClient = new SwaggerHTTPClient();} else { this.httpClient = httpClient; }";
            }

            var baseClass = SyntaxFactory.ClassDeclaration(className)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddMembers(Field("url", "string").AddModifiers(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)),
                            Field("httpClient", "ISwaggerHTTPClient").AddModifiers(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)),
                Constructor(className, constructorCode, new SimplifiedParameter("url", "string", "null", true), new SimplifiedParameter("httpClient", "ISwaggerHTTPClient", config.IncludeHTTPClientForCSharp ? "null" : null, true)))
                .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken));

            if (spec.Definations != null)
            {
                foreach (var defination in spec.Definations)
                {
                    baseClass = AddDefination(baseClass, defination, config);
                }
            }

            foreach (var path in spec.Paths)
            {
                baseClass = AddOperation(baseClass, OperationConfig.FromValues(HTTPAction.Delete, path.Delete, path.Path), config);
                baseClass = AddOperation(baseClass, OperationConfig.FromValues(HTTPAction.Get, path.Get, path.Path), config);
                baseClass = AddOperation(baseClass, OperationConfig.FromValues(HTTPAction.Post, path.Post, path.Path), config);
                baseClass = AddOperation(baseClass, OperationConfig.FromValues(HTTPAction.Put, path.Put, path.Path), config);
                baseClass = AddOperation(baseClass, OperationConfig.FromValues(HTTPAction.Head, path.Head, path.Path), config);
                baseClass = AddOperation(baseClass, OperationConfig.FromValues(HTTPAction.Options, path.Options, path.Path), config);
                baseClass = AddOperation(baseClass, OperationConfig.FromValues(HTTPAction.Patch, path.Patch, path.Path), config);
            }

            if (config.IncludeHTTPClientForCSharp)
            {
                baseClass = baseClass.WithTrailingTrivia(SyntaxFactory.Comment(httpCode));
            }

            return ns.AddMembers(baseClass);
        }

        private static bool IsJsonSchemaEnum(IJsonSchema schema) => schema.Type.ToUpperInvariant().Equals("STRING") && schema.Enum != null && schema.Enum.Length > 0;

        private static string JsonSchemaToDotNetType(string type, string format)
        {
            switch (type.ToUpperInvariant())
            {
                case "DATE-TIME":
                    {
                        return "DateTime";
                    }
                case "INTEGER":
                    {
                        if (!string.IsNullOrWhiteSpace(format))
                        {
                            if (format.Equals("int32", StringComparison.OrdinalIgnoreCase))
                            {
                                return "int";
                            }

                            if (format.Equals("int64", StringComparison.OrdinalIgnoreCase))
                            {
                                return "long";
                            }
                        }

                        return "int";
                    }
                case "NUMBER":
                    {
                        if (!string.IsNullOrWhiteSpace(format))
                        {
                            if (format.Equals("float", StringComparison.OrdinalIgnoreCase))
                            {
                                return "float";
                            }

                            if (format.Equals("double", StringComparison.OrdinalIgnoreCase))
                            {
                                return "double";
                            }
                        }

                        return "double";
                    }
                case "BOOLEAN":
                    {
                        return "bool";
                    }
                case "STRING":
                    {
                        if (!string.IsNullOrWhiteSpace(format))
                        {
                            if (format.Equals("date", StringComparison.OrdinalIgnoreCase) ||
                                format.Equals("date-time", StringComparison.OrdinalIgnoreCase))
                            {
                                return "DateTime";
                            }
                        }

                        break;
                    }
            }

            return type;
        }

        private static string JsonSchemaToDotNetType(string parentName, string nodeName, IJsonSchema schema)
        {
            if (IsJsonSchemaEnum(schema))
            {
                return parentName + nodeName;
            }

            if (schema.Type.Equals("array", StringComparison.OrdinalIgnoreCase))
            {
                var schemaAsProperty = schema as Property;
                if (schemaAsProperty != null)
                {
                    return JsonSchemaToDotNetType(schemaAsProperty.ArrayItemType, null) + "[]";
                }

                return "object[]";
            }

            return JsonSchemaToDotNetType(schema.Type, schema.Format);
        }

        private static ParameterSyntax Parameter(SimplifiedParameter parameterValue)
        {
            var paramType = parameterValue.Type;
            if (!parameterValue.Required && nonnullableTypes.Contains(paramType))
            {
                parameterValue.Nullable = true;
                paramType = $"Nullable<{paramType}>";
            }

            var param = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterValue.Name)).WithType(SyntaxFactory.ParseTypeName(paramType));
            if (!string.IsNullOrWhiteSpace(parameterValue.Default))
            {
                param = param.WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(parameterValue.Default)));
            }

            return param;
        }

        private static StatementSyntax[] ParseStatements(string content)
        {
            var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var result = new StatementSyntax[lines.Length];
            for (var counter = 0; counter < lines.Length; counter++)
            {
                result[counter] = SyntaxFactory.ParseStatement(lines[counter]);
            }

            return result;
        }

        private static PropertyDeclarationSyntax Property(string name, string type) => SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(ClassNameNormaliser(type)), name)
                                                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                                    .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                                                                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

        private static string RefToClass(string @ref) => @ref.Substring(@ref.IndexOf('/', 2) + 1);

        private static UsingDirectiveSyntax Using(string name) => SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(name));

        private class OperationConfig
        {
            public HTTPAction HTTPAction { get; private set; }

            public Operation Operation { get; private set; }

            public string Path { get; private set; }

            public static OperationConfig FromValues(HTTPAction action, Operation operation, string path) => new OperationConfig
            {
                HTTPAction = action,
                Operation = operation,
                Path = path
            };
        }

        private class SimplifiedParameter
        {
            public SimplifiedParameter()
            {
            }

            public SimplifiedParameter(string name, string type, string defaultValue, bool required)
            {
                Name = name;
                Type = type;
                Default = defaultValue;
                Required = required;
            }

            public string Default { get; set; }

            public string Description { get; set; }

            public string Location { get; set; }

            public string Name { get; set; }
            public bool Nullable { get; internal set; }
            public bool Required { get; set; }

            public string Type { get; set; }

            public static SimplifiedParameter FromStringArray(string[] values)
            {
                var result = new SimplifiedParameter
                {
                    Name = values[0],
                    Type = values[1]
                };

                if (values.Length > 2)
                {
                    result.Default = values[2];
                }

                return result;
            }

            public static implicit operator SimplifiedParameter(string[] values) => FromStringArray(values);
        }
    }
}