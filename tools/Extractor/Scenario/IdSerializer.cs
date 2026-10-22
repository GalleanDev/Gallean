namespace Extractor;

internal static class IdSerializer
{
    private readonly record struct Field(int Offset, int Width)
    {
        private int Maximum => (1 << Width) - 1;

        public int Read(uint value)
        {
            return (int)(value >> Offset) & Maximum;
        }

        public uint Write(int field)
        {
            return (uint)field << Offset;
        }

        public bool Fits(int field)
        {
            return field >= 0 && field <= Maximum;
        }
    }

    private static readonly Field Number = new(Offset: 0, Width: 16);
    private static readonly Field Type = new(Offset: 16, Width: 6);
    private static readonly Field CategoryIndex = new(Offset: 22, Width: 8);
    private static readonly Field Category = new(Offset: 30, Width: 2);

    private static readonly char[] CategoryCodes = ['G', 'C', 'S', 'E'];

    private static readonly string[] TypeCodes =
    [
        "TA", "BB", "RR", "LR", "SS", "UU", "UG", "UM", "AA", "TG",
        "MG", "IG", "NA", "DU", "DA", "AL", "DC", "AC", "CC", "CW",
        "CO", "PN", "OB", "SC", "MP", "MB", "IF", "ET", "FT", "PL",
        "KS", "FG", "PB", "RA", "KC", "UN", "MM", "IM", "BG", "SI",
        "RU", "TB", "RD", "CR", "DP", "ST", "LO", "TM", "EV", "SD",
        "TC", "MT", "ML", "SR", "BR", "QL", "TS", "SV"
    ];

    public static Db.Id Deserialize(uint value)
    {
        int type = Type.Read(value);

        if (type < 1 || type > TypeCodes.Length)
        {
            throw new InvalidDataException($"Serialized ID {value} names unknown type {type}.");
        }

        return new Db.Id(
            $"{CategoryCodes[Category.Read(value)]}" +
            $"{CategoryIndex.Read(value):D3}" +
            $"{TypeCodes[type - 1]}" +
            $"{Number.Read(value):X4}");
    }

    public static uint Serialize(Db.Id id)
    {
        int type = Array.IndexOf(TypeCodes, id.TypeCode) + 1;

        if (type < 1)
        {
            throw new InvalidDataException($"ID '{id}' names unknown type '{id.TypeCode}'.");
        }

        int category = Array.IndexOf(CategoryCodes, char.ToUpperInvariant(id.Value[0]));

        if (category < 0)
        {
            throw new InvalidDataException($"ID '{id}' names unknown category '{id.Value[0]}'.");
        }

        if (!CategoryIndex.Fits(id.CategoryIndex))
        {
            throw new InvalidDataException($"ID '{id}' has a category index that does not fit the serialized form.");
        }

        if (!Number.Fits(id.TypeIndex))
        {
            throw new InvalidDataException($"ID '{id}' has a number that does not fit the serialized form.");
        }

        return Category.Write(category) |
               CategoryIndex.Write(id.CategoryIndex) |
               Type.Write(type) |
               Number.Write(id.TypeIndex);
    }
}
