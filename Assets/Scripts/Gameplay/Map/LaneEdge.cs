using UnityEngine;

namespace Gatekeeper
{
    /// <summary>
    /// Marks the collider that covers the grass, so walkers stay on the lanes.
    /// The hero and enemies have kinematic bodies, which colliders don't stop on their own,
    /// so they ask <see cref="Constrain"/> before every move.
    /// </summary>
    public class LaneEdge : MonoBehaviour
    {
        const float Skin = 0.02f;
        static readonly RaycastHit2D[] Hits = new RaycastHit2D[8];
        static ContactFilter2D filter = new ContactFilter2D { useTriggers = false };

        /// <summary>
        /// Returns the part of <paramref name="delta"/> the body can move without walking onto the grass.
        /// When the full move is blocked it slides along the edge on one axis.
        /// </summary>
        public static Vector2 Constrain(Rigidbody2D body, Vector2 delta)
        {
            if (delta.sqrMagnitude < 1e-10f || IsFree(body, delta)) return delta;
            var alongX = new Vector2(delta.x, 0f);
            var alongY = new Vector2(0f, delta.y);
            bool xFree = Mathf.Abs(delta.x) > 1e-5f && IsFree(body, alongX);
            bool yFree = Mathf.Abs(delta.y) > 1e-5f && IsFree(body, alongY);
            if (xFree && yFree) return Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? alongX : alongY;
            if (xFree) return alongX;
            if (yFree) return alongY;
            return Vector2.zero;
        }

        static bool IsFree(Rigidbody2D body, Vector2 delta)
        {
            int count = body.Cast(delta.normalized, filter, Hits, delta.magnitude + Skin);
            for (int i = 0; i < count; i++)
            {
                // A body that already overlaps the edge (pushed in by a knockback) may still move out.
                if (Hits[i].fraction <= 0f) continue;
                if (Hits[i].collider.GetComponent<LaneEdge>() != null) return false;
            }
            return true;
        }
    }
}
