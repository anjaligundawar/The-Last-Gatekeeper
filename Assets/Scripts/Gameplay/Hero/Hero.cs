using UnityEngine;

namespace Gatekeeper
{
    [RequireComponent(typeof(Health), typeof(Rigidbody2D), typeof(HeroCombat))]
    public class Hero : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] Rect arenaBounds = Rect.MinMaxRect(-10.5f, -5.6f, 10.5f, 5.6f);
        [SerializeField] SpriteRenderer body;
        [SerializeField] Color hurtColor = Color.red;

        public Health Health { get; private set; }
        public HeroCombat Combat { get; private set; }
        public IHeroInput Input { get; set; } = new KeyboardHeroInput();
        public Vector2 Position => body2D.position;
        public Vector2 Facing { get; private set; } = Vector2.up;

        Rigidbody2D body2D;
        Color baseColor;
        Vector2 moveInput;
        float flash;
        readonly float[] laneTime = new float[MapConstants.LaneCount];

        void Awake()
        {
            Health = GetComponent<Health>();
            Combat = GetComponent<HeroCombat>();
            body2D = GetComponent<Rigidbody2D>();
            baseColor = body.color;
            Health.Damaged += _ => flash = 0.12f;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            moveInput = Vector2.zero;
            if (gm == null || Health.IsDead) return;

            if (CanMove(gm.Phase))
            {
                moveInput = Input.Move;
                if (moveInput.sqrMagnitude > 0.01f) Facing = moveInput.normalized;
            }

            if (gm.Phase == GamePhase.Wave)
            {
                laneTime[LanePath.LaneOfX(Position.x)] += Time.deltaTime;
                Combat.Tick(Time.deltaTime);
            }

            if (flash > 0f) flash -= Time.deltaTime;
            body.color = flash > 0f ? hurtColor : baseColor;
        }

        void FixedUpdate()
        {
            if (moveInput == Vector2.zero) return;
            var p = body2D.position + moveInput * moveSpeed * Time.fixedDeltaTime;
            p.x = Mathf.Clamp(p.x, arenaBounds.xMin, arenaBounds.xMax);
            p.y = Mathf.Clamp(p.y, arenaBounds.yMin, arenaBounds.yMax);
            body2D.MovePosition(p);
        }

        static bool CanMove(GamePhase phase) =>
            phase == GamePhase.Build || phase == GamePhase.WeaponPick || phase == GamePhase.Wave;

        public void TakeDamage(float amount) => Health.Damage(amount);

        /// <summary>Fraction of the last wave spent in each lane; resets the counter.</summary>
        public float[] ConsumeLaneTime()
        {
            float total = 0f;
            foreach (var t in laneTime) total += t;
            var fractions = new float[laneTime.Length];
            for (int i = 0; i < laneTime.Length; i++)
            {
                fractions[i] = total > 0f ? laneTime[i] / total : 0f;
                laneTime[i] = 0f;
            }
            return fractions;
        }
    }
}
