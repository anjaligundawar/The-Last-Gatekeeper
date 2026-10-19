using UnityEngine;

namespace Gatekeeper
{
    /// <summary>Anything with a reload: towers and the hero's weapon.</summary>
    public interface ICooldown
    {
        /// <summary>1 right after attacking, falling to 0 when ready again.</summary>
        float CooldownFraction { get; }
    }

    /// <summary>World-space bar that fills up while its source reloads and hides when it is ready.</summary>
    public class CooldownBar : MonoBehaviour
    {
        [Tooltip("A component that implements ICooldown, such as Tower or HeroCombat.")]
        [SerializeField] MonoBehaviour source;
        [SerializeField] Transform fill;

        ICooldown cooldown;
        Renderer[] renderers;

        void Awake()
        {
            cooldown = source as ICooldown;
            renderers = GetComponentsInChildren<Renderer>();
        }

        void LateUpdate()
        {
            if (cooldown == null || fill == null) return;
            float remaining = cooldown.CooldownFraction;
            float f = 1f - remaining;
            fill.localScale = new Vector3(f, 1f, 1f);
            fill.localPosition = new Vector3((f - 1f) * 0.5f, 0f, 0f);

            bool visible = remaining > 0f;
            foreach (var r in renderers) r.enabled = visible;
        }
    }
}
