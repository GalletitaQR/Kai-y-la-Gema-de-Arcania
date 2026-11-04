using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Main menu: Jugar, Continuar, Opciones, Creditos, Salir.</summary>
public class MainMenuController : MonoBehaviour
{
    public static bool OpenAsOverlay { get; set; }
    public static bool StartNewGameRequested { get; private set; }

    public static void ResetNewGameRequest()
    {
        StartNewGameRequested = false;
    }

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;

    [Header("Buttons")]
    public Button playButton;
    public Button continueButton;
    public Button optionsButton;
    public Button creditsButton;
    public Button quitButton;
    public Button creditsBackButton;

    [Header("Escenas")]
    public string firstLevelScene = "SampleScene";

    private bool isOverlay;

    private void Start()
    {
        isOverlay = OpenAsOverlay;
        OpenAsOverlay = false;

        Bind(playButton, Play);
        Bind(continueButton, Continue);
        Bind(optionsButton, () => Show(optionsPanel));
        Bind(creditsButton, () => Show(creditsPanel));
        Bind(quitButton, Quit);
        Bind(creditsBackButton, () => Show(mainPanel));

        if (continueButton != null)
            continueButton.interactable = SaveSystem.HasSave;

        Time.timeScale = 1f;
        Show(mainPanel);
    }

    private void Show(GameObject target)
    {
        SetActive(mainPanel, target == mainPanel);
        SetActive(optionsPanel, target == optionsPanel);
        SetActive(creditsPanel, target == creditsPanel);
    }

    private void Play()
    {
        StartNewGameRequested = true;
        SaveSystem.NewGame();

        if (isOverlay)
        {
            CloseOverlayAndStart();
            return;
        }

        ScreenFader.FadeAndLoad(firstLevelScene, 1f);
    }

    private void Continue()
    {
        StartNewGameRequested = false;

        if (isOverlay)
        {
            CloseOverlayAndStart();
            return;
        }

        string scene = SaveSystem.SavedScene;
        if (!Application.CanStreamedLevelBeLoaded(scene))
            scene = firstLevelScene;

        ScreenFader.FadeAndLoad(scene, 1f);
    }

    private void CloseOverlayAndStart()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CloseStartMenu(gameObject.scene);
            return;
        }

        ScreenFader.FadeAndLoad(firstLevelScene, 1f);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null)
            go.SetActive(active);
    }
}
