using System;
using System.Diagnostics;

namespace FTRuntime
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class)]
    public class SwfSequenceIdDefAttribute : Attribute { }
}