using System.Buffers.Binary;
using System.Text.Json;
using TorchSharp;

namespace Umbra;

internal static class SafeTensors
{
    private readonly record struct Entry(string Name, torch.ScalarType Type, long[] Shape, long Begin, long End);

    private readonly record struct Header(long DataStart, IReadOnlyList<Entry> Entries);

    private const string MetadataName = "__metadata__";
    private const string DataTypeName = "dtype";
    private const string ShapeName = "shape";
    private const string OffsetsName = "data_offsets";

    private const int HeaderAlignment = 8;
    private const byte HeaderPadding = (byte)' ';

    public static Dictionary<string, torch.Tensor> LoadStateDict(string path)
    {
        using FileStream stream = File.OpenRead(path);

        return LoadStateDict(stream);
    }

    public static Dictionary<string, torch.Tensor> LoadStateDict(Stream stream)
    {
        Header header = ReadHeader(stream);
        Dictionary<string, torch.Tensor> state = [];

        foreach (Entry entry in header.Entries)
        {
            state[entry.Name] = ReadTensor(stream, header.DataStart, entry);
        }

        return state;
    }

    public static Dictionary<string, torch.Tensor> MapStateDict(string path)
    {
        Header header;

        using (FileStream stream = File.OpenRead(path))
        {
            header = ReadHeader(stream);
        }

        Dictionary<torch.ScalarType, torch.Tensor> mappings = [];
        Dictionary<string, torch.Tensor> state = [];

        try
        {
            foreach (Entry entry in header.Entries)
            {
                if (!mappings.TryGetValue(entry.Type, out torch.Tensor? mapping))
                {
                    mapping = MapFile(path, entry.Type);
                    mappings[entry.Type] = mapping;
                }

                state[entry.Name] = MapTensor(mapping, header.DataStart, entry);
            }
        }
        finally
        {
            foreach (torch.Tensor mapping in mappings.Values)
            {
                mapping.Dispose();
            }
        }

        return state;
    }

    public static void SaveStateDict(string path, IReadOnlyDictionary<string, torch.Tensor> state)
    {
        byte[] header = BuildHeader(state);

        using FileStream stream = File.Create(path);

        byte[] length = new byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(length, header.Length);

        stream.Write(length);
        stream.Write(header);

        foreach (KeyValuePair<string, torch.Tensor> parameter in state)
        {
            torch.Tensor tensor = parameter.Value.contiguous();

            tensor.WriteBytesToStream(stream);
        }
    }

    private static int GetElementSize(torch.ScalarType type)
    {
        return type switch
        {
            torch.ScalarType.Float32 => 4,
            torch.ScalarType.Float16 => 2,
            torch.ScalarType.BFloat16 => 2,
            torch.ScalarType.Int64 => 8,
            _ => throw new NotSupportedException($"The tensor type '{type}' cannot be mapped.")
        };
    }

    private static torch.Tensor MapFile(string path, torch.ScalarType type)
    {
        FileInfo info = new(path);

        FileInfo? target = info.ResolveLinkTarget(returnFinalTarget: true) as FileInfo;

        if (target is null)
        {
            target = info;
        }

        return torch.from_file(
            path, shared: true, size: target.Length / GetElementSize(type), dtype: type, device: torch.CPU);
    }

    private static torch.Tensor MapTensor(torch.Tensor mapping, long dataStart, Entry entry)
    {
        long elementSize = GetElementSize(entry.Type);
        long offset = dataStart + entry.Begin;

        if (offset % elementSize != 0)
        {
            throw new InvalidDataException($"The tensor '{entry.Name}' is not aligned to its element size.");
        }

        torch.Tensor region = mapping.narrow(0, offset / elementSize, (entry.End - entry.Begin) / elementSize);

        return region.view(entry.Shape);
    }

    private static torch.Tensor ReadTensor(Stream stream, long dataStart, Entry entry)
    {
        torch.Tensor tensor = torch.empty(entry.Shape, entry.Type, torch.CPU);

        stream.Seek(dataStart + entry.Begin, SeekOrigin.Begin);
        tensor.ReadBytesFromStream(stream);

        return tensor;
    }

