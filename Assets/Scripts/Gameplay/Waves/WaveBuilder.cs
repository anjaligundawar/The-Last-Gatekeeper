using System.Collections.Generic;

namespace Gatekeeper
{
    public struct SpawnOrder
    {
        public EnemyType Type;
        public int Lane;
    }

    /// <summary>Turns an attack plan and a budget into a concrete list of enemies and lanes.</summary>
    public static class WaveBuilder
    {
        const float FocusShare = 0.7f;

        static readonly EnemyType[] Types = { EnemyType.Grunt, EnemyType.Runner, EnemyType.Shield, EnemyType.Flyer };

        /// <summary>Share of the budget spent on each enemy type, indexed by EnemyType.</summary>
        public static float[] Composition(ArmyType army)
        {
            switch (army)
            {
                case ArmyType.RunnerRush: return new[] { 0.3f, 0.7f, 0f, 0f };
                case ArmyType.ShieldWall: return new[] { 0.4f, 0f, 0.6f, 0f };
                case ArmyType.AirRaid: return new[] { 0f, 0.3f, 0f, 0.7f };
                default: return new[] { 0.7f, 0.15f, 0.15f, 0f };
            }
        }

        public static List<SpawnOrder> Build(AttackPlan plan, int budget, System.Func<EnemyType, int> costOf, System.Random rng)
        {
            var units = new List<EnemyType>();
            var weights = Composition(plan.Army);
            int spent = 0;
            for (int i = 0; i < Types.Length; i++)
            {
                int cost = System.Math.Max(1, costOf(Types[i]));
                int count = (int)(budget * weights[i] / cost);
                for (int n = 0; n < count; n++) units.Add(Types[i]);
                spent += count * cost;
            }
            for (; spent < budget; spent++) units.Add(EnemyType.Grunt); // leftover budget

            Shuffle(units, rng);

            var orders = new List<SpawnOrder>(units.Count);
            for (int i = 0; i < units.Count; i++)
                orders.Add(new SpawnOrder { Type = units[i], Lane = PickLane(plan.Focus, i, rng) });
            return orders;
        }

        static int PickLane(LaneFocus focus, int index, System.Random rng)
        {
            if (focus == LaneFocus.Even) return index % MapConstants.LaneCount;
            if (rng.NextDouble() < FocusShare) return (int)focus;
            int other = rng.Next(MapConstants.LaneCount - 1);
            return other >= (int)focus ? other + 1 : other;
        }

        static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
