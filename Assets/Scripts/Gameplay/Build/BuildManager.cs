using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Gatekeeper
{
    /// <summary>
    /// Build phase: 1/2/3 (or the panel buttons) pick a defence, left-click an empty spot to place,
    /// right-click to remove. Limits come from GameConfig. Defences are cleared after every wave.
    /// </summary>
    public class BuildManager : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] BuildSpot[] spots;
        [Tooltip("Indexed by TowerType: Archer, Cannon, Frost.")]
        [SerializeField] Tower[] towerPrefabs = new Tower[3];
        [SerializeField] Transform towerParent;

        public TowerType Selected { get; set; } = TowerType.Archer;
        public int Total { get; private set; }
        public int MaxTotal => config.maxDefences;
        public int MaxPerType => config.maxPerType;
        public BuildSpot[] Spots => spots;

        readonly int[] counts = new int[MapConstants.TowerTypeCount];
        readonly System.Collections.Generic.List<Collider2D> hits = new System.Collections.Generic.List<Collider2D>();
        BuildSpot hovered;

        public TowerData DataFor(TowerType type) => towerPrefabs[(int)type].Data;
        public int Count(TowerType type) => counts[(int)type];
        public bool CanPlace(TowerType type) => Total < MaxTotal && Count(type) < MaxPerType;

        public bool TryPlace(BuildSpot spot, TowerType type)
        {
            if (spot == null || !spot.IsEmpty || !CanPlace(type)) return false;
            spot.Tower = Instantiate(towerPrefabs[(int)type], spot.transform.position, Quaternion.identity, towerParent);
            counts[(int)type]++;
            Total++;
            return true;
        }

        public bool Remove(BuildSpot spot)
        {
            if (spot == null || spot.IsEmpty) return false;
            counts[(int)spot.Tower.Data.type]--;
            Total--;
            Destroy(spot.Tower.gameObject);
            spot.Tower = null;
            return true;
        }

        public void ClearAll()
        {
            foreach (var spot in spots) Remove(spot);
        }

        void Update()
        {
            var gm = GameManager.Instance;
            bool buildPhase = gm != null && gm.Phase == GamePhase.Build;

            hovered = buildPhase ? SpotUnderMouse() : null;
            foreach (var spot in spots) spot.SetHighlight(spot == hovered, buildPhase);
            if (!buildPhase) return;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) Selected = TowerType.Archer;
                if (keyboard.digit2Key.wasPressedThisFrame) Selected = TowerType.Cannon;
                if (keyboard.digit3Key.wasPressedThisFrame) Selected = TowerType.Frost;
            }

            var mouse = Mouse.current;
            if (mouse == null || hovered == null) return;
            if (mouse.leftButton.wasPressedThisFrame) TryPlace(hovered, Selected);
            if (mouse.rightButton.wasPressedThisFrame) Remove(hovered);
        }

        BuildSpot SpotUnderMouse()
        {
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null) return null;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return null;

            Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            Physics2D.OverlapPoint(world, new ContactFilter2D().NoFilter(), hits);
            foreach (var hit in hits)
                if (hit.TryGetComponent(out BuildSpot spot)) return spot;
            return null;
        }
    }
}