    private static Header ReadHeader(Stream stream)
    {
        byte[] length = new byte[sizeof(long)];
        stream.ReadExactly(length);

        byte[] contents = new byte[BinaryPrimitives.ReadInt64LittleEndian(length)];
        stream.ReadExactly(contents);

        long dataStart = stream.Position;
        using JsonDocument document = JsonDocument.Parse(contents);
        List<Entry> entries = [];

        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            if (property.Name != MetadataName)
            {
                JsonElement offsets = property.Value.GetProperty(OffsetsName);

                entries.Add(new Entry(
                    property.Name,
                    GetScalarType(property.Value.GetProperty(DataTypeName)),
                    ReadShape(property.Value.GetProperty(ShapeName)),
                    offsets[0].GetInt64(),
                    offsets[1].GetInt64()));
            }
        }

        return new Header(dataStart, entries);
    }

    private static long[] ReadShape(JsonElement shape)
    {
        long[] dimensions = new long[shape.GetArrayLength()];
        int index = 0;

        foreach (JsonElement dimension in shape.EnumerateArray())
        {
            dimensions[index] = dimension.GetInt64();
            index++;
        }

        return dimensions;
    }

    private static byte[] BuildHeader(IReadOnlyDictionary<string, torch.Tensor> state)
    {
        using MemoryStream buffer = new();

        using (Utf8JsonWriter writer = new(buffer))
        {
            long offset = 0;

            writer.WriteStartObject();

            foreach (KeyValuePair<string, torch.Tensor> parameter in state)
            {
                torch.Tensor tensor = parameter.Value;
                long size = tensor.NumberOfElements * tensor.ElementSize;

                writer.WriteStartObject(parameter.Key);
                writer.WriteString(DataTypeName, GetDataTypeName(tensor.dtype));

                writer.WriteStartArray(ShapeName);

                foreach (long dimension in tensor.shape)
                {
                    writer.WriteNumberValue(dimension);
                }

                writer.WriteEndArray();

                writer.WriteStartArray(OffsetsName);
                writer.WriteNumberValue(offset);
                writer.WriteNumberValue(offset + size);
                writer.WriteEndArray();

                writer.WriteEndObject();

                offset += size;
            }

            writer.WriteEndObject();
        }

        return AlignHeader(buffer.ToArray());
    }

    private static byte[] AlignHeader(byte[] header)
    {
        int length = (header.Length + HeaderAlignment - 1) / HeaderAlignment * HeaderAlignment;
        byte[] aligned = new byte[length];

        aligned.AsSpan().Fill(HeaderPadding);
        header.CopyTo(aligned, 0);

        return aligned;
    }

    private static torch.ScalarType GetScalarType(JsonElement dataType)
    {
        string? name = dataType.GetString();

        return name switch
        {
            "F64" => torch.ScalarType.Float64,
            "F32" => torch.ScalarType.Float32,
            "F16" => torch.ScalarType.Float16,
            "BF16" => torch.ScalarType.BFloat16,
            "I64" => torch.ScalarType.Int64,
            "I32" => torch.ScalarType.Int32,
            "I16" => torch.ScalarType.Int16,
            "I8" => torch.ScalarType.Int8,
            "U8" => torch.ScalarType.Byte,
            "BOOL" => torch.ScalarType.Bool,
            _ => throw new NotSupportedException($"The data type '{name}' is not supported.")
        };
    }

    private static string GetDataTypeName(torch.ScalarType type)
    {
        return type switch
        {
            torch.ScalarType.Float64 => "F64",
            torch.ScalarType.Float32 => "F32",
            torch.ScalarType.Float16 => "F16",
            torch.ScalarType.BFloat16 => "BF16",
            torch.ScalarType.Int64 => "I64",
            torch.ScalarType.Int32 => "I32",
            torch.ScalarType.Int16 => "I16",
            torch.ScalarType.Int8 => "I8",
            torch.ScalarType.Byte => "U8",
            torch.ScalarType.Bool => "BOOL",
            _ => throw new NotSupportedException($"The data type '{type}' is not supported.")
        };
    }
}
