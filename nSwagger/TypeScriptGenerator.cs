namespace nSwagger
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public class CoderStringBuilder
    {
        private readonly StringBuilder stringBuilder;
        private string _padding;
        private int indent = 0;

        public CoderStringBuilder()
        {
            stringBuilder = new StringBuilder();
        }

        public void AppendLine()
        {
            stringBuilder.AppendLine();
        }

        public void AppendLine(string line, bool noPadding = false)
        {
            var padding = "";
            if (!noPadding)
            {
                padding = _padding;
            }

            stringBuilder.AppendLine(padding + line);
        }

        public void Indent()
        {
            indent += 4;
            _padding = "".PadRight(indent, ' ');
        }

        public void Outdent()
        {
            indent -= 4;
            _padding = "".PadRight(indent, ' ');
        }

        public override string ToString() => stringBuilder.ToString();

        internal void Append(string content, bool noPadding = false)
        {
            var padding = "";
            if (!noPadding)
            {
                padding = _padding;
            }

            stringBuilder.Append(padding + content);
        }
    }

    public class TypeScriptGenerator
    {
        private List<string> existingInterfaces = new List<string>();
        private Regex splitOutGeneric = new Regex("(?<generic>\\w+)\\[(?<type>\\w+)]");
        private Regex whitespace = new Regex("\\s");

        public string ItemTypeCleaner(Item item)
        {
            if (!string.IsNullOrWhiteSpace(item.Ref))
            {
                return RefToClass(item.Ref);
            }

            if (item.Type.Equals("array", StringComparison.OrdinalIgnoreCase))
            {
                return "Array<any>";
            }

            return CleanClassName(item.Type);
        }

        public void Run(Configuration swaggerConfig, Specification[] specifications)
        {
            existingInterfaces.Add("any");
            var output = new CoderStringBuilder();
            output.AppendLine($"//{Messages.VersionIdentifierPrefix}:{swaggerConfig.nSwaggerVersion}");
            output.AppendLine($"// {Messages.Notice}");
            output.AppendLine($"namespace {swaggerConfig.Namespace} {{");
            output.Indent();
            foreach (var specification in specifications)
            {
                Process(output, specification);
            }

            output.Outdent();
            output.AppendLine("}");

            if (!swaggerConfig.DoNotWriteTargetFile)
            {
                File.WriteAllText(swaggerConfig.Target, output.ToString());
            }
        }

        private void AddAPICall(CoderStringBuilder output, PathItem path, HTTPAction action)
        {
            var operation = default(Operation);
            switch (action)
            {
                case HTTPAction.Put:
                    {
                        operation = path.Put;
                        break;
                    }
                case HTTPAction.Get:
                    {
                        operation = path.Get;
                        break;
                    }
                case HTTPAction.Post:
                    {
                        operation = path.Post;
                        break;
                    }
                case HTTPAction.Delete:
                    {
                        operation = path.Delete;
                        break;
                    }
                case HTTPAction.Head:
                    {
                        operation = path.Head;
                        break;
                    }
                case HTTPAction.Options:
                    {
                        operation = path.Options;
                        break;
                    }
                case HTTPAction.Patch:
                    {
                        operation = path.Patch;
                        break;
                    }
            }
            if (operation == null)
            {
                return;
            }

            var parameters = "";
            if (operation.Parameters != null)
            {
                var optional = "";
                if (operation.Parameters.All(_ => !_.Required))
                {
                    optional = "?";
                }

                parameters = $"parameters{optional}: {CleanClassName(operation.OperationId + "Request")}";
            }

            var methodName = "";
            if (operation.OperationId != null)
            {
                methodName = CleanClassName(operation.OperationId);
            }
            else
            {
                methodName = CleanClassName(GetPathToMethodName(action.ToString(), path.Path));
            }

            var operationContent = new StringBuilder(methodName + "(" + parameters + "): ");
            var success = operation.Responses.FirstOrDefault(_ => _.HttpStatusCode >= 200 && _.HttpStatusCode <= 299);
            if (success == null || success.Schema == null)
            {
                operationContent.Append("PromiseLike<void>;");
            }
            else
            {
                operationContent.Append($"PromiseLike<{SchemaTypeCleaner(success.Schema)}>;");
            }

            output.AppendLine(operationContent.ToString());
        }

        private void AddAPIRequest(CoderStringBuilder output, Operation operation)
        {
            if (operation == null)
            {
                return;
            }

            if (operation.Parameters != null)
            {
                AddParameterInterface(output, operation.OperationId + "Request", operation.Parameters);
            }
        }

        private void AddParameterInterface(CoderStringBuilder output, string sourceName, Parameter[] parameters)
        {
            var name = CleanClassName(sourceName);
            if (existingInterfaces.Contains(name))
            {
                return;
            }

            existingInterfaces.Add(name);

            output.AppendLine($"export interface {name} {{");
            output.Indent();
            foreach (var parameter in parameters)
            {
                var propertyName = parameter.Name;
                if (!parameter.Required)
                {
                    propertyName += "?";
                }

                var propertyType = "any";
                var bodyParameter = parameter as BodyParameter;
                if (bodyParameter != null)
                {
                    propertyType = SchemaTypeCleaner(bodyParameter.Schema);
                }

                var otherParameter = parameter as OtherParameter;
                if (otherParameter != null)
                {
                    var arrayParameter = parameter as OtherArrayParameter;
                    if (arrayParameter != null)
                    {
                        propertyType = $"Array<{CleanClassName(arrayParameter.Items[0].Type)}>";
                    }
                    else
                    {
                        propertyType = CleanClassName(otherParameter.Type);
                    }
                }

                output.AppendLine($"{propertyName}: {propertyType};");
            }

            output.Outdent();
            output.AppendLine("}");
            output.AppendLine();
        }

        private void AddTypes(CoderStringBuilder output, string sourceName, Property[] properties)
        {
            var enums = new List<EnumInfo>();
            var name = CleanClassName(sourceName);
            if (existingInterfaces.Contains(name))
            {
                return;
            }

            existingInterfaces.Add(name);

            output.AppendLine($"export interface {name} {{");
            output.Indent();
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    var propertyName = property.Name;
                    var propertyType = PropertyTypeCleaner(property);
                    if (property.Enum != null)
                    {
                        propertyType = "string";
                        enums.Add(new EnumInfo
                        {
                            EnumPropertyName = propertyName,
                            EnumValues = property.Enum,
                            EnumClassName = name + propertyName
                        });
                    }

                    output.AppendLine($"{propertyName}: {propertyType};");
                }
            }

            output.Outdent();
            output.AppendLine("}");
            output.AppendLine();

            foreach (var @enum in enums)
            {
                output.AppendLine($"export enum {@enum.EnumClassName} {{");
                output.Indent();
                var addComma = false;
                foreach (var enumValue in @enum.EnumValues)
                {
                    if (!addComma)
                    {
                        addComma = true;
                    }
                    else
                    {
                        output.AppendLine(",", true);
                    }

                    output.Append(enumValue);
                }
                output.AppendLine();
                output.Outdent();
                output.AppendLine("}");
                output.AppendLine();
            }
        }

        private string CamelCase(IEnumerable<string> segments)
        {
            if (segments.Count() == 1)
            {
                return segments.First();
            }

            return segments.Aggregate((curr, next) =>
            {
                if (curr.Length == 0)
                {
                    return next[0].ToString().ToLowerInvariant() + next.Substring(1);
                }

                return next[0].ToString().ToUpperInvariant() + next.Substring(1);
            });
        }

        private string CleanClassName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "";
            }

            var jsonType = whitespace.Replace(input.Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", ""), "");
            if (jsonType.Equals("object", StringComparison.OrdinalIgnoreCase))
            {
                return "any";
            }

            if (jsonType.Equals("integer", StringComparison.OrdinalIgnoreCase))
            {
                return "number";
            }

            return jsonType;
        }

        private string GetPathToMethodName(string method, string path)
        {
            if (path == "/" || path == "")
            {
                return method;
            }

            // clean url path for requests ending with "/"
            var cleanPath = path;
            if (cleanPath.IndexOf("/", cleanPath.Length - 1, StringComparison.OrdinalIgnoreCase) != -1)
            {
                cleanPath = cleanPath.Substring(0, cleanPath.Length - 1);
            }

            var segments = cleanPath.Split('/').Skip(1).Select(segment =>
            {
                if (segment[0] == '{' && segment[segment.Length - 1] == '}')
                {
                    return "by" + segment[1].ToString().ToUpperInvariant() + segment.Substring(2, segment.Length - 2);
                }

                return segment;
            });

            var result = CamelCase(segments);
            return method.ToLowerInvariant() + result[0].ToString().ToUpperInvariant() + result.Substring(1);
        }

        private void Process(CoderStringBuilder output, Specification specification)
        {
            output.AppendLine($"export module {CleanClassName(specification.Info.Title)} {{");
            output.Indent();
            foreach (var defination in specification.Definations)
            {
                AddTypes(output, defination.Name, defination.Properties);
            }

            foreach (var path in specification.Paths)
            {
                AddAPIRequest(output, path.Delete);
                AddAPIRequest(output, path.Get);
                AddAPIRequest(output, path.Head);
                AddAPIRequest(output, path.Options);
                AddAPIRequest(output, path.Patch);
                AddAPIRequest(output, path.Post);
                AddAPIRequest(output, path.Put);
            }

            output.AppendLine("export interface API {");
            output.Indent();
            output.AppendLine("setToken(value: string, headerOrQueryName: string, isQuery: boolean): void;");

            foreach (var path in specification.Paths)
            {
                AddAPICall(output, path, HTTPAction.Delete);
                AddAPICall(output, path, HTTPAction.Get);
                AddAPICall(output, path, HTTPAction.Head);
                AddAPICall(output, path, HTTPAction.Options);
                AddAPICall(output, path, HTTPAction.Patch);
                AddAPICall(output, path, HTTPAction.Post);
                AddAPICall(output, path, HTTPAction.Put);
            }

            output.Outdent();
            output.AppendLine("}");

            output.Outdent();
            output.AppendLine("}");
        }

        private string PropertyTypeCleaner(Property property)
        {
            if (!string.IsNullOrWhiteSpace(property.Ref))
            {
                return RefToClass(property.Ref);
            }

            if (property.Type.Equals("array", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(property.ArrayItemType))
                {
                    return "Array<any>";
                }

                if (property.ArrayItemType.Contains('/'))
                {
                    return $"Array<{RefToClass(property.ArrayItemType)}>";
                }

                return $"Array<{CleanClassName(property.ArrayItemType)}>";
            }

            return CleanClassName(property.Type);
        }

        private string RefToClass(string @ref) => CleanClassName(@ref.Substring(@ref.IndexOf("/", 2, StringComparison.Ordinal) + 1));

        private string SchemaTypeCleaner(Schema property)
        {
            if (!string.IsNullOrWhiteSpace(property.Ref))
            {
                return CleanClassName(RefToClass(property.Ref));
            }

            if (property.Type.Equals("array", StringComparison.OrdinalIgnoreCase))
            {
                var array = "Array<any>";
                if (property.Items != null && property.Items.Length == 1)
                {
                    array = $"Array<{CleanClassName(ItemTypeCleaner(property.Items[0]))}>";
                }

                return array;
            }

            return CleanClassName(property.Type);
        }

        private class EnumInfo
        {
            public string EnumClassName { get; set; }

            public string EnumPropertyName { get; set; }

            public string[] EnumValues { get; set; }
        }
    }
}