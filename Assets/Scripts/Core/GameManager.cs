using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState
{
    Intro,      // prologue dialogue, the player cannot move
    Playing,    // normal gameplay (HUD running)
    Paused,
    Cutscene,   // staff sequence, the player is controlled by script
    Victory,    // end of level screen
    GameOver
}

/// <summary>
/// Level state machine: Playing / Paused / Victory / Game Over, plus score, timer and HUD.
/// One per level scene (built by Tools > Kai > Setup Game).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool SkipStartMenuOnce { get; set; }

    [Header("Escenas")]
    public string menuSceneName = "MainMenu";
    [Tooltip("Scene loaded after the victory screen. Empty = back to the main menu.")]
    public string nextSceneName = "Nivel2";

    [Header("HUD")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI lifeText;
    public TextMeshProUGUI weaponText;

    [Header("Pausa")]
    public GameObject pausePanel;
    public GameObject optionsPanel;
    public Button resumeButton;
    public Button restartButton;
    public Button optionsButton;
    public Button quitButton;

    [Header("Game Over")]
    public GameObject gameOverPanel;
    [Tooltip("Reiniciar: repeats this level")]
    public Button retryButton;
    [Tooltip("Jugar de nuevo: starts a brand new game from level 1")]
    public Button playAgainButton;
    public Button gameOverMenuButton;

    [Header("Fin del nivel")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryScoreText;
    public TextMeshProUGUI victoryTimeText;
    public Button nextLevelButton;
    [Tooltip("Seconds before the victory screen fades into the next level by itself.")]
    public float victoryAutoDelay = 6f;

    public GameState State { get; private set; } = GameState.Playing;
    public int Score { get; private set; }
    public float ElapsedTime { get; private set; }
    public bool IsPlaying => State == GameState.Playing;
    public bool IsStartMenuOpen { get; private set; }

    public static GameManager EnsureExists()
    {
        if (Instance != null)
            return Instance;

        GameManager existing = FindFirstObjectByType<GameManager>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject("GameManager");
        return go.AddComponent<GameManager>();
    }

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;

        AutoWireIfNeeded();

        string activeScene = SceneManager.GetActiveScene().name;
        bool isFirstLevel = activeScene == "SampleScene" || activeScene == "Nivel1";

        if (SkipStartMenuOnce || !isFirstLevel)
        {
            SkipStartMenuOnce = false;
            IsStartMenuOpen = false;
        }
        else
        {
            IsStartMenuOpen = true;
            MainMenuController.OpenAsOverlay = true;
        }

        Bind(resumeButton, Resume);
        Bind(restartButton, Restart);
        Bind(optionsButton, OpenOptions);
        Bind(quitButton, GoToMenu);
        Bind(retryButton, Restart);
        Bind(playAgainButton, PlayAgain);
        Bind(gameOverMenuButton, GoToMenu);
        Bind(nextLevelButton, GoToNextLevel);
    }

    private void AutoWireIfNeeded()
    {
        GameObject gameUi = GameObject.Find("GameUI");
        if (gameUi == null)
            return;

        Transform root = gameUi.transform;

        if (lifeText == null) lifeText = FindComp<TextMeshProUGUI>(root, "HUD/LifeText");
        if (scoreText == null) scoreText = FindComp<TextMeshProUGUI>(root, "HUD/ScoreText");
        if (timeText == null) timeText = FindComp<TextMeshProUGUI>(root, "HUD/TimeText");
        if (weaponText == null) weaponText = FindComp<TextMeshProUGUI>(root, "HUD/WeaponText");

        if (pausePanel == null) pausePanel = FindGo(root, "PausePanel");
        if (optionsPanel == null) optionsPanel = FindGo(root, "OptionsPanel");
        if (resumeButton == null) resumeButton = FindComp<Button>(root, "PausePanel/ResumeButton");
        if (restartButton == null) restartButton = FindComp<Button>(root, "PausePanel/RestartButton");
        if (optionsButton == null) optionsButton = FindComp<Button>(root, "PausePanel/OptionsButton");
        if (quitButton == null) quitButton = FindComp<Button>(root, "PausePanel/QuitButton");

        if (gameOverPanel == null) gameOverPanel = FindGo(root, "GameOverPanel");
        if (retryButton == null) retryButton = FindComp<Button>(root, "GameOverPanel/RetryButton");
        if (playAgainButton == null) playAgainButton = FindComp<Button>(root, "GameOverPanel/PlayAgainButton");
        if (gameOverMenuButton == null) gameOverMenuButton = FindComp<Button>(root, "GameOverPanel/MenuButton");

        if (victoryPanel == null) victoryPanel = FindGo(root, "VictoryPanel");
        if (victoryScoreText == null) victoryScoreText = FindComp<TextMeshProUGUI>(root, "VictoryPanel/ScoreText");
        if (victoryTimeText == null) victoryTimeText = FindComp<TextMeshProUGUI>(root, "VictoryPanel/TimeText");
        if (nextLevelButton == null) nextLevelButton = FindComp<Button>(root, "VictoryPanel/NextLevelButton");
    }

    private static GameObject FindGo(Transform root, string relativePath)
    {
        Transform t = root.Find(relativePath);
        return t != null ? t.gameObject : null;
    }

    private static T FindComp<T>(Transform root, string relativePath) where T : Component
    {
        Transform t = root.Find(relativePath);
        return t != null ? t.GetComponent<T>() : null;
    }

    private void Start()
    {
        State = GameState.Intro;
        SetActive(pausePanel, false);
        SetActive(optionsPanel, false);
        SetActive(gameOverPanel, false);
        SetActive(victoryPanel, false);

        SaveSystem.SaveScene(SceneManager.GetActiveScene().name);
        RefreshScore();
        RefreshTime();

        if (IsStartMenuOpen)
            StartCoroutine(ShowStartMenuRoutine());
        else
            BeginGame();
    }

    private IEnumerator ShowStartMenuRoutine()
    {
        if (!IsStartMenuOpen)
            yield break;

        if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            BeginGame();
            yield break;
        }

        AsyncOperation load = SceneManager.LoadSceneAsync(menuSceneName, LoadSceneMode.Additive);
        if (load == null)
        {
            IsStartMenuOpen = false;
            BeginGame();
            yield break;
        }

        yield return load;
    }

    public void BeginGame()
    {
        IsStartMenuOpen = false;
        State = GameState.Playing;
        Time.timeScale = 1f;
    }

    public void CloseStartMenu(Scene menuScene)
    {
        StartCoroutine(CloseStartMenuRoutine(menuScene));
    }

    private IEnumerator CloseStartMenuRoutine(Scene menuScene)
    {
        if (menuScene.IsValid() && menuScene.isLoaded)
            yield return SceneManager.UnloadSceneAsync(menuScene);

        BeginGame();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (State == GameState.Playing)
        {
            ElapsedTime += Time.deltaTime;
            RefreshTime();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            OnEscape();
    }

    // ---------- State changes ----------

    public void SetState(GameState state)
    {
        State = state;
    }

    private void OnEscape()
    {
        if (State == GameState.Playing)
        {
            Pause();
        }
        else if (State == GameState.Paused)
        {
            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                optionsPanel.SetActive(false);
                SetActive(pausePanel, true);
            }
            else
            {
                Resume();
            }
        }
    }

    public void Pause()
    {
        if (State != GameState.Playing)
            return;

        State = GameState.Paused;
        Time.timeScale = 0f;
        SetActive(pausePanel, true);
        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.pause : null);
    }

    public void Resume()
    {
        if (State != GameState.Paused)
            return;

        State = GameState.Playing;
        Time.timeScale = 1f;
        SetActive(pausePanel, false);
        SetActive(optionsPanel, false);
    }

    private void OpenOptions()
    {
        SetActive(pausePanel, false);
        SetActive(optionsPanel, true);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        ScreenFader.FadeAndLoad(SceneManager.GetActiveScene().name, 0.6f);
    }

    /// <summary>Restarts from level 1 without repeating the introductory dialogue.</summary>
    public void PlayAgain()
    {
        Time.timeScale = 1f;
        SaveSystem.PrologueSeen = true;
        SaveSystem.SaveScene(SaveSystem.FirstLevelScene);
        SkipStartMenuOnce = true;
        ScreenFader.FadeAndLoad(SaveSystem.FirstLevelScene, 0.8f);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        ScreenFader.FadeAndLoad(menuSceneName, 0.8f);
    }

    public void OnPlayerDied()
    {
        if (State == GameState.GameOver || State == GameState.Victory)
            return;

        State = GameState.GameOver;
        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        yield return new WaitForSecondsRealtime(1.3f);

        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.gameOver : null);
        SetActive(gameOverPanel, true);
    }

    public void CompleteLevel()
    {
        if (State == GameState.Victory || State == GameState.GameOver)
            return;

        State = GameState.Victory;
        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.victory : null);

        if (!string.IsNullOrEmpty(nextSceneName))
            SaveSystem.SaveScene(nextSceneName);

        if (victoryScoreText != null)
            victoryScoreText.text = "Puntaje: " + Score;
        if (victoryTimeText != null)
            victoryTimeText.text = "Tiempo: " + FormatTime(ElapsedTime);

        SetActive(victoryPanel, true);
        StartCoroutine(VictoryRoutine());
    }

    private IEnumerator VictoryRoutine()
    {
        yield return new WaitForSecondsRealtime(victoryAutoDelay);
        GoToNextLevel();
    }

    private void GoToNextLevel()
    {
        StopAllCoroutines();
        Time.timeScale = 1f;

        // Slow fade to black, then the next level (Mario Bros style).
        ScreenFader.FadeAndLoad(string.IsNullOrEmpty(nextSceneName) ? menuSceneName : nextSceneName, 2.2f);
    }

    // ---------- Score and HUD ----------

    public void AddScore(int amount)
    {
        Score = Mathf.Max(0, Score + amount);
        RefreshScore();
    }

    public void SetPlayerHealth(int current, int max)
    {
        if (lifeText != null)
            lifeText.text = "Vida: " + current + "/" + max;
    }

    public void SetPlayerWeapon(string weaponName)
    {
        if (weaponText != null)
            weaponText.text = "Arma: " + weaponName;
    }

    private void RefreshScore()
    {
        if (scoreText != null)
            scoreText.text = "Puntaje: " + Score;
    }

    private void RefreshTime()
    {
        if (timeText != null)
            timeText.text = "Tiempo: " + FormatTime(ElapsedTime);
    }

    public static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    // ---------- Helpers ----------

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
