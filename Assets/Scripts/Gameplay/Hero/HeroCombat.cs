using UnityEngine;

namespace Gatekeeper
{
    /// <summary>Weapon logic. Attacks auto-aim at the nearest valid enemy in range.</summary>
    public class HeroCombat : MonoBehaviour
    {
        [Tooltip("Indexed by WeaponType: Bow, Sword, Spear.")]
        [SerializeField] WeaponData[] weapons = new WeaponData[3];
        [SerializeField] Projectile arrowPrefab;
        [SerializeField] FadeOut slashEffect;
        [SerializeField] FadeOut thrustEffect;

        public WeaponType Weapon { get; set; } = WeaponType.Bow;
        public WeaponData Current => weapons[(int)Weapon];
        public WeaponData Get(WeaponType type) => weapons[(int)type];

        Hero hero;
        float cooldown;

        void Awake() => hero = GetComponent<Hero>();

        public void Tick(float dt)
        {
            if (cooldown > 0f) cooldown -= dt;
            if (cooldown > 0f || !hero.Input.AttackHeld) return;
            if (Attack(Current)) cooldown = Current.cooldown;
        }

        bool Attack(WeaponData weapon)
        {
            var origin = hero.Position;
            var target = Targeting.Nearest(origin, weapon.range, weapon.hitsAir);

            switch (weapon.type)
            {
                case WeaponType.Bow:
                    if (target == null) return false;
                    var arrow = Instantiate(arrowPrefab, origin, Quaternion.identity);
                    arrow.Launch(target, weapon.damage, weapon.damageType);
                    return true;

                case WeaponType.Sword:
                    foreach (var enemy in Targeting.InRange(origin, weapon.range, weapon.hitsAir))
                        enemy.TakeHit(weapon.damage, weapon.damageType);
                    FadeOut.Spawn(slashEffect, origin, Vector2.one * weapon.range * 2f);
                    return true;

                case WeaponType.Spear:
                    var dir = target != null ? (target.Position - origin).normalized : hero.Facing;
                    foreach (var enemy in Targeting.InLine(origin, dir, weapon.range, weapon.width, weapon.hitsAir))
                        enemy.TakeHit(weapon.damage, weapon.damageType);
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    FadeOut.Spawn(thrustEffect, origin + dir * weapon.range * 0.5f, new Vector2(weapon.range, weapon.width), angle);
                    return true;
            }
            return false;
        }
    }
}
