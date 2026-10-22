namespace Gallean.Client;

internal sealed class SoundDecodeException(AssetSoundKey key, Exception innerException)
    : Exception($"Failed to decode sound '{key.CanonicalId}'.", innerException);
