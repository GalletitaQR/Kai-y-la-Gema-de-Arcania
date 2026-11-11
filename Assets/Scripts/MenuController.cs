using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls navigation between the main menu panels and the settings
/// (player name, graphics quality, brightness and fullscreen).
/// </summary>
public class MenuController : MonoBehaviour
{
    // Indexes of the buttons inside the menuButtons array.
    private const int PlayIndex = 0;
    private const int SettingsIndex = 1;
    private const int ScoresIndex = 2;
    private const int CreditsIndex = 3;
    private const int QuitIndex = 4;

    private const string PlayerNameKey = "PlayerName";
    private const string QualityKey = "QualityOption";
    private const string BrightnessKey = "Brightness";
    private const string FullscreenKey = "Fullscreen";

    private const int DefaultQualityOption = 2;      // Alta
    private const float DefaultBrightness = 0.8f;
    private const float MaxDimAlpha = 0.75f;         // Overlay opacity when brightness is 0

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject scoresPanel;
    public GameObject creditsPanel;

    [Header("Buttons")]
    [Tooltip("Order: Play, Settings, Scores, Credits, Quit")]
    public Button[] menuButtons;
    public Button[] backButtons;

    [Header("Settings")]
    public TMP_InputField playerNameField;
    public TMP_Dropdown qualityDropdown;
    public Slider brightnessSlider;
    public Toggle fullscreenToggle;
    [Tooltip("Black full-screen Image (raycast disabled) used to simulate brightness.")]
    public Image brightnessOverlay;

    [Header("Game")]
    [Tooltip("Exact name of the game scene (it must be in the Build Profiles scene list).")]
    public string gameSceneName = "Nivel1";

    private void Start()
    {
        BindButtons();
        BindSettings();
        LoadSettings();
        ShowPanel(mainPanel);
    }

    // ---------- Navigation ----------

    private void BindButtons()
    {
        Bind(PlayIndex, Play);
        Bind(SettingsIndex, () => ShowPanel(settingsPanel));
        Bind(ScoresIndex, () => ShowPanel(scoresPanel));
        Bind(CreditsIndex, () => ShowPanel(creditsPanel));
        Bind(QuitIndex, Quit);

        if (backButtons == null)
            return;

        foreach (Button back in backButtons)
        {
            if (back != null)
                back.onClick.AddListener(() => ShowPanel(mainPanel));
        }
    }

    private void Bind(int index, UnityAction action)
    {
        if (menuButtons != null && index < menuButtons.Length && menuButtons[index] != null)
            menuButtons[index].onClick.AddListener(action);
    }

    private void ShowPanel(GameObject target)
    {
        SetActive(mainPanel, target == mainPanel);
        SetActive(settingsPanel, target == settingsPanel);
        SetActive(scoresPanel, target == scoresPanel);
        SetActive(creditsPanel, target == creditsPanel);
    }

    private static void SetActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    private void Play()
    {
        SavePlayerName();
        PlayerPrefs.Save();

        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError($"Scene '{gameSceneName}' is not in the Build Profiles scene list.");
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- Settings ----------

    private void BindSettings()
    {
        if (playerNameField != null)
            playerNameField.onEndEdit.AddListener(_ => SavePlayerName());

        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(SetQuality);

        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener(SetBrightness);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
    }

    private void LoadSettings()
    {
        if (playerNameField != null)
            playerNameField.SetTextWithoutNotify(PlayerPrefs.GetString(PlayerNameKey, string.Empty));

        if (qualityDropdown != null)
        {
            int option = PlayerPrefs.GetInt(QualityKey, DefaultQualityOption);
            option = Mathf.Clamp(option, 0, qualityDropdown.options.Count - 1);
            qualityDropdown.SetValueWithoutNotify(option);
            qualityDropdown.RefreshShownValue();
            ApplyQuality(option);
        }

        if (brightnessSlider != null)
        {
            float brightness = PlayerPrefs.GetFloat(BrightnessKey, DefaultBrightness);
            brightnessSlider.SetValueWithoutNotify(brightness);
            ApplyBrightness(brightness);
        }

        if (fullscreenToggle != null)
        {
            bool fullscreen = PlayerPrefs.HasKey(FullscreenKey)
                ? PlayerPrefs.GetInt(FullscreenKey) == 1
                : Screen.fullScreen;
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);

            if (PlayerPrefs.HasKey(FullscreenKey))
                Screen.fullScreen = fullscreen;
        }
    }

    private void SavePlayerName()
    {
        if (playerNameField != null)
            PlayerPrefs.SetString(PlayerNameKey, playerNameField.text.Trim());
    }

    private void SetQuality(int option)
    {
        ApplyQuality(option);
        PlayerPrefs.SetInt(QualityKey, option);
    }

    private void ApplyQuality(int option)
    {
        // The dropdown has fewer options than the project has quality levels,
        // so the option is spread evenly across the available levels.
        int levels = QualitySettings.names.Length;
        int options = qualityDropdown.options.Count;
        int level = (options > 1 && levels > 1)
            ? Mathf.RoundToInt(option / (float)(options - 1) * (levels - 1))
            : 0;

        QualitySettings.SetQualityLevel(level, true);
    }

    private void SetBrightness(float value)
    {
        ApplyBrightness(value);
        PlayerPrefs.SetFloat(BrightnessKey, value);
    }

    private void ApplyBrightness(float value)
    {
        if (brightnessOverlay != null)
            brightnessOverlay.color = new Color(0f, 0f, 0f, (1f - value) * MaxDimAlpha);
    }

    private void SetFullscreen(bool isOn)
    {
        Screen.fullScreen = isOn;
        PlayerPrefs.SetInt(FullscreenKey, isOn ? 1 : 0);
    }
}
