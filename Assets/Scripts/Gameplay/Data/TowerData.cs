using UnityEngine;

namespace Gatekeeper
{
    [CreateAssetMenu(menuName = "Gatekeeper/Tower Data", fileName = "TowerData")]
    public class TowerData : ScriptableObject
    {
        public TowerType type;
        public string displayName = "Archer";
        [TextArea] public string description;
        public Color uiColor = Color.white;

        [Header("Attack")]
        public float range = 3.5f;
        public float interval = 0.8f;
        public float damage = 8f;
        public DamageType damageType = DamageType.Arrow;
        public bool hitsAir = true;
        public float splashRadius;

        [Header("Slow (Frost)")]
        [Range(0f, 1f)] public float slowFactor = 1f;
        public float slowDuration;
    }
}
