using UnityEngine;

namespace Gatekeeper
{
    /// <summary>World-space bar: scales the fill sprite from the left edge.</summary>
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] Health health;
        [SerializeField] Transform fill;
        [SerializeField] bool hideWhenFull = true;

        Renderer[] renderers;

        void Awake() => renderers = GetComponentsInChildren<Renderer>();

        void LateUpdate()
        {
            if (health == null || fill == null) return;
            float f = health.Fraction;
            fill.localScale = new Vector3(f, 1f, 1f);
            fill.localPosition = new Vector3((f - 1f) * 0.5f, 0f, 0f);

            bool visible = !(hideWhenFull && f >= 1f);
            foreach (var r in renderers) r.enabled = visible;
        }
    }
}
