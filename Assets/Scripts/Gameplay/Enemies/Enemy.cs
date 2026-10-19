using System;
using UnityEngine;

namespace Gatekeeper
{
    [RequireComponent(typeof(Health), typeof(Rigidbody2D))]
    public class Enemy : MonoBehaviour
    {
        [SerializeField] EnemyData data;

        [Header("Visuals")]
        [SerializeField] SpriteRenderer body;
        [SerializeField] SpriteRenderer stateIcon;
        [SerializeField] Color chaseColor = new Color(1f, 0.75f, 0.1f);
        [SerializeField] Color attackColor = new Color(1f, 0.2f, 0.15f);
        [SerializeField] Color slowedColor = new Color(0.6f, 0.9f, 1f);
        [SerializeField] FadeOut deathEffect;

        [Header("State icons (leave empty to tint the icon instead)")]
        [SerializeField] Sprite chaseIcon;
        [SerializeField] Sprite attackIcon;
        [SerializeField] Sprite attackGateIcon;

        [Header("Hit feedback")]
        [Tooltip("How long the body flashes white when hit. Needs the Sprite Flash material.")]
        [SerializeField] float flashDuration = 0.08f;
        [Tooltip("How long a knockback push lasts. The enemy doesn't act while it is pushed.")]
        [SerializeField] float knockbackDuration = 0.12f;

        /// <summary>Raised when an enemy is killed (not when it is cleared at a wave timeout).</summary>
        public static event Action<Enemy> Killed;

        /// <summary>Raised when an enemy leaves play for any reason, so the wave can read its final progress.</summary>
        public static event Action<Enemy> Removed;

        public EnemyData Data => data;
        public LanePath Lane { get; private set; }
        public Hero Hero { get; private set; }
        public Gate Gate { get; private set; }
        public Health Health { get; private set; }

        public int WaypointIndex { get; set; } = 1;
        public float AttackTimer { get; set; }
        public EnemyStateId StateId => state.Id;
        public bool IsFlying => data.flying;
        public bool IsDead => Health == null || Health.IsDead;
        public Vector2 Position => body2D.position;
        public bool ReachedGate { get; private set; }

        Rigidbody2D body2D;
        CharacterAnimator animator;
        EnemyWeaponVisual weapon;
        IEnemyState state = EnemyStates.March;
        Color baseColor;
        Color displayColor;
        Vector3 baseScale;
        float flashTimer;
        float slowTimer;
        float slowFactor = 1f;
        float lungeTimer;
        bool removed;
        Vector2 knockback;
        float knockbackTimer;
        bool hitPending;
        bool hitOnGate;
        float hitTimer;
        MaterialPropertyBlock flashBlock;
        bool hasFlashMaterial;

        static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

        void Awake()
        {
            Health = GetComponent<Health>();
            body2D = GetComponent<Rigidbody2D>();
            animator = GetComponent<CharacterAnimator>();
            weapon = GetComponent<EnemyWeaponVisual>();
            Health.Died += OnDied;
            baseColor = displayColor = body.color;
            baseScale = body.transform.localScale;
            stateIcon.enabled = false;
            flashBlock = new MaterialPropertyBlock();
            hasFlashMaterial = body.sharedMaterial != null && body.sharedMaterial.HasProperty(FlashAmountId);
        }

        /// <summary>Called by the WaveSpawner right after Instantiate.</summary>
        public void Setup(LanePath lane, Hero hero, Gate gate, float hpScale)
        {
            Lane = lane;
            Hero = hero;
            Gate = gate;
            Health.SetMax(data.maxHp * hpScale);
            transform.position = lane.Spawn;
            body2D.position = lane.Spawn;
            EnemyRegistry.Add(this);
            state.Enter(this);
        }

        void FixedUpdate()
        {
            if (IsDead || Lane == null) return;
            if (GameManager.Instance == null || GameManager.Instance.Phase != GamePhase.Wave) return;
            if (knockbackTimer > 0f)
            {
                // Pushed back: slide and skip the state machine until the push ends.
                float t = knockbackTimer / knockbackDuration;
                Step(knockback * t * Time.fixedDeltaTime);
                knockbackTimer -= Time.fixedDeltaTime;
                return;
            }
            state.Tick(this, Time.fixedDeltaTime);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (slowTimer > 0f) slowTimer -= dt;
            if (flashTimer > 0f) flashTimer -= dt;
            if (lungeTimer > 0f) lungeTimer -= dt;
            if (hitPending && !IsDead)
            {
                hitTimer -= dt;
                if (hitTimer <= 0f) LandHit();
            }
            UpdateVisuals(dt);
        }

        // ---- State machine helpers -----------------------------------------

        public void ChangeState(EnemyStateId id)
        {
            if (state.Id == id) return;
            state = EnemyStates.Get(id);
            state.Enter(this);
        }

        public bool CanSeeHero(float range) =>
            !IsFlying && !ReachedGate && Hero != null && !Hero.Health.IsDead && DistanceToHero() <= range;

