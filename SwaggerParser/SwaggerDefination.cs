//http://swagger.io/specification/
namespace SwaggerParser
{
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Linq;

    internal class SwaggerContact
    {
        private SwaggerContact()
        {
        }

        public IEnumerable<string> Email { get; private set; }

        public IEnumerable<string> Name { get; private set; }

        public IEnumerable<string> Url { get; private set; }

        public static SwaggerContact FromNode(JObject contact)
        {
            if (contact == null)
            {
                return null;
            }

            var result = new SwaggerContact
            {
                Name = contact["name"]?.Values<string>(),
                Url = contact["url"]?.Values<string>(),
                Email = contact["email"]?.Values<string>(),
            };

            return result;
        }
    }

    internal class SwaggerDefination2
    {
        private SwaggerDefination2()
        {
        }

        public IEnumerable<string> Consumes { get; private set; }

        public string Host { get; private set; }

        public SwaggerInfo Info { get; private set; }

        public object Paths { get; private set; }

        public IEnumerable<string> Produces { get; private set; }

        public IEnumerable<string> Schemes { get; private set; }

        public string SwaggerVersion { get; private set; }

        //todo: paths
        //todo: definations
        //todo: parameters
        //todo: responses
        //todo: security
        //todo: securityDefinations
        //todo: tags
        //todo: externalDocs

        public static SwaggerDefination2 FromNode(JObject root)
        {
            var result = new SwaggerDefination2
            {
                SwaggerVersion = root["swagger"].Value<string>(),
                Host = root["host"]?.Value<string>(),
                Schemes = root["schemes"]?.Values<string>(),
                Consumes = root["consumes"]?.Values<string>(),
                Produces = root["produces"]?.Values<string>(),
                Info = SwaggerInfo.FromNode(root["info"] as JObject),
                Paths = root["paths"].AsJEnumerable().Select(_ => SwaggerPath.FromNode(_)).ToArray()
            };

            return result;
        }
    }

    internal class SwaggerExternalDocumentation
    {
        private SwaggerExternalDocumentation()
        {
        }

        public string Description { get; private set; }

        public string Url { get; private set; }

        public static SwaggerExternalDocumentation FromNode(JToken node)
        {
            var nodeValue = node.First();
            var result = new SwaggerExternalDocumentation
            {
                Description = nodeValue["description"]?.Value<string>(),
                Url = nodeValue["url"]?.Value<string>(),
            };

            return result;
        }
    }

    internal class SwaggerInfo
    {
        private SwaggerInfo()
        {
        }

        public SwaggerContact Contact { get; private set; }

        public string Description { get; private set; }

        public SwaggerLicense License { get; private set; }

        public string TermsOfService { get; private set; }

        public string Title { get; private set; }

        public string Version { get; private set; }

        public static SwaggerInfo FromNode(JObject info)
        {
            var result = new SwaggerInfo
            {
                Title = info["title"].Value<string>(),
                Description = info["description"]?.Value<string>(),
                TermsOfService = info["termsOfService"]?.Value<string>(),
                Version = info["version"].Value<string>(),
                Contact = SwaggerContact.FromNode(info["contact"] as JObject),
                License = SwaggerLicense.FromNode(info["license"] as JObject)
            };

            return result;
        }
    }

    internal class SwaggerLicense
    {
        private SwaggerLicense()
        {
        }

        public IEnumerable<string> Name { get; private set; }

        public IEnumerable<string> Url { get; private set; }

        public static SwaggerLicense FromNode(JObject contact)
        {
            if (contact == null)
            {
                return null;
            }

            var result = new SwaggerLicense
            {
                Name = contact["name"].Values<string>(),
                Url = contact["url"]?.Values<string>(),
            };

            return result;
        }
    }

    internal class SwaggerOperation
    {
        private SwaggerOperation()
        {
        }

        public IEnumerable<string> Consumes { get; private set; }

        public bool? Deprecated { get; private set; }

        public string Description { get; private set; }

        public string OperationId { get; private set; }

        public IEnumerable<string> Produces { get; private set; }

        public IEnumerable<string> Schemes { get; private set; }

        public string Summary { get; private set; }

        public IEnumerable<string> Tags { get; private set; }

        public static SwaggerOperation FromNode(JToken node)
        {
            var nodeValue = node.First();
            var result = new SwaggerOperation
            {
                Tags = nodeValue["tags"]?.Values<string>(),
                Summary = nodeValue["summary"]?.Value<string>(),
                Description = nodeValue["description"]?.Value<string>(),
                //todo: external docs
                OperationId = nodeValue["operationId"]?.Value<string>(),
                Consumes = nodeValue["consumes"]?.Values<string>(),
                Produces = nodeValue["produces"]?.Values<string>(),
                //todo: parameter
                //todo: responses
                Schemes = nodeValue["schemes"]?.Values<string>(),
                Deprecated = nodeValue["deprecated"]?.Value<bool>(),
                //todo: security
            };

            return result;
        }
    }

    internal class SwaggerPath
    {
        private SwaggerPath()
        {
        }

        public string Path { get; private set; }

        public static SwaggerPath FromNode(JToken node)
        {
            var nodeValue = node.First();
            var result = new SwaggerPath
            {
                Path = (node as JProperty).Name
            };
            //todo:get
            //todo:post
            //todo:put
            //todo:delete
            //todo:options
            //todo:head
            //todo:patch
            //todo:parameters

            return result;
        }
    }
}