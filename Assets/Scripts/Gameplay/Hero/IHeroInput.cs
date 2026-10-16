using UnityEngine;
using UnityEngine.InputSystem;

namespace Gatekeeper
{
    public interface IHeroInput
    {
        Vector2 Move { get; }
        bool AttackHeld { get; }
    }

    /// <summary>WASD / arrows to move, Space or J to attack.</summary>
    public sealed class KeyboardHeroInput : IHeroInput
    {
        public Vector2 Move
        {
            get
            {
                var k = Keyboard.current;
                if (k == null) return Vector2.zero;
                var v = Vector2.zero;
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1f;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1f;
                if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1f;
                return v.sqrMagnitude > 1f ? v.normalized : v;
            }
        }

        public bool AttackHeld
        {
            get
            {
                var k = Keyboard.current;
                return k != null && (k.spaceKey.isPressed || k.jKey.isPressed);
            }
        }
    }
}
