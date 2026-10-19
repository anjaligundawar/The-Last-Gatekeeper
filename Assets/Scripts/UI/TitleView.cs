using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Gatekeeper
{
    /// <summary>Start screen: game name, Start and How to Play. The game waits here until Start.</summary>
    public class TitleView : MonoBehaviour
    {
        [SerializeField] Button startButton;
        [SerializeField] Button howToPlayButton;
        [SerializeField] GameObject howToPlayPanel;
        [SerializeField] Button backButton;

        void Start()
        {
            startButton.onClick.AddListener(() => GameManager.Instance.StartGame());
            howToPlayButton.onClick.AddListener(() => howToPlayPanel.SetActive(true));
            backButton.onClick.AddListener(() => howToPlayPanel.SetActive(false));
            howToPlayPanel.SetActive(false);
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (howToPlayPanel.activeSelf)
            {
                if (k.escapeKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame) howToPlayPanel.SetActive(false);
            }
            else if (k.enterKey.wasPressedThisFrame) GameManager.Instance.StartGame();
            else if (k.hKey.wasPressedThisFrame) howToPlayPanel.SetActive(true);
        }
    }
}
