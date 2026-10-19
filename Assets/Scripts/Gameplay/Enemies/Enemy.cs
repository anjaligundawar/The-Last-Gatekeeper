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
        IEnemyState state = EnemyStates.March;
        Color baseColor;
        Color displayColor;
        Vector3 baseScale;
        float flashTimer;
        float slowTimer;
        float slowFactor = 1f;
        float lungeTimer;
        bool removed;

        void Awake()
        {
            Health = GetComponent<Health>();
            body2D = GetComponent<Rigidbody2D>();
            Health.Died += OnDied;
            baseColor = displayColor = body.color;
            baseScale = body.transform.localScale;
            stateIcon.enabled = false;
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
            state.Tick(this, Time.fixedDeltaTime);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (slowTimer > 0f) slowTimer -= dt;
            if (flashTimer > 0f) flashTimer -= dt;
            if (lungeTimer > 0f) lungeTimer -= dt;
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
            body2D.MovePosition(Vector2.MoveTowards(Position, target, speed * dt));
        }

        public void MarkReachedGate()
        {
            ReachedGate = true;
        }

        public void Lunge() => lungeTimer = 0.12f;

        // ---- Combat ---------------------------------------------------------

        public void TakeHit(float damage, DamageType type)
        {
            if (IsDead) return;
            flashTimer = 0.08f;
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
                    stateIcon.enabled = true;
                    stateIcon.color = chaseColor;
                    break;
                case EnemyStateId.Attack:
                case EnemyStateId.AttackGate:
                    target = Color.Lerp(baseColor, attackColor, 0.45f);
                    stateIcon.enabled = true;
                    stateIcon.color = attackColor;
                    break;
                default:
                    stateIcon.enabled = false;
                    break;
            }
            if (slowTimer > 0f) target = Color.Lerp(target, slowedColor, 0.5f);

            displayColor = Color.Lerp(displayColor, target, 1f - Mathf.Exp(-10f * dt));
            body.color = flashTimer > 0f ? Color.white : displayColor;
            body.transform.localScale = baseScale * (lungeTimer > 0f ? 1.2f : 1f);
        }
    }
}
