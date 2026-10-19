using UnityEngine;

namespace Gatekeeper
{
    /// <summary>
    /// Flip-book animation for 4-direction character sheets: walks while the object moves,
    /// stands on the first frame when it stops, and shows the attack frame when told to.
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        [SerializeField] SpriteRenderer body;
        [Tooltip("4 frames each.")]
        [SerializeField] Sprite[] walkDown = new Sprite[4];
        [SerializeField] Sprite[] walkUp = new Sprite[4];
        [SerializeField] Sprite[] walkLeft = new Sprite[4];
        [SerializeField] Sprite[] walkRight = new Sprite[4];
        [Tooltip("Optional, one per direction: down, up, left, right.")]
        [SerializeField] Sprite[] attack = new Sprite[4];
        [SerializeField] float frameTime = 0.14f;
        [SerializeField] float attackTime = 0.2f;
        [Tooltip("Seconds without movement before the walk stops. Physics moves the body in fixed steps, so some frames see no movement even while walking.")]
        [SerializeField] float stopDelay = 0.1f;
        [Tooltip("Keep flapping when standing still (flyers).")]
        [SerializeField] bool alwaysAnimate;

        Vector2 lastPosition;
        Vector2 facing = Vector2.down;
        float frameTimer;
        int frame;
        float attackTimer;
        float stillTimer;

        public Vector2 Facing => facing;

        void Awake()
        {
            lastPosition = transform.position;
            stillTimer = stopDelay;
        }

        public void PlayAttack(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f) facing = direction;
            attackTimer = attackTime;
        }

        void LateUpdate()
        {
            Vector2 position = transform.position;
            var delta = position - lastPosition;
            lastPosition = position;
            bool movedThisFrame = delta.sqrMagnitude > 0.000001f;
            if (movedThisFrame && attackTimer <= 0f) facing = delta;
            stillTimer = movedThisFrame ? 0f : stillTimer + Time.deltaTime;
            bool moving = stillTimer < stopDelay;

            int dir = Direction(facing);
            if (attackTimer > 0f)
            {
                attackTimer -= Time.deltaTime;
                var hit = attack != null && attack.Length == 4 ? attack[dir] : null;
                if (hit != null)
                {
                    body.sprite = hit;
                    return;
                }
            }

            var frames = dir == 0 ? walkDown : dir == 1 ? walkUp : dir == 2 ? walkLeft : walkRight;
            if (moving || alwaysAnimate)
            {
                frameTimer += Time.deltaTime;
                if (frameTimer >= frameTime)
                {
                    frameTimer -= frameTime;
                    frame = (frame + 1) % frames.Length;
                }
            }
            else
            {
                frame = 0;
                frameTimer = 0f;
            }
            body.sprite = frames[frame];
        }

        // 0 down, 1 up, 2 left, 3 right
        static int Direction(Vector2 v)
        {
            if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) return v.x < 0f ? 2 : 3;
            return v.y > 0f ? 1 : 0;
        }
    }
}
