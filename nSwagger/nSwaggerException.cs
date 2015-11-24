
namespace nSwagger
{
    using System;
    [Serializable]
    public class nSwaggerException : Exception
    {
     
        public nSwaggerException(string message) : base(message)
        {
        }

        public nSwaggerException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}