using UnityEngine;

namespace Gatekeeper
{
    public class CameraShake : MonoBehaviour
    {
        Vector3 origin;
        float timer;
        float strength;

        void Awake() => origin = transform.localPosition;

        public void Shake(float amount, float duration)
        {
            strength = Mathf.Max(strength, amount);
            timer = Mathf.Max(timer, duration);
        }

        void LateUpdate()
        {
            if (timer > 0f)
            {
                timer -= Time.unscaledDeltaTime;
                transform.localPosition = origin + (Vector3)(Random.insideUnitCircle * strength);
                if (timer <= 0f)
                {
                    strength = 0f;
                    transform.localPosition = origin;
                }
            }
        }
    }
}
