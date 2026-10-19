using UnityEngine;

namespace Gatekeeper
{
    /// <summary>Homing projectile. With a splash radius it explodes on arrival and hits ground enemies only.</summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField] float speed = 11f;
        [SerializeField] FadeOut impactEffect;
        [Tooltip("Knockback speed on hit. Splash pushes away from the blast.")]
        [SerializeField] float knockback = 1.5f;

        Enemy target;
        Vector2 destination;
        float damage;
        DamageType damageType;
        float splashRadius;

        public void Launch(Enemy targetEnemy, float hitDamage, DamageType type, float splash = 0f)
        {
            target = targetEnemy;
            destination = target.Position;
            damage = hitDamage;
            damageType = type;
            splashRadius = splash;
        }

        void Update()
        {
            if (target != null && !target.IsDead) destination = target.Position;
            transform.position = Vector2.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            var travel = destination - (Vector2)transform.position;
            if (travel.sqrMagnitude > 0.0025f)
            {
                transform.right = travel;
                return;
            }

            if (splashRadius > 0f)
            {
                foreach (var enemy in Targeting.InRange(destination, splashRadius, hitsAir: false))
                    enemy.TakeHit(damage, damageType, Push(enemy.Position - destination, transform.right));
                FadeOut.Spawn(impactEffect, destination, Vector2.one * splashRadius * 2f);
                if (GameManager.Instance != null) GameManager.Instance.Shake(0.03f, 0.05f);
            }
            else if (target != null && !target.IsDead)
            {
                target.TakeHit(damage, damageType, transform.right * knockback);
            }
            Destroy(gameObject);
        }

        Vector2 Push(Vector2 away, Vector2 fallback) =>
            (away.sqrMagnitude > 0.0001f ? away.normalized : fallback.normalized) * knockback;
    }
}
