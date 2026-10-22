namespace Gallean.Client;

internal static class StableHash
{
    public static int Combine(int a, int b)
    {
        unchecked
        {
            uint h = 2166136261u;
            h = (h ^ (uint)a) * 16777619u;
            h = (h ^ (uint)b) * 16777619u;

            h ^= h >> 15;
            h *= 2246822519u;
            h ^= h >> 13;
            h *= 3266489917u;
            h ^= h >> 16;

            return (int)(h & int.MaxValue);
        }
    }
}