        public float DistanceToHero() => Vector2.Distance(Position, Hero.Position);
        public float DistanceToGate() => Vector2.Distance(Position, Gate.AttackPoint);

        public void MoveTowards(Vector2 target, float dt)
        {
            float speed = data.speed * (slowTimer > 0f ? slowFactor : 1f);
            Step(Vector2.MoveTowards(Position, target, speed * dt) - Position);
        }

        // Ground enemies stay on the lanes; flyers go straight over the grass.
        void Step(Vector2 delta)
        {
            if (!IsFlying) delta = LaneEdge.Constrain(body2D, delta);
            body2D.MovePosition(Position + delta);
        }

        public void MarkReachedGate()
        {
            ReachedGate = true;
        }

        /// <summary>
        /// Start an attack on the hero, or on the gate in AttackGate. The damage lands when the
        /// weapon reaches the target: mid swing, when the sphere arrives, or when the arrow does.
        /// </summary>
        public void Strike()
        {
            hitOnGate = StateId == EnemyStateId.AttackGate || Hero == null;
            var target = hitOnGate ? Gate.AttackPoint : Hero.Position;
            lungeTimer = 0.12f;
            if (animator != null) animator.PlayAttack(target - Position);
            hitTimer = weapon != null ? weapon.Play(target) : 0f;
            hitPending = true;
            if (hitTimer <= 0f) LandHit();
        }

        void LandHit()
        {
            hitPending = false;
            if (GameManager.Instance == null || GameManager.Instance.Phase != GamePhase.Wave) return;
            if (hitOnGate)
            {
                Gate.Health.Damage(data.damage);
                return;
            }
            if (Hero == null || Hero.Health.IsDead) return;
            var away = Hero.Position - Position;
            var push = away.sqrMagnitude > 0.0001f ? away.normalized * data.hitKnockback : Vector2.zero;
            Hero.TakeHit(data.damage, push);
        }

        // ---- Combat ---------------------------------------------------------

        /// <param name="push">Knockback velocity in units per second. Zero for no knockback.</param>
        public void TakeHit(float damage, DamageType type, Vector2 push = default)
        {
            if (IsDead) return;
            flashTimer = flashDuration;
            if (push != Vector2.zero && data.knockbackScale > 0f)
            {
                knockback = push * data.knockbackScale;
                knockbackTimer = knockbackDuration;
            }
            Health.Damage(damage * data.Multiplier(type));
        }

        public void ApplySlow(float factor, float duration)
        {
            slowFactor = Mathf.Min(slowTimer > 0f ? slowFactor : 1f, factor);
            slowTimer = Mathf.Max(slowTimer, duration);
        }

        void OnDied()
        {
            FadeOut.Spawn(deathEffect, Position, Vector2.one * baseScale.x * 1.8f);
            Killed?.Invoke(this);
            Despawn();
        }

        /// <summary>Remove from play without counting as a kill.</summary>
        public void Despawn()
        {
            if (removed) return;
            removed = true;
            EnemyRegistry.Remove(this);
            Removed?.Invoke(this);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (!removed) EnemyRegistry.Remove(this);
        }

        // ---- Visual feedback: tint + icon per state, smoothed ---------------

        void UpdateVisuals(float dt)
        {
            Color target = baseColor;
            switch (state.Id)
            {
                case EnemyStateId.Chase:
                    target = Color.Lerp(baseColor, chaseColor, 0.35f);
                    ShowIcon(chaseIcon, chaseColor);
                    break;
                case EnemyStateId.Attack:
                    target = Color.Lerp(baseColor, attackColor, 0.45f);
                    ShowIcon(attackIcon, attackColor);
                    break;
                case EnemyStateId.AttackGate:
                    target = Color.Lerp(baseColor, attackColor, 0.45f);
                    ShowIcon(attackGateIcon != null ? attackGateIcon : attackIcon, attackColor);
                    break;
                default:
                    stateIcon.enabled = false;
                    break;
            }
            if (slowTimer > 0f) target = Color.Lerp(target, slowedColor, 0.5f);

            displayColor = Color.Lerp(displayColor, target, 1f - Mathf.Exp(-10f * dt));
            bool flashing = flashTimer > 0f;
            if (hasFlashMaterial)
            {
                body.color = displayColor;
                body.GetPropertyBlock(flashBlock);
                flashBlock.SetFloat(FlashAmountId, flashing ? 1f : 0f);
                body.SetPropertyBlock(flashBlock);
            }
            else
            {
                body.color = flashing ? Color.white : displayColor;
            }
            body.transform.localScale = baseScale * (lungeTimer > 0f ? 1.2f : 1f);
        }

        void ShowIcon(Sprite icon, Color tint)
        {
            stateIcon.enabled = true;
            if (icon != null)
            {
                stateIcon.sprite = icon;
                stateIcon.color = Color.white;
            }
            else
            {
                stateIcon.color = tint;
            }
        }
    }
}
