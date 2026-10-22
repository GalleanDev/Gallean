namespace Gallean.Entities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class GlobalDataFileAttribute(string fileName) : Attribute
{
    public string FileName { get; } = fileName;
    public bool Index { get; init; }
}
