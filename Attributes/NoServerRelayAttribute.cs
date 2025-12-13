using System;

namespace KCM.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NoServerRelayAttribute : Attribute
    {
    }
}

