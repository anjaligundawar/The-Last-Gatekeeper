using UnityEngine;

namespace Gatekeeper
{
    /// <summary>
    /// Shows the current weapon on a pivot at the hero's centre and rotates it when attacking:
    /// the sword sweeps an arc, the spear pokes forward, the bow aims and recoils.
    /// </summary>
    public class HeroWeaponVisual : MonoBehaviour
    {
        [Tooltip("Child at the hero's centre. The weapon sprite is its child, pointing along +X.")]
        [SerializeField] Transform pivot;
        [SerializeField] SpriteRenderer weaponRenderer;
        [Tooltip("Indexed by WeaponType: Bow, Sword, Spear.")]
        [SerializeField] Sprite[] weaponSprites = new Sprite[3];

        [Header("Feel")]
        [SerializeField] float attackTime = 0.15f;
        [Tooltip("Total sword arc in degrees, centred on the aim direction.")]
        [SerializeField] float swordArc = 140f;
        [Tooltip("How far the weapon sits from the hero's centre when idle.")]
        [SerializeField] float holdDistance = 0.35f;
        [SerializeField] float spearReach = 0.6f;
        [SerializeField] float bowRecoil = 0.12f;

        Hero hero;
        CharacterAnimator animator;
        WeaponType playing;
        float aimAngle;
        float timer;
        int baseOrder;

        void Awake()
        {
            hero = GetComponent<Hero>();
            animator = GetComponent<CharacterAnimator>();
            baseOrder = weaponRenderer.sortingOrder;
        }

        public void Play(WeaponType type, Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f) direction = hero.Facing;
            playing = type;
            aimAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            timer = attackTime;
            if (animator != null) animator.PlayAttack(direction);
        }

        void LateUpdate()
        {
            var type = hero.Combat.Weapon;
            weaponRenderer.sprite = weaponSprites[(int)type];

            float angle;
            float distance = holdDistance;
            if (timer > 0f && playing == type)
            {
                timer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(timer / attackTime); // 0 -> 1 over the attack
                angle = aimAngle;
                switch (type)
                {
                    case WeaponType.Sword:
                        angle = aimAngle + Mathf.Lerp(swordArc * 0.5f, -swordArc * 0.5f, EaseOut(t));
                        break;
                    case WeaponType.Spear:
                        distance = holdDistance + spearReach * Mathf.Sin(t * Mathf.PI);
                        break;
                    case WeaponType.Bow:
                        distance = holdDistance - bowRecoil * Mathf.Sin(t * Mathf.PI);
                        break;
                }
            }
            else
            {
                // Idle: hold the weapon pointing the way the hero faces, tilted a little.
                var facing = hero.Facing;
                angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg + (type == WeaponType.Bow ? 0f : 35f);
            }

            pivot.localRotation = Quaternion.Euler(0f, 0f, angle);
            weaponRenderer.transform.localPosition = new Vector3(distance, 0f, 0f);
            // Pointing up means the weapon is behind the hero.
            weaponRenderer.sortingOrder = Mathf.Sin(angle * Mathf.Deg2Rad) > 0.3f ? baseOrder - 2 : baseOrder;
        }

        static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
