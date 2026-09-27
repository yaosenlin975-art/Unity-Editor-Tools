using System;

namespace Lin.Runtime.Attribute
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class GetInParentAttribute : System.Attribute { }
}
