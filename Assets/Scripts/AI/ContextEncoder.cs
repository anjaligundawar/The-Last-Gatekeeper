namespace Gatekeeper.AI
{
    /// <summary>
    /// Turns a <see cref="DefenseSnapshot"/> into the context vector x the bandit learns from.
    ///
    /// Layout (d = 34):
    ///   [0..26]  spot x tower one-hot   (index = spot * 3 + towerType)
    ///   [27..29] weapon one-hot         (Bow, Sword, Spear)
    ///   [30..32] hero lane time         (Left, Middle, Right fractions)
    ///   [33]     bias                   (always 1)
    ///
    /// Feature engineering is a big lever on how well LinUCB learns. Ideas to try:
    /// per-lane counts, "lane has no anti-air" flags, or weapon x lane interactions.
    /// If you add features, bump <see cref="Dimensions"/> and <see cref="FeatureName"/>.
    /// </summary>
    public static class ContextEncoder
    {
        public const int SpotFeatureCount = MapConstants.SpotCount * MapConstants.TowerTypeCount;
        public const int WeaponOffset = SpotFeatureCount;
        public const int HeroLaneOffset = WeaponOffset + MapConstants.WeaponTypeCount;
        public const int BiasIndex = HeroLaneOffset + MapConstants.LaneCount;
        public const int Dimensions = BiasIndex + 1;

        public static int SpotFeatureIndex(int spot, TowerType type) => spot * MapConstants.TowerTypeCount + (int)type;

        public static float[] Encode(DefenseSnapshot snapshot)
        {
            var x = new float[Dimensions];

            for (int spot = 0; spot < MapConstants.SpotCount; spot++)
            {
                var tower = snapshot.Spots[spot];
                if (tower.HasValue) x[SpotFeatureIndex(spot, tower.Value)] = 1f;
            }

            x[WeaponOffset + (int)snapshot.Weapon] = 1f;

            for (int lane = 0; lane < MapConstants.LaneCount; lane++)
                x[HeroLaneOffset + lane] = snapshot.HeroLaneTime[lane];

            x[BiasIndex] = 1f;
            return x;
        }

        public enum FeatureKind { Spot, Weapon, HeroLane, Bias }

        public static FeatureKind KindOf(int index)
        {
            if (index < WeaponOffset) return FeatureKind.Spot;
            if (index < HeroLaneOffset) return FeatureKind.Weapon;
            if (index < BiasIndex) return FeatureKind.HeroLane;
            return FeatureKind.Bias;
        }

        public static string FeatureName(int index)
        {
            switch (KindOf(index))
            {
                case FeatureKind.Spot:
                    int spot = index / MapConstants.TowerTypeCount;
                    var tower = (TowerType)(index % MapConstants.TowerTypeCount);
                    return tower + " @ " + SpotName(spot);
                case FeatureKind.Weapon:
                    return "Weapon: " + (WeaponType)(index - WeaponOffset);
                case FeatureKind.HeroLane:
                    return "Hero time: " + MapConstants.LaneNames[index - HeroLaneOffset];
                default:
                    return "Bias";
            }
        }

        public static string SpotName(int spot) =>
            MapConstants.LaneNames[MapConstants.LaneOfSpot(spot)] + " " + (MapConstants.SlotInLane(spot) + 1);
    }
}
