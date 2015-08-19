namespace SwaggerParser
{
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

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

        public IEnumerable<string> Produces { get; private set; }

        public IEnumerable<string> Schemes { get; private set; }

        public string SwaggerVersion { get; private set; }

        public static SwaggerDefination2 FromNode(JObject root)
        {
            var result = new SwaggerDefination2
            {
                SwaggerVersion = root["swagger"].Value<string>(),
                Host = root["host"]?.Value<string>(),
                Schemes = root["schemes"]?.Values<string>(),
                Consumes = root["consumes"]?.Values<string>(),
                Produces = root["produces"]?.Values<string>(),
            };

            return result;
        }

        //todo: info
    }

    internal class SwaggerInfo
    {
        private SwaggerInfo()
        {
        }

        public SwaggerContact Contact { get; private set; }

        public string Description { get; private set; }

        public string TermsOfService { get; private set; }

        public string Title { get; private set; }

        public string Version { get; private set; }

        //todo: license element

        public static SwaggerInfo FromNode(JObject info)
        {
            var result = new SwaggerInfo
            {
                Title = info["title"].Value<string>(),
                Description = info["description"]?.Value<string>(),
                TermsOfService = info["termsOfService"]?.Value<string>(),
                Version = info["version"].Value<string>(),
                Contact = SwaggerContact.FromNode(info["contact"] as JObject),
            };

            return result;
        }
    }
}