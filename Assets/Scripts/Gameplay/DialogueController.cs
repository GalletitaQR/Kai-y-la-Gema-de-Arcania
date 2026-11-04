using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea(2, 4)]
    public string text;
    [Tooltip("Optional picture shown above the dialogue box (portal images).")]
    public Sprite illustration;
}

/// <summary>
/// Prologue of level 1: the Guardian of Arcania explains the threat to Kai.
/// Space / click = next line (or finish typing), Tab = skip. It only plays the first time (SaveSystem.PrologueSeen).
/// When it ends the tutorial starts.
/// </summary>
public class DialogueController : MonoBehaviour
{
    private const string PlayerName = "Kai";

    [Header("UI")]
    public GameObject panel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI bodyText;
    public Image portrait;
    public Image illustration;

    [Header("Referencias")]
    public MentorNPC mentor;
    public TutorialController tutorial;

    [Header("Texto")]
    public float charactersPerSecond = 45f;

    [Header("Guion del prologo")]
    public DialogueLine[] lines =
    {
        new DialogueLine { speaker = "Guardián de Arcanía", text = "Kai... por fin despiertas. Te esperaba en este bosque encantado." },
        new DialogueLine { speaker = "Kai", text = "¿Guardián? ¿Qué ocurre? El aire vibra y el bosque se siente extraño." },
        new DialogueLine { speaker = "Guardián de Arcanía", text = "Un portal de magos oscuros se está abriendo sobre Arcanía. Si termina de abrirse, nuestro mundo mágico será destruido." },
        new DialogueLine { speaker = "Guardián de Arcanía", text = "Yo debo quedarme aquí, vigilando el portal para que ninguna criatura salga. No puedo abandonar mi puesto." },
        new DialogueLine { speaker = "Kai", text = "Entonces... ¿quién puede detenerlos?" },
        new DialogueLine { speaker = "Guardián de Arcanía", text = "Solo quien posea el poder de la Gema de Arcanía puede salvar a nuestro mundo. Pero la gema está rota en varios fragmentos, ocultos en pruebas a lo largo del reino." },
        new DialogueLine { speaker = "Guardián de Arcanía", text = "Cada nivel es una prueba. Supéralas, reúne los fragmentos y la gema recobrará su poder. Los corazones del bosque te devolverán la vida." },
        new DialogueLine { speaker = "Kai", text = "Lo haré. No dejaré que Arcanía caiga." },
        new DialogueLine { speaker = "Guardián de Arcanía", text = "Ve, joven aprendiz. El primer fragmento te espera al final del bosque. ¡Cuidado con las criaturas!" }
    };

    private bool skipRequested;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            skipRequested = true;
    }

    private void Start()
    {
        ConfigureFullScreenIllustration();
        SetPanel(false);
        StartCoroutine(BeginAfterStartMenu());
    }

    private void ConfigureFullScreenIllustration()
    {
        if (illustration == null)
            return;

        RectTransform rect = illustration.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        illustration.preserveAspect = false;
    }

    private IEnumerator BeginAfterStartMenu()
    {
        while (GameManager.Instance != null && GameManager.Instance.IsStartMenuOpen)
            yield return null;

        bool playNewGame = MainMenuController.StartNewGameRequested;
        MainMenuController.ResetNewGameRequest();

        if ((!playNewGame && SaveSystem.PrologueSeen) || lines == null || lines.Length == 0)
        {
            if (tutorial != null)
                tutorial.SkipWithoutShowing();
            if (GameManager.Instance != null)
                GameManager.Instance.BeginGame();
            yield break;
        }

        yield return StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Intro);

        yield return new WaitForSeconds(0.6f);
        SetPanel(true);

        foreach (DialogueLine line in lines)
        {
            ShowLine(line);
            yield return TypeLine(line.text);

            // Wait for the player to continue.
            yield return null;
            while (!AdvancePressed() && !skipRequested)
                yield return null;

            if (skipRequested)
                break;
        }

        SetPanel(false);
        SaveSystem.PrologueSeen = true;

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Playing);

        if (tutorial != null)
            tutorial.Begin();
    }

    private void ShowLine(DialogueLine line)
    {
        if (speakerText != null)
            speakerText.text = line.speaker;

        if (portrait != null)
        {
            if (mentor != null && mentor.Portrait != null)
            {
                portrait.sprite = mentor.Portrait;
                portrait.gameObject.SetActive(true);
            }
        }

        if (illustration != null)
        {
            bool hasPicture = line.illustration != null;
            if (hasPicture)
            {
                illustration.sprite = line.illustration;
                illustration.gameObject.SetActive(true);
            }
        }
    }

    private IEnumerator TypeLine(string text)
    {
        bodyText.text = text;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();

        int total = bodyText.textInfo.characterCount;
        float shown = 0f;

        // One frame so the key press that opened this line is not read again.
        yield return null;

        while (shown < total)
        {
            if (AdvancePressed() || skipRequested)
                break;

            shown += charactersPerSecond * Time.unscaledDeltaTime;
            bodyText.maxVisibleCharacters = Mathf.FloorToInt(shown);
            yield return null;
        }

        bodyText.maxVisibleCharacters = total;
    }

    private static bool AdvancePressed()
    {
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
    }

    private void SetPanel(bool visible)
    {
        if (panel != null)
            panel.SetActive(visible);
    }
}
