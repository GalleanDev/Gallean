namespace Gallean.Entities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class CampaignDataFileAttribute(string fileName) : Attribute
{
    public string FileName { get; } = fileName;
}
