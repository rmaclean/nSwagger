namespace nSwagger
{
    using nSwagger.Attributes;

    public interface IJsonSchema
    {
        string CollectionFormat { get; set; }

        string Default { get; set; }

        string[] Enum { get; set; }

        bool? ExclusiveMaximum { get; set; }

        bool? ExclusiveMinimum { get; set; }

        string Format { get; set; }

        double? Maximum { get; set; }

        int? MaxItems { get; set; }

        int? MaxLength { get; set; }

        double? Minimum { get; set; }

        int? MinItems { get; set; }

        int? MinLength { get; set; }

        double? MultipleOf { get; set; }

        string Pattern { get; set; }

        string Type { get; set; }

        bool UniqueItems { get; set; }
    }

    public class ArrayItems : Item
    {
        public Item[] Items { get; set; }
    }

    public class BodyParameter : Parameter
    {
        [Required]
        public Schema Schema { get; set; }
    }

    public class Contact
    {
        public string Email { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }
    }

    public class Defination : Schema
    {
        public string Name { get; set; }
    }

    public class Example
    {
        public string MimeType { get; set; }

        public string Value { get; set; }
    }

    public class ExternalDocs
    {
        public string Description { get; set; }

        [Required]
        public string Url { get; set; }
    }

    public class Header : IJsonSchema
    {
        public string CollectionFormat { get; set; }

        public string Default { get; set; }

        public string Description { get; set; }

        public string[] Enum { get; set; }

        public bool? ExclusiveMaximum { get; set; }

        public bool? ExclusiveMinimum { get; set; }

        public string Format { get; set; }

        public string Key { get; set; }

        public double? Maximum { get; set; }

        public int? MaxItems { get; set; }

        public int? MaxLength { get; set; }

        public double? Minimum { get; set; }

        public int? MinItems { get; set; }

        public int? MinLength { get; set; }

        public double? MultipleOf { get; set; }

        public string Pattern { get; set; }

        [Required]
        public string Type { get; set; }

        public bool UniqueItems { get; set; }
    }

    public class HeaderArray : Header
    {
        [Required]
        public Item[] Items { get; set; }
    }

    public class Info
    {
        public Contact Contact { get; set; }

        public string Description { get; set; }

        public License License { get; set; }

        public string TermsOfService { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Version { get; set; }
    }

    public class Item : IJsonSchema
    {
        public string CollectionFormat { get; set; }

        public string Default { get; set; }

        public string[] Enum { get; set; }

        public bool? ExclusiveMaximum { get; set; }

        public bool? ExclusiveMinimum { get; set; }

        public string Format { get; set; }

        public double? Maximum { get; set; }

        public int? MaxItems { get; set; }

        public int? MaxLength { get; set; }

        public double? Minimum { get; set; }

        public int? MinItems { get; set; }

        public int? MinLength { get; set; }

        public double? MultipleOf { get; set; }

        public string Pattern { get; set; }

        public string Ref { get; internal set; }

        [Required]
        public string Type { get; set; }

        public bool UniqueItems { get; set; }
    }

    public class License
    {
        [Required]
        public string Name { get; set; }

        public string Url { get; set; }
    }

    public class Operation
    {
        public string[] Consumes { get; set; }

        public bool Deprecated { get; set; }

        public string Description { get; set; }

        public ExternalDocs ExternalDocs { get; set; }

        public string OperationId { get; set; }

        public Parameter[] Parameters { get; set; }

        public string[] Produces { get; set; }

        [Required]
        public Response[] Responses { get; set; }

        public string[] Schemes { get; set; }

        public SecurityRequirement[] Security { get; set; }

        public string Summary { get; set; }

        public string[] Tags { get; set; }
    }

    public class OtherArrayParameter : OtherParameter
    {
        [Required]
        public Item[] Items { get; set; }
    }

    public class OtherParameter : Parameter, IJsonSchema
    {
        public bool AllowEmptyValue { get; set; }

        public string CollectionFormat { get; set; }

        public string Default { get; set; }

        public string[] Enum { get; set; }

        public bool? ExclusiveMaximum { get; set; }

        public bool? ExclusiveMinimum { get; set; }

        public string Format { get; set; }

        public double? Maximum { get; set; }

        public int? MaxItems { get; set; }

        public int? MaxLength { get; set; }

        public double? Minimum { get; set; }

        public int? MinItems { get; set; }

        public int? MinLength { get; set; }

        public double? MultipleOf { get; set; }

        public string Pattern { get; set; }

        [Required]
        public string Type { get; set; }

        public bool UniqueItems { get; set; }
    }

    public class Parameter
    {
        public string Description { get; set; }

        [Required]
        public string In { get; set; }

        [Required]
        public string Name { get; set; }

        public bool Required { get; set; }
    }

    public class PathItem
    {
        public Operation Delete { get; set; }

        public Operation Get { get; set; }

        public Operation Head { get; set; }

        public Operation Options { get; set; }

        public Parameter[] Parameters { get; set; }

        public Operation Patch { get; set; }

        [Required]
        public string Path { get; set; }

        public Operation Post { get; set; }

        public Operation Put { get; set; }
    }

    public class Property : IJsonSchema
    {
        public string ArrayItemType { get; internal set; }

        public string CollectionFormat { get; set; }

        public string Default { get; set; }

        public string[] Enum { get; set; }

        public bool? ExclusiveMaximum { get; set; }

        public bool? ExclusiveMinimum { get; set; }

        public string Format { get; set; }

        public double? Maximum { get; set; }

        public int? MaxItems { get; set; }

        public int? MaxLength { get; set; }

        public double? Minimum { get; set; }

        public int? MinItems { get; set; }

        public int? MinLength { get; set; }

        public double? MultipleOf { get; set; }

        public string Name { get; set; }

        public string Pattern { get; set; }

        public string Ref { get; internal set; }

        public string Type { get; set; }

        public bool UniqueItems { get; set; }
    }

    public class Response
    {
        [Required]
        public string Description { get; set; }

        public Example[] Example { get; set; }

        public Header[] Headers { get; set; }

        public int HttpStatusCode { get; set; }

        public Schema Schema { get; set; }
    }

    public class Schema : IJsonSchema
    {
        public string CollectionFormat { get; set; }

        public string Default { get; set; }

        public string Description { get; set; }

        public string Discriminator { get; set; }

        public string[] Enum { get; set; }

        public string Example { get; set; }

        public bool? ExclusiveMaximum { get; set; }

        public bool? ExclusiveMinimum { get; set; }

        public ExternalDocs ExternalDocs { get; set; }

        public string Format { get; set; }

        public Item[] Items { get; internal set; }

        public string Key { get; set; }

        public double? Maximum { get; set; }

        public int? MaxItems { get; set; }

        public int? MaxLength { get; set; }

        public int? MaxProperties { get; set; }

        public double? Minimum { get; set; }

        public int? MinItems { get; set; }

        public int? MinLength { get; set; }

        public int? MinProperties { get; set; }

        public double? MultipleOf { get; set; }

        public string Pattern { get; set; }

        public Property[] Properties { get; internal set; }

        //todo: get from JSON schema - http://json-schema.org/
        //items
        //allOf
        //properties
        //additionaProperties
        public bool ReadOnly { get; set; }

        public string Ref { get; internal set; }

        public bool Required { get; set; }

        public string Title { get; set; }

        [Required]
        public string Type { get; set; }

        public bool UniqueItems { get; set; }

        public string XML { get; set; }
    }

    public class Scope
    {
        public string Name { get; set; }

        public string Value { get; set; }
    }

    public class SecurityDefination
    {
        public string AuthorizationUrl { get; set; }

        public string DefinationName { get; set; }

        public string Description { get; set; }

        public string Flow { get; set; }

        [Required]
        public string In { get; set; }

        [Required]
        public string Name { get; set; }

        public Scope[] Scopes { get; set; }

        public string TokenUrl { get; set; }

        [Required]
        public string Type { get; set; }
    }

    public class SecurityRequirement
    {
        public string Name { get; set; }

        public string[] Values { get; set; }
    }

    public class Specification
    {
        public string BasePath { get; set; }

        public string[] Consumes { get; set; }

        public Defination[] Definations { get; set; }

        public ExternalDocs ExternalDocs { get; set; }

        public string Host { get; set; }

        [Required]
        public Info Info { get; set; }

        public Parameter[] Parameters { get; internal set; }

        [Required]
        public PathItem[] Paths { get; set; }

        public string[] Produces { get; set; }

        public Response[] Responses { get; set; }

        public string[] Schemes { get; set; }

        public SecurityRequirement[] Security { get; set; }

        public SecurityDefination[] SecurityDefinations { get; set; }

        [Required]
        public string Swagger { get; set; }

        public Tag[] Tags { get; set; }
    }

    public class Tag
    {
        public string Description { get; set; }

        public ExternalDocs ExternalDocs { get; set; }

        [Required]
        public string Name { get; set; }
    }
}