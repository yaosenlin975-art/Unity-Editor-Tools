using System;

namespace Lin.Runtime.Attribute
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class GetInChildAttribute : System.Attribute
    {
        public GetInChildAttribute() : this(string.Empty)
        {

        }

        public GetInChildAttribute(string path)
        {
            Path = path;
        }

        public string Path { get; }
    }
}
