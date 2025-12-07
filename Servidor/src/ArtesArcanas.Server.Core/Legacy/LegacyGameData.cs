namespace ArtesArcanas.Server.Core.Legacy;

public sealed class LegacyGameData
{
    public LegacyGameData(
        LegacyAdministratorRegistry administrators,
        LegacyClanStorage clans,
        LegacyCastleStorage castles,
        LegacyPriceStorage prices,
        LegacyItemCatalog items,
        LegacyMonsterCatalog monsters,
        LegacyAnimationMap animationMap)
    {
        Administrators = administrators;
        Clans = clans;
        Castles = castles;
        Prices = prices;
        Items = items;
        Monsters = monsters;
        AnimationMap = animationMap;
    }

    public LegacyAdministratorRegistry Administrators { get; }
    public LegacyClanStorage Clans { get; }
    public LegacyCastleStorage Castles { get; }
    public LegacyPriceStorage Prices { get; }
    public LegacyItemCatalog Items { get; }
    public LegacyMonsterCatalog Monsters { get; }
    public LegacyAnimationMap AnimationMap { get; }
}
