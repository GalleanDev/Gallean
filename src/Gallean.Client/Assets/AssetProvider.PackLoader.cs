using Gallean.Configuration;

namespace Gallean.Client;

internal sealed partial class AssetProvider
{
    private static class PackLoader
    {
        public static IReadOnlyList<AssetPack>? Load()
        {
            return LoadFrom(Path.Combine(AppContext.BaseDirectory, "Assets")) ??
                   LoadFrom(AppContext.BaseDirectory) ??
                   LoadFrom(AppDirectories.Assets);
        }

        private static IReadOnlyList<AssetPack>? LoadFrom(string directory)
        {
            List<AssetPack> packs = ReadPacks(directory);
            List<AssetPack> selectedPacks = SelectPacks(packs);

            foreach (AssetPack pack in packs)
            {
                if (!selectedPacks.Contains(pack))
                {
                    pack.Dispose();
                }
            }

            if (selectedPacks.Count > 0)
            {
                return selectedPacks;
            }
            else
            {
                return null;
            }
        }

        private static List<AssetPack> ReadPacks(string directory)
        {
            List<AssetPack> packs = [];
            try
            {
                foreach (string path in EnumeratePaths(directory))
                {
                    packs.Add(ReadPack(path));
                }
            }
            catch
            {
                foreach (AssetPack pack in packs)
                {
                    pack.Dispose();
                }

                throw;
            }

            return [.. packs.OrderBy(static pack => pack.Manifest.CreatedAt)];
        }

        private static AssetPack ReadPack(string path)
        {
            try
            {
                return AssetPack.Read(path);
            }
            catch (AssetPackUnsupportedFormatException ex)
            {
                throw new InstalledAssetPackIncompatibleException(path, ex);
            }
        }

        private static List<AssetPack> SelectPacks(IReadOnlyList<AssetPack> packs)
        {
            AssetPack? basePack = null;
            List<AssetPack> overlayPacks = [];

            foreach (AssetPack pack in packs)
            {
                if (pack.Manifest.Sparse)
                {
                    overlayPacks.Add(pack);
                }
                else
                {
                    basePack = pack;
                }
            }

            if (basePack is not null)
            {
                return [basePack, .. overlayPacks];
            }
            else
            {
                return [];
            }
        }

        private static IEnumerable<string> EnumeratePaths(string directory)
        {
            if (Directory.Exists(directory))
            {
                return Directory.EnumerateFiles(directory, "*.galleanpack");
            }
            else
            {
                return [];
            }
        }
    }
}
