using UnityEngine;
using UnityEngine.UI;

/// <summary>Plays the "buttonClick" sound of the AudioLibrary. Added automatically to every Button.</summary>
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayClick);
    }

    private static void PlayClick()
    {
        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.buttonClick : null);
    }
}
