using UnityEngine;

namespace Gatekeeper
{
    [RequireComponent(typeof(Collider2D))]
    public class BuildSpot : MonoBehaviour
    {
        [Tooltip("0-8, lane * 3 + slot. Slot 0 is nearest the enemy spawn.")]
        [SerializeField] int index;
        [SerializeField] SpriteRenderer marker;
        [SerializeField] Color idleColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] Color hoverColor = Color.white;
        [SerializeField] Sprite idleSprite;
        [Tooltip("Optional: shown while the mouse is over the spot.")]
        [SerializeField] Sprite hoverSprite;

        public int Index => index;
        public int Lane => MapConstants.LaneOfSpot(index);
        public Tower Tower { get; set; }
        public bool IsEmpty => Tower == null;

        public void SetHighlight(bool hovered, bool visible)
        {
            marker.enabled = visible;
            marker.color = hovered ? hoverColor : idleColor;
            if (hoverSprite != null && idleSprite != null) marker.sprite = hovered ? hoverSprite : idleSprite;
        }
    }
}
