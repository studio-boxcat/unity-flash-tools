using System;
using System.Diagnostics;

namespace FT
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class)]
    public class SwfSequenceIdDefAttribute : Attribute { }
}