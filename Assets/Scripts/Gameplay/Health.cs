using System;
using UnityEngine;

namespace Gatekeeper
{
    public class Health : MonoBehaviour
    {
        [SerializeField] float maxHp = 100f;

        public float Max => maxHp;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;
        public float Fraction => maxHp > 0f ? Current / maxHp : 0f;

        public event Action<float> Damaged;
        public event Action Died;

        void Awake() => Current = maxHp;

        public void SetMax(float value)
        {
            maxHp = value;
            Current = value;
        }

        public void Damage(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(amount);
            if (IsDead) Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(maxHp, Current + amount);
        }
    }
}
