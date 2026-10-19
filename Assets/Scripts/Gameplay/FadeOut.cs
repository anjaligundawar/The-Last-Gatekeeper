using UnityEngine;

namespace Gatekeeper
{
    /// <summary>Short-lived visual effect: fades its sprite out, then destroys itself.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class FadeOut : MonoBehaviour
    {
        [SerializeField] float duration = 0.15f;

        SpriteRenderer sprite;
        float startAlpha;
        float t;

        public static FadeOut Spawn(FadeOut prefab, Vector2 position, Vector2 size, float angle = 0f)
        {
            if (prefab == null) return null;
            var fx = Instantiate(prefab, position, Quaternion.Euler(0f, 0f, angle));
            // The prefab's own scale maps its sprite to a 1 x 1 unit box, so size is in world units.
            var unit = prefab.transform.localScale;
            fx.transform.localScale = new Vector3(size.x * unit.x, size.y * unit.y, 1f);
            return fx;
        }

        void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
            startAlpha = sprite.color.a;
        }

        void Update()
        {
            t += Time.deltaTime;
            var c = sprite.color;
            c.a = Mathf.Lerp(startAlpha, 0f, t / duration);
            sprite.color = c;
            if (t >= duration) Destroy(gameObject);
        }
    }
}
