using System.Collections.Generic;
using UnityEngine;

namespace Gatekeeper
{
    /// <summary>Physics2D queries for towers and weapons.</summary>
    public static class Targeting
    {
        static readonly List<Collider2D> hits = new List<Collider2D>();
        static readonly ContactFilter2D filter = new ContactFilter2D().NoFilter();

        static bool Valid(Enemy e, bool hitsAir) => e != null && !e.IsDead && (hitsAir || !e.IsFlying);

        public static Enemy Nearest(Vector2 position, float range, bool hitsAir)
        {
            Enemy best = null;
            float bestSqr = float.MaxValue;
            foreach (var enemy in InRange(position, range, hitsAir))
            {
                float sqr = (enemy.Position - position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = enemy;
                }
            }
            return best;
        }

        public static List<Enemy> InRange(Vector2 position, float range, bool hitsAir)
        {
            Physics2D.OverlapCircle(position, range, filter, hits);
            return Collect(hitsAir);
        }

        public static List<Enemy> InLine(Vector2 origin, Vector2 direction, float length, float width, bool hitsAir)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Physics2D.OverlapBox(origin + direction * length * 0.5f, new Vector2(length, width), angle, filter, hits);
            return Collect(hitsAir);
        }

        static List<Enemy> Collect(bool hitsAir)
        {
            var result = new List<Enemy>();
            foreach (var hit in hits)
                if (hit.TryGetComponent(out Enemy enemy) && Valid(enemy, hitsAir) && !result.Contains(enemy))
                    result.Add(enemy);
            return result;
        }
    }
}
