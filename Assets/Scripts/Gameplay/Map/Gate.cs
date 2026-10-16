using UnityEngine;

namespace Gatekeeper
{
    [RequireComponent(typeof(Health))]
    public class Gate : MonoBehaviour
    {
        [SerializeField] Transform attackPoint;
        [SerializeField] SpriteRenderer body;
        [SerializeField] Color damagedColor = new Color(0.35f, 0.2f, 0.15f);
        [Tooltip("Enemies closer than this to the attack point start hitting the gate.")]
        [SerializeField] float reach = 0.6f;

        public Health Health { get; private set; }
        public Vector2 AttackPoint => attackPoint.position;
        public float Reach => reach;

        Color baseColor;
        float flash;

        void Awake()
        {
            Health = GetComponent<Health>();
            baseColor = body.color;
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
        }
    }
}
