namespace Gallean.Client;

internal static class LeaderEquipmentSlotFormatter
{
    public static string FormatName(LeaderEquipmentSlot slot)
    {
        return slot switch
        {
            LeaderEquipmentSlot.Banner => Tr("Banner"),
            LeaderEquipmentSlot.Tome => Tr("Tome"),
            LeaderEquipmentSlot.Battle1 => Tr("Battle item I"),
            LeaderEquipmentSlot.Battle2 => Tr("Battle item II"),
            LeaderEquipmentSlot.Artifact1 => Tr("Artifact I"),
            LeaderEquipmentSlot.Artifact2 => Tr("Artifact II"),
            LeaderEquipmentSlot.Boots => Tr("Boots")
        };
    }
}
