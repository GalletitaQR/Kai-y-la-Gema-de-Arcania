using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;

    [Header("Botones del menú principal")]
    public Button playButton;
    public Button optionsButton;
    public Button creditsButton;
    public Button quitButton;

    [Header("Botones volver")]
    public Button[] backButtons;

    [Header("Audio")]
    public Slider volumeSlider;
    public Toggle muteToggle;
    public AudioSource musicPlayer;

    [Header("Escena del juego")]
    public string gameSceneName = "MenuPractica";

    private void Start()
    {
        ConfigureButtons();
        ConfigureAudio();
        Show(mainPanel);
    }

    private void ConfigureButtons()
    {
        if (playButton != null)
            playButton.onClick.AddListener(Play);

        if (optionsButton != null)
            optionsButton.onClick.AddListener(() => Show(optionsPanel));

        if (creditsButton != null)
            creditsButton.onClick.AddListener(() => Show(creditsPanel));

        if (quitButton != null)
            quitButton.onClick.AddListener(Quit);

        if (backButtons != null)
        {
            foreach (Button backButton in backButtons)
            {
                if (backButton != null)
                    backButton.onClick.AddListener(() => Show(mainPanel));
            }
        }
    }

    private void ConfigureAudio()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(SetVolume);
            SetVolume(volumeSlider.value);
        }

        if (muteToggle != null)
        {
            muteToggle.onValueChanged.AddListener(SetMusicOn);
            SetMusicOn(muteToggle.isOn);
        }
    }

    private void Show(GameObject panel)
    {
        if (mainPanel != null)
            mainPanel.SetActive(panel == mainPanel);

        if (optionsPanel != null)
            optionsPanel.SetActive(panel == optionsPanel);

        if (creditsPanel != null)
            creditsPanel.SetActive(panel == creditsPanel);
    }

    private void Play()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    private void Quit()
    {
        Debug.Log("Salir");
        Application.Quit();
    }

    private void SetVolume(float value)
    {
        if (musicPlayer != null)
            musicPlayer.volume = value;
    }

    private void SetMusicOn(bool on)
    {
        if (musicPlayer != null)
            musicPlayer.mute = !on;
    }
}