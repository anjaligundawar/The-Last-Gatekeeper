using UnityEngine;

namespace Gatekeeper
{
    /// <summary>
    /// Shows an enemy's weapon and animates it when the enemy attacks:
    /// the sword swings across its pivot, the sphere flies to the target and comes back,
    /// and the bow stays put while only the arrow flies. The weapon flashes white as the attack starts.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class EnemyWeaponVisual : MonoBehaviour
    {
        [Tooltip("Child at the enemy's centre. The weapon sprite is its child, pointing along +X.")]
        [SerializeField] Transform pivot;
        [Tooltip("The sword, the sphere or the bow.")]
        [SerializeField] SpriteRenderer weaponRenderer;
        [Tooltip("Bow only: the arrow that flies. Hidden until the bow shoots.")]
        [SerializeField] SpriteRenderer arrow;

        [Header("Feel")]
        [Tooltip("How far the weapon sits from the enemy's centre.")]
        [SerializeField] float holdDistance = 0.3f;
        [SerializeField] float swingTime = 0.2f;
        [Tooltip("Total sword arc in degrees, centred on the target.")]
        [SerializeField] float swordArc = 130f;
        [Tooltip("Sphere flight out and back, in seconds. It hits halfway.")]
        [SerializeField] float sphereTime = 0.4f;
        [SerializeField] float arrowSpeed = 9f;
        [Tooltip("How long the weapon flashes white when the attack starts. Needs the Sprite Flash material.")]
        [SerializeField] float flashDuration = 0.06f;

        Enemy enemy;
        CharacterAnimator animator;
        EnemyWeapon kind;
        float timer;
        float duration;
        float flashTimer;
        float aimAngle;
        Vector2 target;
        Vector2 arrowStart;
        int baseOrder;
        MaterialPropertyBlock flashBlock;

        static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

        void Awake()
        {
            enemy = GetComponent<Enemy>();
            animator = GetComponent<CharacterAnimator>();
            kind = enemy.Data != null ? enemy.Data.weapon : EnemyWeapon.Sword;
            baseOrder = weaponRenderer.sortingOrder;
            flashBlock = new MaterialPropertyBlock();
            if (arrow != null) arrow.enabled = false;
        }

        /// <summary>Start the attack animation towards a world position. Returns the seconds until it hits.</summary>
        public float Play(Vector2 worldTarget)
        {
            target = worldTarget;
            var direction = target - (Vector2)pivot.position;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector2.down;
            aimAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            flashTimer = flashDuration;

            switch (kind)
            {
                case EnemyWeapon.Sphere:
                    duration = sphereTime;
                    timer = duration;
                    return sphereTime * 0.5f;

                case EnemyWeapon.Bow:
                    arrowStart = Hand(aimAngle);
                    duration = Mathf.Max(0.05f, Vector2.Distance(arrowStart, target) / arrowSpeed);
                    timer = duration;
                    return duration;

                default:
                    duration = swingTime;
                    timer = duration;
                    return swingTime * 0.5f;
            }
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (flashTimer > 0f) flashTimer -= dt;
            bool attacking = timer > 0f;
            if (attacking) timer -= dt;
            float t = attacking ? 1f - Mathf.Clamp01(timer / duration) : 0f; // 0 -> 1 over the attack

            float angle;
            if (attacking)
            {
                angle = aimAngle;
                if (kind == EnemyWeapon.Sword)
                    angle = aimAngle + Mathf.Lerp(swordArc * 0.5f, -swordArc * 0.5f, EaseOut(t));
            }
            else
            {
                // Idle: hold the weapon the way the enemy faces, tilted a little.
                var facing = animator != null ? animator.Facing : Vector2.down;
                angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg + (kind == EnemyWeapon.Bow ? 0f : 35f);
            }

            pivot.localRotation = Quaternion.Euler(0f, 0f, angle);
            weaponRenderer.transform.localPosition = new Vector3(holdDistance, 0f, 0f);
            if (kind == EnemyWeapon.Sphere && attacking)
            {
                // Out to the target and back to the hand: furthest at the halfway point.
                weaponRenderer.transform.position = Vector2.Lerp(Hand(angle), target, Mathf.Sin(t * Mathf.PI));
            }

            if (arrow != null)
            {
                arrow.enabled = kind == EnemyWeapon.Bow && attacking;
                if (arrow.enabled)
                {
                    arrow.transform.position = Vector2.Lerp(arrowStart, target, t);
                    arrow.transform.rotation = Quaternion.Euler(0f, 0f, aimAngle);
                }
            }

            // Pointing up means the weapon is behind the enemy.
            weaponRenderer.sortingOrder = Mathf.Sin(angle * Mathf.Deg2Rad) > 0.3f ? baseOrder - 2 : baseOrder;
            SetFlash(weaponRenderer);
            if (arrow != null) SetFlash(arrow);
        }

        Vector2 Hand(float angle)
        {
            float rad = angle * Mathf.Deg2Rad;
            return (Vector2)pivot.position + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * holdDistance;
        }

        void SetFlash(SpriteRenderer sprite)
        {
            if (sprite.sharedMaterial == null || !sprite.sharedMaterial.HasProperty(FlashAmountId)) return;
            sprite.GetPropertyBlock(flashBlock);
            flashBlock.SetFloat(FlashAmountId, flashTimer > 0f ? 1f : 0f);
            sprite.SetPropertyBlock(flashBlock);
        }

        static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
