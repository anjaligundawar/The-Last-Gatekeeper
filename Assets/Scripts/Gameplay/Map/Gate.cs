using UnityEngine;

namespace Gatekeeper
{
    [RequireComponent(typeof(Health))]
    public class Gate : MonoBehaviour
    {
        [SerializeField] Transform attackPoint;
        [SerializeField] SpriteRenderer body;
        [Tooltip("Optional: swapped in below 50% and 25% HP.")]
        [SerializeField] Sprite damagedSprite;
        [SerializeField] Sprite brokenSprite;
        [SerializeField] Color damagedColor = new Color(0.7f, 0.55f, 0.5f);
        [Tooltip("Enemies closer than this to the attack point start hitting the gate.")]
        [SerializeField] float reach = 0.6f;

        public Health Health { get; private set; }
        public Vector2 AttackPoint => attackPoint.position;
        public float Reach => reach;

        Color baseColor;
        Sprite healthySprite;
        float flash;

        void Awake()
        {
            Health = GetComponent<Health>();
            baseColor = body.color;
            healthySprite = body.sprite;
            Health.Damaged += _ =>
            {
                flash = 0.1f;
                if (GameManager.Instance != null) GameManager.Instance.Shake(0.08f, 0.1f);
            };
        }

        void Update()
        {
            if (flash > 0f) flash -= Time.deltaTime;
            var tint = Color.Lerp(baseColor, damagedColor, (1f - Health.Fraction) * 0.6f);
            body.color = flash > 0f ? Color.white : tint;

            float f = Health.Fraction;
            var sprite = f < 0.25f && brokenSprite != null ? brokenSprite
                : f < 0.5f && damagedSprite != null ? damagedSprite
                : healthySprite;
            if (body.sprite != sprite) body.sprite = sprite;
        }
    }
}
