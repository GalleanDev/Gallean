using System.Text.Json.Serialization;

namespace GameContent;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ImageSceneElement), "Image")]
[JsonDerivedType(typeof(ButtonSceneElement), "Button")]
[JsonDerivedType(typeof(TextBlockSceneElement), "TextBlock")]
[JsonDerivedType(typeof(TextListBoxSceneElement), "TextListBox")]
[JsonDerivedType(typeof(ToggleButtonSceneElement), "ToggleButton")]
public abstract record SceneElement
{
    public abstract SceneElementType Type { get; }
    public required string Name { get; init; }
    public Rectangle Position { get; init; }
}
