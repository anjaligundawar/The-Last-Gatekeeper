namespace Gatekeeper.AI
{
    /// <summary>
    /// Everything the War Council is allowed to see about the player before a wave.
    /// Plain data so it can be built by the game and unit tested without Unity.
    /// </summary>
    public sealed class DefenseSnapshot
    {
        /// <summary>Tower on each build spot, or null when the spot is empty. Index = lane * 3 + slot.</summary>
        public readonly TowerType?[] Spots = new TowerType?[MapConstants.SpotCount];

        public WeaponType Weapon;

        /// <summary>Fraction of last wave the hero spent in each lane (sums to 1, or all zero on wave 1).</summary>
        public readonly float[] HeroLaneTime = new float[MapConstants.LaneCount];

        public int CountInLane(int lane)
        {
            int count = 0;
            for (int slot = 0; slot < MapConstants.SpotsPerLane; slot++)
                if (Spots[lane * MapConstants.SpotsPerLane + slot].HasValue) count++;
            return count;
        }

        public int CountInLane(int lane, TowerType type)
        {
            int count = 0;
            for (int slot = 0; slot < MapConstants.SpotsPerLane; slot++)
                if (Spots[lane * MapConstants.SpotsPerLane + slot] == type) count++;
            return count;
        }
    }
}
