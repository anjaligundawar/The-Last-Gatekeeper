using UnityEngine;

namespace Gatekeeper
{
    /// <summary>A path from an enemy spawn to the gate, defined by child waypoint transforms.</summary>
    public class LanePath : MonoBehaviour
    {
        [SerializeField] int laneIndex;
        [SerializeField] Transform[] waypoints;

        Vector2[] points;
        float[] cumulative;

        public int Index => laneIndex;
        public int Count => Points.Length;
        public Vector2 Spawn => Points[0];
        public Vector2 End => Points[Points.Length - 1];
        public float Length { get; private set; }

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
