using System;
using UnityEngine;

namespace Gatekeeper
{
    /// <summary>
    /// A scripted defender with a habit, used to evaluate the War Council.
    /// A learning Council should discover and exploit the habit; a random one can't.
    /// </summary>
    [Serializable]
    public class BotPersona
    {
        public string name = "Bot";
        [Tooltip("Spot index (0-8) and tower placed there, in order. Extra entries past the limits are ignored.")]
        public BotPlacement[] placements;
        public WeaponType weapon = WeaponType.Bow;
        [Tooltip("Lane the bot hero guards: 0 Left, 1 Middle, 2 Right.")]
        public int favouriteLane = 1;
        [Range(0f, 1f), Tooltip("Chance per placement to pick a random spot and tower instead.")]
        public float noise = 0.1f;
    }

    [Serializable]
    public struct BotPlacement
    {
        public int spot;
        public TowerType tower;
    }

    /// <summary>Bot hero: stands in its favourite lane, chases nearby enemies and always attacks.</summary>
    public sealed class BotHeroInput : IHeroInput
    {
        static readonly float[] LaneX = { -6f, 0f, 6f };

        readonly Hero hero;
        readonly Vector2 post;

        public BotHeroInput(Hero hero, int lane)
        {
            this.hero = hero;
            post = new Vector2(LaneX[Mathf.Clamp(lane, 0, 2)], -1.5f);
        }

        public Vector2 Move
        {
            get
            {
                var target = post;
                var enemy = Targeting.Nearest(post, 4f, hitsAir: true);
                if (enemy != null) target = enemy.Position;
                var delta = target - hero.Position;
                return delta.magnitude < 0.5f ? Vector2.zero : delta.normalized;
            }
        }

        public bool AttackHeld => true;
    }
}
