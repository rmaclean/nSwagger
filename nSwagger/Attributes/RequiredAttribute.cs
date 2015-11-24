namespace nSwagger.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    internal sealed class RequiredAttribute : Attribute
    {
    }
}