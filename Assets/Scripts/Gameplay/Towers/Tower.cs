using UnityEngine;

namespace Gatekeeper
{
    public class Tower : MonoBehaviour, ICooldown
    {
        [SerializeField] TowerData data;
        [SerializeField] SpriteRenderer rangeIndicator;
        [Tooltip("Archer and Cannon fire this. Leave empty for Frost.")]
        [SerializeField] Projectile projectilePrefab;
        [Tooltip("Frost pulse effect.")]
        [SerializeField] FadeOut pulseEffect;

        public TowerData Data => data;

        public float CooldownFraction
        {
            get
            {
                var gm = GameManager.Instance;
                if (gm == null || gm.Phase != GamePhase.Wave || data.interval <= 0f) return 0f;
                return Mathf.Clamp01(cooldown / data.interval);
            }
        }

        float cooldown;

        void Start() => rangeIndicator.transform.localScale = Vector3.one * data.range * 2f;

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            rangeIndicator.enabled = gm.Phase == GamePhase.Build;
            if (gm.Phase != GamePhase.Wave) return;

            cooldown -= Time.deltaTime;
            if (cooldown > 0f) return;
            if (data.type == TowerType.Frost ? Pulse() : Fire()) cooldown = data.interval;
        }

        bool Fire()
        {
            var target = Targeting.Nearest(transform.position, data.range, data.hitsAir);
            if (target == null) return false;
            var projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            projectile.Launch(target, data.damage, data.damageType, data.splashRadius);
            return true;
        }

        bool Pulse()
        {
            var targets = Targeting.InRange(transform.position, data.range, data.hitsAir);
            if (targets.Count == 0) return false;
            foreach (var enemy in targets)
            {
                enemy.TakeHit(data.damage, data.damageType);
                enemy.ApplySlow(data.slowFactor, data.slowDuration);
            }
            FadeOut.Spawn(pulseEffect, transform.position, Vector2.one * data.range * 2f);
            return true;
        }
    }
}
