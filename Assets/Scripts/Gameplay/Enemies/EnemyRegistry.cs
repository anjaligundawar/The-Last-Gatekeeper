using System.Collections.Generic;

namespace Gatekeeper
{
    /// <summary>Tracks living enemies so the wave knows when it is over.</summary>
    public static class EnemyRegistry
    {
        static readonly List<Enemy> alive = new List<Enemy>();

        public static IReadOnlyList<Enemy> Alive => alive;
        public static int Count => alive.Count;

        public static void Add(Enemy enemy) => alive.Add(enemy);
        public static void Remove(Enemy enemy) => alive.Remove(enemy);
    }
}
