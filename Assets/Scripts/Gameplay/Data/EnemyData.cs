using UnityEngine;

namespace Gatekeeper
{
    [CreateAssetMenu(menuName = "Gatekeeper/Enemy Data", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        public EnemyType type;
        public string displayName = "Grunt";

        [Header("Stats")]
        public float maxHp = 30f;
        public float speed = 1.4f;
        public float damage = 5f;
        public float attackInterval = 1f;
        [Tooltip("Budget points this enemy costs the War Council.")]
        public int cost = 1;
        [Tooltip("Flyers ignore the hero, cannons and the sword.")]
        public bool flying;

        [Header("Damage taken multipliers")]
        public float arrowMultiplier = 1f;
        public float explosiveMultiplier = 1f;

        [Tooltip("1 = normal knockback, 0 = can't be pushed.")]
        public float knockbackScale = 1f;

        [Header("Weapon")]
        [Tooltip("Sword swings on a pivot, Sphere flies out and back, Bow shoots an arrow.")]
        public EnemyWeapon weapon = EnemyWeapon.Sword;
        [Tooltip("How hard this enemy's hits push the hero, in units per second.")]
        public float hitKnockback = 5f;

        [Header("Behaviour (detect < lose, so enemies don't flicker between states)")]
        public float detectRange = 3f;
        public float loseRange = 5f;
        public float attackRange = 0.8f;

        public float Multiplier(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Arrow: return arrowMultiplier;
                case DamageType.Explosive: return explosiveMultiplier;
                default: return 1f;
            }
        }
    }
}
