using UnityEngine;

namespace Gatekeeper
{
    [CreateAssetMenu(menuName = "Gatekeeper/Weapon Data", fileName = "WeaponData")]
    public class WeaponData : ScriptableObject
    {
        public WeaponType type;
        public string displayName = "Bow";
        [TextArea] public string description;

        public float range = 6f;
        public float cooldown = 0.45f;
        public float damage = 8f;
        public DamageType damageType = DamageType.Arrow;
        public bool hitsAir = true;
        [Tooltip("Spear only: width of the thrust.")]
        public float width = 0.5f;
    }
}
