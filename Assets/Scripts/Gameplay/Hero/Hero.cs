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
        [Tooltip("How long the body flashes white when hit, before the red hurt tint. Needs the Sprite Flash material.")]
        [SerializeField] float flashDuration = 0.06f;
        [Tooltip("How long a knockback push lasts. The hero can't steer while pushed.")]
        [SerializeField] float knockbackDuration = 0.15f;

        public Health Health { get; private set; }
        public HeroCombat Combat { get; private set; }
        public IHeroInput Input { get; set; } = new KeyboardHeroInput();
        public Vector2 Position => body2D.position;
        public Vector2 Facing { get; private set; } = Vector2.up;

        Rigidbody2D body2D;
        Color baseColor;
        Vector2 moveInput;
        float flash;
        float whiteFlash;
        Vector2 knockback;
        float knockbackTimer;
        MaterialPropertyBlock flashBlock;
        bool hasFlashMaterial;

        static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

        void Awake()
        {
            Health = GetComponent<Health>();
            Combat = GetComponent<HeroCombat>();
            body2D = GetComponent<Rigidbody2D>();
            baseColor = body.color;
            Health.Damaged += _ =>
            {
                flash = 0.15f;
                whiteFlash = flashDuration;
            };
            flashBlock = new MaterialPropertyBlock();
            hasFlashMaterial = body.sharedMaterial != null && body.sharedMaterial.HasProperty(FlashAmountId);
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

            if (gm.Phase == GamePhase.Wave) Combat.Tick(Time.deltaTime);

            if (flash > 0f) flash -= Time.deltaTime;
            if (whiteFlash > 0f) whiteFlash -= Time.deltaTime;
            body.color = flash > 0f ? hurtColor : baseColor;
            if (hasFlashMaterial)
            {
                body.GetPropertyBlock(flashBlock);
                flashBlock.SetFloat(FlashAmountId, whiteFlash > 0f ? 1f : 0f);
                body.SetPropertyBlock(flashBlock);
            }
            else if (whiteFlash > 0f)
            {
                body.color = Color.white;
            }
        }

        void FixedUpdate()
        {
            if (knockbackTimer > 0f)
            {
                // Pushed back: slide and ignore movement input until the push ends.
                float t = knockbackTimer / knockbackDuration;
                MoveTo(body2D.position + knockback * t * Time.fixedDeltaTime);
                knockbackTimer -= Time.fixedDeltaTime;
                return;
            }
            if (moveInput == Vector2.zero) return;
            MoveTo(body2D.position + moveInput * moveSpeed * Time.fixedDeltaTime);
        }

        void MoveTo(Vector2 p)
        {
            p.x = Mathf.Clamp(p.x, arenaBounds.xMin, arenaBounds.xMax);
            p.y = Mathf.Clamp(p.y, arenaBounds.yMin, arenaBounds.yMax);
            body2D.MovePosition(body2D.position + LaneEdge.Constrain(body2D, p - body2D.position));
        }

        static bool CanMove(GamePhase phase) =>
            phase == GamePhase.Build || phase == GamePhase.WeaponPick || phase == GamePhase.Wave;

        public void TakeDamage(float amount) => TakeHit(amount, Vector2.zero);

        /// <param name="push">Knockback velocity in units per second. Zero for no knockback.</param>
        public void TakeHit(float amount, Vector2 push)
        {
            if (Health.IsDead) return;
            if (push != Vector2.zero)
            {
                knockback = push;
                knockbackTimer = knockbackDuration;
            }
            Health.Damage(amount);
        }
    }
}
