using System.Diagnostics;

namespace SimpleFluidd.Configuration;

[AttributeUsage(AttributeTargets.Property)]
[DebuggerDisplay("{VarableName}, Required: {Required}")]
public sealed class EnvironmentVarAttribute(string variableName, bool required = false) : Attribute
{
    public string VariableName { get; } = variableName;

    public bool Required { get; } = required;
}
