namespace nSwagger
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public static class Parser
    {
        public static Specification Parse(string content)
        {
            var parsed = JsonConvert.DeserializeObject(content) as JObject;
            return ParseSwaggerObject(parsed);
        }

        private static Header CreateArrayHeader(JProperty header)
        {
            var result = new HeaderArray
            {
                Items = new[] { ParseItems(header["items"]) }
            };

            return result;
        }

        private static Parameter ParseBodyParameter(JToken parameter)
        {
            var result = new BodyParameter();
            SetCommonParameter(result, parameter);
            result.Required = parameter["required"]?.Value<bool>() ?? false;
            result.Schema = ParseSchema(parameter["schema"]);
            if (result.Schema == null)
            {
                result.Schema = new Schema
                {
                    Type = "string"
                };
            }

            return result;
        }

        private static void ParseCommonOtherParameter(OtherParameter item, JToken parameter)
        {
            item.AllowEmptyValue = parameter["allowEmptyValue"]?.Value<bool>() ?? false;
            item.Required = parameter["required"]?.Value<bool>() ?? true;
            ParseJsonSchema(item, parameter);
        }

        private static Contact ParseContact(JObject contact)
        {
            if (contact == null)
            {
                return null;
            }

            var result = new Contact();
            result.Email = contact["email"]?.Value<string>();
            result.Url = contact["url"]?.Value<string>();
            result.Name = contact["name"]?.Value<string>();
            return result;
        }

        private static Defination[] ParseDefinations(JToken definations)
        {
            if (definations == null)
            {
                return null;
            }

            var result = new List<Defination>();
            foreach (JProperty defination in definations)
            {
                var item = new Defination
                {
                    Name = defination.Name
                };

                ParseJsonSchema(item, defination.Value);
                item.Properties = ParseProperties(defination.Value["properties"]);
                result.Add(item);
            }

            return result.ToArray();
        }

        private static Example[] ParseExample(JToken examples)
        {
            if (examples == null)
            {
                return null;
            }

            var results = new List<Example>();
            foreach (JProperty example in examples)
            {
                var result = new Example
                {
                    MimeType = example.Name,
                    Value = example.Value<string>()
                };
            }

            return results.ToArray();
        }

        private static ExternalDocs ParseExternalDocs(JToken externalDocs)
        {
            if (externalDocs == null)
            {
                return null;
            }

            return new ExternalDocs
            {
                Url = externalDocs["url"].Value<string>(),
                Description = externalDocs["description"]?.Value<string>()
            };
        }

        private static Header[] ParseHeaders(JToken headers)
        {
            if (headers == null)
            {
                return null;
            }

            var result = new List<Header>();

            foreach (JProperty header in headers)
            {
                var item = default(Header);
                var type = header["type"].Value<string>();
                switch (type.ToUpperInvariant())
                {
                    case "ARRAY":
                        {
                            item = CreateArrayHeader(header);
                            break;
                        }
                    default:
                        {
                            item = new Header();
                            break;
                        }
                }

                SetCommonHeaderValues(item, header, type);
                result.Add(item);
            }

            return result.ToArray();
        }

        private static Info ParseInfoObject(JObject infoNode)
        {
            var result = new Info
            {
                Title = infoNode["title"].Value<string>(),
                Version = infoNode["version"].Value<string>()
            };

            result.Description = infoNode["description"]?.Value<string>();
            result.TermsOfService = infoNode["termsOfService"]?.Value<string>();
            result.Contact = ParseContact(infoNode["contact"] as JObject);
            result.License = ParseLicense(infoNode["license"] as JObject);

            Debug.WriteLine("Finished parsing info. {0}", result);
            return result;
        }

        private static Item ParseItems(JToken items)
        {
            if (items == null)
            {
                return null;
            }

            if (items["$ref"] != null)
            {
                return new Item
                {
                    Ref = items["$ref"].Value<string>()
                };
            }

            if (items["type"] != null)
            {
                var type = items["type"].Value<string>();

                switch (type.ToUpperInvariant())
                {
                    case "ARRAY":
                        {
                            var newItem = new ArrayItems
                            {
                                Type = type
                            };

                            newItem.Items = new[] { ParseItems(items["items"]) };
                            ParseJsonSchema(newItem, items["items"]);
                            return newItem;
                        }
                    default:
                        {
                            var newItem = new Item
                            {
                                Type = type
                            };

                            ParseJsonSchema(newItem, items);
                            return newItem;
                        }
                }
            }

            return null;
        }

        private static void ParseJsonSchema(IJsonSchema item, JToken node)
        {
            item.Format = node["format"]?.Value<string>();
            item.CollectionFormat = node["collectionFormat"]?.Value<string>();
            item.Default = node["default"]?.Value<string>();
            item.Maximum = node["maximum"]?.Value<double>();
            item.ExclusiveMaximum = node["exclusiveMaximum"]?.Value<bool>() ?? false;
            item.Minimum = node["minimum"]?.Value<double>();
            item.ExclusiveMinimum = node["exclusiveMinimum"]?.Value<bool>() ?? false;
            item.MaxLength = node["maxLength"]?.Value<int>();
            item.MinLength = node["minLength"]?.Value<int>();
            item.Pattern = node["pattern"]?.Value<string>();
            item.MaxItems = node["maxItems"]?.Value<int>();
            item.MinItems = node["minItems"]?.Value<int>();
            item.UniqueItems = node["uniqueItems"]?.Value<bool>() ?? false;
            item.Enum = node["enum"]?.Values<string>().ToArray();
            item.MultipleOf = node["multipleOf"]?.Value<double>();
        }

        private static License ParseLicense(JObject license)
        {
            if (license == null)
            {
                return null;
            }

            var result = new License
            {
                Name = license["name"].Value<string>()
            };

            result.Url = license["url"]?.Value<string>();
            return result;
        }

        private static Operation ParseOperation(JToken operation)
        {
            if (operation == null)
            {
                return null;
            }

            var result = new Operation();
            result.OperationId = operation["operationId"]?.Value<string>();
            Debug.WriteLine($"Starting parsing operation {result.OperationId}");
            result.Tags = operation["tags"]?.Values<string>().ToArray();
            result.Summary = operation["summary"]?.Value<string>();
            result.Description = operation["description"]?.Value<string>();
            result.ExternalDocs = ParseExternalDocs(operation["externalDocs"]);
            result.Consumes = operation["consumes"]?.Values<string>().ToArray();
            result.Produces = operation["produces"]?.Values<string>().ToArray();
            result.Parameters = ParseParameters(operation["parameters"]);
            result.Responses = ParseResponses(operation["responses"]);
            result.Schemes = operation["schemes"]?.Values<string>().ToArray();
            result.Deprecated = operation["deprecated"]?.Value<bool>() ?? false;
            result.Security = ParseSecurityRequirements(operation["security"] as JArray);
            Debug.WriteLine($"Finished parsing operation {result.OperationId}");
            return result;
        }

        private static void ParseOtherArrayParameter(OtherArrayParameter item, JToken parameter)
        {
            item.Items = new[] { ParseItems(parameter["items"]) };
        }

        private static Parameter[] ParseParameters(JToken parameters)
        {
            if (parameters == null)
            {
                return null;
            }

            var result = new List<Parameter>();
            foreach (var parameter in parameters)
            {
                var item = default(Parameter);
                var @in = parameter["in"].Value<string>();
                switch (parameter["in"].Value<string>().ToUpperInvariant())
                {
                    case "HEADER":
                        {
                            item = new OtherParameter
                            {
                                Name = parameter["name"].Value<string>(),
                                In = "header",
                                Required = parameter["required"]?.Value<bool>() ?? true,
                                Description = parameter["description"]?.Value<string>(),
                                Type = "string"
                            };

                            break;
                        }

                    case "FORMDATA":
                    case "BODY":
                        {
                            item = ParseBodyParameter(parameter);
                            break;
                        }
                    case "PATH":
                    case "QUERY":
                        {
                            item = ParsePathParameter(parameter);
                            break;
                        }
                }

                result.Add(item);
            }

            return result.ToArray();
        }

        private static PathItem[] ParsePath(JObject paths)
        {
            var result = new List<PathItem>();
            foreach (JProperty path in paths.Children())
            {
                var pathItem = new PathItem
                {
                    Path = path.Name
                };

                pathItem.Get = ParseOperation(path.Children()["get"].SingleOrDefault());
                pathItem.Put = ParseOperation(path.Children()["put"].SingleOrDefault());
                pathItem.Post = ParseOperation(path.Children()["post"].SingleOrDefault());
                pathItem.Delete = ParseOperation(path.Children()["delete"].SingleOrDefault());
                pathItem.Options = ParseOperation(path.Children()["options"].SingleOrDefault());
                pathItem.Head = ParseOperation(path.Children()["head"].SingleOrDefault());
                pathItem.Patch = ParseOperation(path.Children()["patch"].SingleOrDefault());
                pathItem.Parameters = ParseParameters(path.Children()["parameters"].SingleOrDefault());
                result.Add(pathItem);
            }

            Debug.WriteLine($"Paths parsed: {result.Aggregate("", (curr, next) => curr + (curr.Length > 0 ? ", " : "") + next)}");

            return result.ToArray();
        }

        private static Parameter ParsePathParameter(JToken parameter)
        {
            var type = parameter["type"].Value<string>();
            var item = default(OtherParameter);
            switch (type.ToUpper())
            {
                case "ARRAY":
                    {
                        item = new OtherArrayParameter();
                        ParseOtherArrayParameter(item as OtherArrayParameter, parameter);
                        break;
                    }
                default:
                    {
                        item = new OtherParameter();
                        break;
                    }
            }

            item.Name = parameter["name"].Value<string>();
            item.In = parameter["in"].Value<string>();
            item.Type = type;
            item.Required = parameter["required"]?.Value<bool>() ?? true;
            ParseCommonOtherParameter(item, parameter);
            return item;
        }

        private static Property[] ParseProperties(JToken properties)
        {
            if (properties == null)
            {
                return null;
            }

            var result = new List<Property>();
            foreach (JProperty property in properties)
            {
                var item = new Property();
                item.Name = property.Name;
                if (property.Value["$ref"] != null)
                {
                    item.Ref = property.Value["$ref"].Value<string>();
                }
                else
                {
                    ParseJsonSchema(item, property.Value);
                    item.Type = property.Value["type"].Value<string>();
                    if (item.Type.Equals("array", StringComparison.OrdinalIgnoreCase))
                    {
                        var items = property.Value["items"];
                        if (items["type"] != null)
                        {
                            item.ArrayItemType = items["type"].Value<string>();
                        }
                        else
                        {
                            item.ArrayItemType = items["$ref"].Value<string>();
                        }
                    }
                }

                result.Add(item);
            }

            return result.ToArray();
        }

        private static Response[] ParseResponses(JToken responses)
        {
            if (responses == null)
            {
                return null;
            }

            var result = new List<Response>();
            foreach (JProperty response in responses)
            {
                var statusCode = 0;
                if (!int.TryParse(response.Name, out statusCode))
                {
                    statusCode = 200;
                }

                var item = new Response
                {
                    HttpStatusCode = statusCode,
                    Description = response.Children()["description"].SingleOrDefault()?.Value<string>(),
                    Schema = ParseSchema(response.Children()["schema"].SingleOrDefault()),
                    Headers = ParseHeaders(response.Children()["header"].SingleOrDefault()),
                    Example = ParseExample(response.Children()["example"].SingleOrDefault())
                };

                result.Add(item);
            }

            return result.ToArray();
        }

        private static Schema ParseSchema(JToken schema)
        {
            if (schema == null)
            {
                return null;
            }

            var item = new Schema();

            if (schema["$ref"] != null)
            {
                item.Ref = schema["$ref"].Value<string>();
            }
            else
            {
                item.Title = schema["title"]?.Value<string>();
                item.Description = schema["description"]?.Value<string>();
                ParseJsonSchema(item, schema);
                item.Items = new[] { ParseItems(schema["items"]) };
                item.Properties = ParseProperties(schema["properties"]);
                item.Discriminator = schema["discriminator"]?.Value<string>();
                item.ReadOnly = schema["readOnly"]?.Value<bool>() ?? false;
                item.XML = schema["xml"]?.Value<string>();
                item.ExternalDocs = ParseExternalDocs(schema["externalDocs"]);
                item.Example = schema["example"]?.Value<string>();
                item.Type = schema["type"]?.Value<string>();
            }

            return item;
        }

        private static Scope[] ParseScope(JToken scopes)
        {
            if (scopes == null)
            {
                return null;
            }

            var result = new List<Scope>();
            foreach (JProperty scope in scopes)
            {
                var item = new Scope
                {
                    Name = scope.Name,
                    Value = scope.Value.Value<string>()
                };

                result.Add(item);
            }

            return result.ToArray();
        }

        private static SecurityDefination[] ParseSecurityDefinations(JToken securityDefinitions)
        {
            if (securityDefinitions == null)
            {
                return null;
            }

            var result = new List<SecurityDefination>();
            foreach (JProperty securityDefinition in securityDefinitions)
            {
                var type = securityDefinition.First["type"].Value<string>();
                var item = new SecurityDefination
                {
                    DefinationName = securityDefinition.Name,
                    Type = type,
                    Description = securityDefinition.First["description"]?.Value<string>()
                };

                if (type.Equals("apiKey", StringComparison.OrdinalIgnoreCase))
                {
                    item.Name = securityDefinition.First["name"].Value<string>();
                    item.In = securityDefinition.First["in"].Value<string>();
                }

                if (type.Equals("oauth2", StringComparison.OrdinalIgnoreCase))
                {
                    var flow = securityDefinition.First["flow"].Value<string>();
                    item.Flow = flow;
                    item.Scopes = ParseScope(securityDefinition.First["scopes"]);
                    if (flow.Equals("implicit", StringComparison.OrdinalIgnoreCase))
                    {
                        item.AuthorizationUrl = securityDefinition.First["authorizationUrl"].Value<string>();
                    }

                    if (flow.Equals("password", StringComparison.OrdinalIgnoreCase) || flow.Equals("application", StringComparison.OrdinalIgnoreCase))
                    {
                        item.TokenUrl = securityDefinition.First["tokenUrl"]?.Value<string>();
                    }

                    if (flow.Equals("accessCode", StringComparison.OrdinalIgnoreCase))
                    {
                        item.AuthorizationUrl = securityDefinition.First["authorizationUrl"].Value<string>();
                        item.TokenUrl = securityDefinition.First["tokenUrl"].Value<string>();
                    }
                }

                result.Add(item);
            }

            return result.ToArray();
        }

        private static SecurityRequirement[] ParseSecurityRequirements(JArray securitySchemes)
        {
            if (securitySchemes == null)
            {
                return null;
            }

            var result = new List<SecurityRequirement>();
            foreach (JObject securityObject in securitySchemes)
            {
                var security = ((securitySchemes.First() as JObject).First as JProperty);
                var item = new SecurityRequirement
                {
                    Name = security.Name,
                    Values = (security.Value as JArray).Values<string>().ToArray()
                };

                result.Add(item);
            }

            return result.ToArray();
        }

        private static Specification ParseSwaggerObject(JObject parsed)
        {
            Debug.WriteLine("Starting parsing");
            var result = new Specification();
            result.Swagger = parsed["swagger"].Value<string>();
            Debug.WriteLine($"Swagger version {result.Swagger}");
            result.Info = ParseInfoObject(parsed["info"] as JObject);
            result.Host = parsed["host"]?.Value<string>();
            Debug.WriteLine($"Host: {result.Host}");
            result.BasePath = parsed["basePath"]?.Value<string>();
            Debug.WriteLine($"Base Path: {result.BasePath}");
            result.Schemes = parsed["schemes"]?.Values<string>().ToArray();
            Debug.WriteLine($"Schemes: {result.Schemes?.Aggregate("", (curr, next) => curr + (curr.Length > 0 ? ", " : "") + next)}");
            result.Consumes = parsed["consumes"]?.Values<string>().ToArray();
            Debug.WriteLine($"Consumes: {result.Consumes?.Aggregate("", (curr, next) => curr + (curr.Length > 0 ? ", " : "") + next)}");
            result.Produces = parsed["produces"]?.Values<string>().ToArray();
            Debug.WriteLine($"Produces: {result.Produces?.Aggregate("", (curr, next) => curr + (curr.Length > 0 ? ", " : "") + next)}");
            result.Paths = ParsePath(parsed["paths"] as JObject);
            result.Definations = ParseDefinations(parsed["definitions"]);
            result.Parameters = ParseParameters(parsed["parameters"]);
            result.Responses = ParseResponses(parsed["responses"]);
            result.SecurityDefinations = ParseSecurityDefinations(parsed["securityDefinitions"]);
            result.Security = ParseSecurityRequirements(parsed["security"] as JArray);
            result.Tags = ParseTags(parsed["tags"]);
            result.ExternalDocs = ParseExternalDocs(parsed["externalDocs"]);
            return result;
        }

        private static Tag[] ParseTags(JToken tags)
        {
            if (tags == null)
            {
                return null;
            }

            var result = new List<Tag>();
            foreach (var tag in tags)
            {
                var item = new Tag
                {
                    Name = tag["name"].Value<string>(),
                    Description = tag["description"]?.Value<string>(),
                    ExternalDocs = ParseExternalDocs(tag["externalDocs"])
                };

                result.Add(item);
            }

            return result.ToArray();
        }

        private static void SetCommonHeaderValues(Header item, JProperty header, string type)
        {
            item.Key = header.Name;
            item.Description = header["description"]?.Value<string>();
            item.Type = type;
            ParseJsonSchema(item, header);
        }

        private static void SetCommonParameter(Parameter result, JToken parameter)
        {
            result.Name = parameter["name"].Value<string>();
            result.In = parameter["in"].Value<string>();
            result.Description = parameter["description"]?.Value<string>();
        }
    }
}