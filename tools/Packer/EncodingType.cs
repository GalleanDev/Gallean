namespace Packer;

[Flags]
internal enum EncodingType
{
    None = 0,
    Palette = 1 << 0,
    Lossy = 1 << 1
}
