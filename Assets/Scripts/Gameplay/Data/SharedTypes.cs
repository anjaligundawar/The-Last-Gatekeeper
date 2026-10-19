namespace Gatekeeper
{
    /// <summary>Defences the player can place on a build spot.</summary>
    public enum TowerType
    {
        Archer = 0,
        Cannon = 1,
        Frost = 2,
    }

    /// <summary>Weapons the hero can pick before each wave.</summary>
    public enum WeaponType
    {
        Bow = 0,
        Sword = 1,
        Spear = 2,
    }

    public static class MapConstants
    {
        public const int LaneCount = 3;
        public const int SpotsPerLane = 3;
        public const int SpotCount = LaneCount * SpotsPerLane;
        public const int TowerTypeCount = 3;
        public const int WeaponTypeCount = 3;

        public static readonly string[] LaneNames = { "Left", "Middle", "Right" };

        public static int LaneOfSpot(int spot) => spot / SpotsPerLane;
        public static int SlotInLane(int spot) => spot % SpotsPerLane;
    }
}
