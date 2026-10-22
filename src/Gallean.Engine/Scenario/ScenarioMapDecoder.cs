using System.Buffers.Binary;

namespace Gallean.Engine;

internal static class ScenarioMapDecoder
{
    public readonly record struct TileInfo(int TerrainId, int GroundId, int ImageVariant);

    private const int BlockWidth = 8;
    private const int BlockHeight = 4;
    private const int TilesPerBlock = BlockWidth * BlockHeight;
    private const int BlockByteCount = TilesPerBlock * sizeof(int);

    public static TileInfo[] DecodeMap(int size, IEnumerable<Db.ScenarioMapBlock> blocksList)
    {
        TileInfo[] map = new TileInfo[size * size];

        foreach ((Db.ScenarioId _, Db.Id blockId, string blockData) in blocksList)
        {
            if (blockId.Value.Length >= 10)
            {
                string yyStr = blockId.Value[6..8];
                string xxStr = blockId.Value[8..10];

                int yPos = Convert.ToInt32(yyStr, 16);
                int xPos = Convert.ToInt32(xxStr, 16);

                int[] tiles = DecodeBlock(blockData);

                for (int by = 0; by < BlockHeight; by++)
                {
                    for (int bx = 0; bx < BlockWidth; bx++)
                    {
                        int mapX = xPos + bx;
                        int mapY = yPos + by;

                        if (mapX < size && mapY < size)
                        {
                            map[mapY * size + mapX] = ParseTileInfo(tiles[by * BlockWidth + bx]);
                        }
                    }
                }
            }
        }

        return map;
    }

    private static int[] DecodeBlock(string data)
    {
        byte[] bytes = SixbitTextDecoder.Decode(data);

        if (bytes.Length < BlockByteCount)
        {
            throw new InvalidDataException($"Map block data holds {bytes.Length} bytes instead of {BlockByteCount}.");
        }

        int[] tiles = new int[TilesPerBlock];

        for (int i = 0; i < TilesPerBlock; i++)
        {
            tiles[i] = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(i * sizeof(int)));
        }

        return tiles;
    }

    private static TileInfo ParseTileInfo(int value)
    {
        int terrainId = (value >> 0) & 0b111;
        int groundId = (value >> 3) & 0b11111;
        int imageVariant = (value >> 26) & 0b111111;

        return new TileInfo(terrainId, groundId, imageVariant);
    }
}
