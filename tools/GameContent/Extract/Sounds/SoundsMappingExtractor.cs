namespace GameContent;

public sealed class SoundsMappingExtractor
{
    private const int FileTableRecordId = 2;
    private const int ListNameLength = 33;

    private readonly IReadOnlyDictionary<string, UnitTypeSounds> unitTypeSounds;

    private SoundsMappingExtractor(IReadOnlyDictionary<string, UnitTypeSounds> unitTypeSounds)
    {
        this.unitTypeSounds = unitTypeSounds;
    }

    public static SoundsMappingExtractor Load(string path)
    {
        MqdbResourceArchive archive = MqdbResourceArchive.Load(path, FileTableRecordId, MqdbFileTableLayout.NameId);

        using FileStream stream = archive.OpenArchive();

        IReadOnlyList<SoundInfo> soundInfoList = ReadSoundInfoList(archive, stream);

        return new SoundsMappingExtractor(ReadUnitTypeSounds(archive, stream, soundInfoList));
    }

    public IReadOnlyDictionary<string, UnitTypeSounds> GetAllUnitTypeSounds()
    {
        return unitTypeSounds;
    }

    private static string FormatUnitTypeId(int unitTypeId)
    {
        return $"G000UU{unitTypeId & 0xffff:X4}";
    }

    private static IReadOnlyList<SoundInfo> ReadSoundInfoList(MqdbResourceArchive archive, Stream stream)
    {
        MqdbResourceFile file = archive.GetFile("SNDINFO.DAT");

        stream.Seek(file.Offset, SeekOrigin.Begin);

        int count = stream.ReadInt32LittleEndian();
        List<SoundInfo> result = new(count);

        for (int i = 0; i < count; i++)
        {
            int unitTypeId = stream.ReadInt32LittleEndian();

            string attackListName = stream.ReadAsciiString(ListNameLength);
            stream.Skip(3);
            int beginAttackSoundFrameIndex = stream.ReadInt32LittleEndian();
            int endAttackSoundFrameIndex = stream.ReadInt32LittleEndian();

            string hitListName = stream.ReadAsciiString(ListNameLength);
            stream.Skip(3);
            int beginAttackHitSoundFrameIndex = stream.ReadInt32LittleEndian();
            int endAttackHitSoundFrameIndex = stream.ReadInt32LittleEndian();

            string touchListName = stream.ReadAsciiString(ListNameLength);
            string damagedHitListName = stream.ReadAsciiString(ListNameLength);
            string walkListName = stream.ReadAsciiString(ListNameLength);

            stream.Skip(1);

            result.Add(new SoundInfo
            {
                UnitTypeId = unitTypeId,
                AttackListName = attackListName,
                BeginAttackSoundFrameIndex = beginAttackSoundFrameIndex,
                EndAttackSoundFrameIndex = endAttackSoundFrameIndex,
                HitListName = hitListName,
                BeginAttackHitSoundFrameIndex = beginAttackHitSoundFrameIndex,
                EndAttackHitSoundFrameIndex = endAttackHitSoundFrameIndex,
                TouchListName = touchListName,
                DamagedListName = damagedHitListName,
                WalkListName = walkListName
            });
        }

        return result;
    }

    private static IReadOnlyDictionary<string, UnitTypeSounds> ReadUnitTypeSounds(
        MqdbResourceArchive archive,
        Stream stream,
        IReadOnlyList<SoundInfo> soundInfoList)
    {
        Dictionary<string, IReadOnlyList<string>> soundListsByName = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, UnitTypeSounds> unitTypeSounds = [];

        foreach (SoundInfo info in soundInfoList)
        {
            UnitTypeSounds sounds = new()
            {
                UnitTypeId = FormatUnitTypeId(info.UnitTypeId),
                AttackSounds = ResolveSoundList(info.AttackListName),
                BeginAttackSoundFrameIndex = info.BeginAttackSoundFrameIndex,
                EndAttackSoundFrameIndex = info.EndAttackSoundFrameIndex,
                HitSounds = ResolveSoundList(info.HitListName),
                BeginAttackHitSoundFrameIndex = info.BeginAttackHitSoundFrameIndex,
                EndAttackHitSoundFrameIndex = info.EndAttackHitSoundFrameIndex,
                TouchSounds = ResolveSoundList(info.TouchListName),
                DamagedSounds = ResolveSoundList(info.DamagedListName),
                WalkSounds = ResolveSoundList(info.WalkListName)
            };

            unitTypeSounds.Add(sounds.UnitTypeId, sounds);
        }

        return unitTypeSounds;

        IReadOnlyList<string> ResolveSoundList(string soundName)
        {
            if (string.IsNullOrEmpty(soundName))
            {
                return [];
            }

            string extension = Path.GetExtension(soundName);

            if (extension.Equals(".LST", StringComparison.OrdinalIgnoreCase))
            {
                if (!soundListsByName.TryGetValue(soundName, out IReadOnlyList<string>? soundList))
                {
                    soundList = ReadSoundList(archive, stream, soundName);
                    soundListsByName.Add(soundName, soundList);
                }

                return soundList;
            }
            else
            {
                return [soundName];
            }
        }
    }

    private static IReadOnlyList<string> ReadSoundList(MqdbResourceArchive archive, Stream stream, string fileName)
    {
        MqdbResourceFile? file = archive.TryGetFile(fileName);
        if (file is null)
        {
            return [];
        }

        stream.Seek(file.Offset, SeekOrigin.Begin);
        stream.ReadAsciiString();
        stream.Skip(4);

        List<string> sounds = [];

        while (true)
        {
            string soundName = stream.ReadAsciiString();
            if (string.IsNullOrEmpty(soundName))
            {
                break;
            }

            sounds.Add(soundName);
        }

        return sounds;
    }
}
