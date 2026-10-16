using UnityEngine;

namespace Gatekeeper
{
    /// <summary>A path from an enemy spawn to the gate, defined by child waypoint transforms.</summary>
    public class LanePath : MonoBehaviour
    {
        [SerializeField] int laneIndex;
        [SerializeField] Transform[] waypoints;
        [Tooltip("Tinted by the AI brain panel to show how weak the Council thinks this lane is.")]
        [SerializeField] SpriteRenderer heatOverlay;

        Vector2[] points;
        float[] cumulative;

        public int Index => laneIndex;
        public int Count => Points.Length;
        public Vector2 Spawn => Points[0];
        public Vector2 End => Points[Points.Length - 1];
        public float Length { get; private set; }
        public SpriteRenderer HeatOverlay => heatOverlay;

        Vector2[] Points
        {
            get
            {
                if (points == null) Cache();
                return points;
            }
        }

        public Vector2 GetPoint(int index) => Points[index];

        void Cache()
        {
            points = new Vector2[waypoints.Length];
            cumulative = new float[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
            {
                points[i] = waypoints[i].position;
                if (i > 0) cumulative[i] = cumulative[i - 1] + Vector2.Distance(points[i - 1], points[i]);
            }
            Length = cumulative[cumulative.Length - 1];
        }

        /// <summary>Progress (0..1) of a position heading for waypoint <paramref name="nextIndex"/>.</summary>
        public float Progress(Vector2 position, int nextIndex)
        {
            var p = Points;
            if (nextIndex <= 0) return 0f;
            if (nextIndex >= p.Length) return 1f;
            float done = cumulative[nextIndex] - Vector2.Distance(position, p[nextIndex]);
            return Mathf.Clamp01(done / Length);
        }

        /// <summary>Which lane an x position belongs to (used to track where the hero stands).</summary>
        public static int LaneOfX(float x) => x < -3.5f ? 0 : (x > 3.5f ? 2 : 1);

        void OnDrawGizmos()
        {
            if (waypoints == null) return;
            Gizmos.color = Color.yellow;
            for (int i = 1; i < waypoints.Length; i++)
                if (waypoints[i - 1] != null && waypoints[i] != null)
                    Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
        }
    }
}
