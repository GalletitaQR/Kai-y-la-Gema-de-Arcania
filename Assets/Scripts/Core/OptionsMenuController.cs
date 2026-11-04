using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Options panel: music volume, SFX volume, graphics quality and the controls list.
/// Works in the main menu and in the pause menu.
/// </summary>
public class OptionsMenuController : MonoBehaviour
{
    private const string QualityKey = "QualityOption";
    private const int DefaultQualityOption = 2; // Alta

    public Slider musicSlider;
    public Slider sfxSlider;
    public TMP_Dropdown qualityDropdown;
    public Button backButton;
    [Tooltip("Panel shown again when Back is pressed (main menu or pause panel).")]
    public GameObject returnPanel;

    private bool bound;

    private void Awake()
    {
        if (backButton != null)
            backButton.onClick.AddListener(Back);
    }

    private void OnEnable()
    {
        Bind();
        SyncFromSettings();
    }

    private void Bind()
    {
        if (bound)
            return;

        bound = true;

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetMusicVolume(v));

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetSfxVolume(v));
        }

        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(SetQuality);
    }

    private void SyncFromSettings()
    {
        if (AudioManager.Instance != null)
        {
            if (musicSlider != null)
                musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
            if (sfxSlider != null)
                sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
        }

        if (qualityDropdown != null)
        {
            int option = Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, DefaultQualityOption), 0, qualityDropdown.options.Count - 1);
            qualityDropdown.SetValueWithoutNotify(option);
            qualityDropdown.RefreshShownValue();
        }
    }

    private void SetQuality(int option)
    {
        PlayerPrefs.SetInt(QualityKey, option);
        ApplyQuality(option, qualityDropdown.options.Count);
    }

    /// <summary>Spreads the dropdown options evenly over the project's quality levels.</summary>
    public static void ApplyQuality(int option, int optionCount)
    {
        int levels = QualitySettings.names.Length;
        int level = (optionCount > 1 && levels > 1)
            ? Mathf.RoundToInt(option / (float)(optionCount - 1) * (levels - 1))
            : 0;

        QualitySettings.SetQualityLevel(level, true);
    }

    private void Back()
    {
        gameObject.SetActive(false);
        if (returnPanel != null)
            returnPanel.SetActive(true);
    }
}
