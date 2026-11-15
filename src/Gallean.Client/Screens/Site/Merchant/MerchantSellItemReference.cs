namespace Gallean.Client;

internal readonly record struct MerchantSellItemReference(ItemDefinitionId ItemId, int? Charges) : IDragSourceReference;
