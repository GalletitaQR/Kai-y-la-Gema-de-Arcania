using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida en pantalla para el Jefe Final del Nivel 4 (Dragón Guardián + Mage Esqueleto).
/// Se construye o enlaza automáticamente dentro del Canvas principal al iniciar la pelea.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    public static BossHealthBarUI Instance { get; private set; }

    [Header("Referencias UI (Se crean automáticamente si están vacías)")]
    public GameObject panelRoot;
    public Image fillImage;
    public TextMeshProUGUI bossTitleText;
    public TextMeshProUGUI phaseStatusText;

    void Awake()
    {
        Instance = this;
        EnsureUIBuilt();
        SetVisible(false);
    }

    private void EnsureUIBuilt()
    {
        if (panelRoot != null && fillImage != null) return;

        Canvas targetCanvas = FindFirstObjectByType<Canvas>();
        if (targetCanvas == null) return;

        panelRoot = new GameObject("BossHealthBarPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelRoot.transform.SetParent(targetCanvas.transform, false);

        RectTransform panelRt = panelRoot.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 1f);
        panelRt.anchorMax = new Vector2(0.5f, 1f);
        panelRt.pivot = new Vector2(0.5f, 1f);
        panelRt.anchoredPosition = new Vector2(0f, -22f);
        panelRt.sizeDelta = new Vector2(640f, 78f);

        Image bgImg = panelRoot.GetComponent<Image>();
        bgImg.color = new Color(0.06f, 0.04f, 0.10f, 0.88f);

        // Fondo de la barra
        GameObject barBg = new GameObject("BarBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barBg.transform.SetParent(panelRoot.transform, false);
        RectTransform barBgRt = barBg.GetComponent<RectTransform>();
        barBgRt.anchorMin = new Vector2(0f, 0f);
        barBgRt.anchorMax = new Vector2(1f, 0f);
        barBgRt.pivot = new Vector2(0.5f, 0f);
        barBgRt.anchoredPosition = new Vector2(0f, 12f);
        barBgRt.sizeDelta = new Vector2(-36f, 24f);
        barBg.GetComponent<Image>().color = new Color(0.18f, 0.08f, 0.12f, 1f);

        // Relleno de la barra
        GameObject barFill = new GameObject("BarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barFill.transform.SetParent(barBg.transform, false);
        RectTransform fillRt = barFill.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.offsetMin = new Vector2(2f, 2f);
        fillRt.offsetMax = new Vector2(-2f, -2f);

        fillImage = barFill.GetComponent<Image>();
        fillImage.color = new Color(0.92f, 0.18f, 0.18f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 1f;

        // Título del Jefe
        GameObject titleGo = new GameObject("BossTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(panelRoot.transform, false);
        RectTransform titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -6f);
        titleRt.sizeDelta = new Vector2(-24f, 24f);

        bossTitleText = titleGo.GetComponent<TextMeshProUGUI>();
        bossTitleText.text = "DRAGÓN GUARDIÁN DE ARCANIA";
        bossTitleText.fontSize = 19f;
        bossTitleText.fontStyle = FontStyles.Bold;
        bossTitleText.alignment = TextAlignmentOptions.Center;
        bossTitleText.color = new Color(1f, 0.9f, 0.55f, 1f);

        // Subtítulo de Fase
        GameObject phaseGo = new GameObject("PhaseStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        phaseGo.transform.SetParent(panelRoot.transform, false);
        RectTransform phaseRt = phaseGo.GetComponent<RectTransform>();
        phaseRt.anchorMin = new Vector2(0f, 1f);
        phaseRt.anchorMax = new Vector2(1f, 1f);
        phaseRt.pivot = new Vector2(0.5f, 1f);
        phaseRt.anchoredPosition = new Vector2(0f, -27f);
        phaseRt.sizeDelta = new Vector2(-24f, 18f);

        phaseStatusText = phaseGo.GetComponent<TextMeshProUGUI>();
        phaseStatusText.text = "";
        phaseStatusText.fontSize = 14f;
        phaseStatusText.alignment = TextAlignmentOptions.Center;
        phaseStatusText.color = new Color(0.85f, 0.85f, 0.95f, 1f);
    }

    public void SetVisible(bool visible)
    {
        EnsureUIBuilt();
        if (panelRoot != null)
        {
            panelRoot.SetActive(visible);
        }
    }

    public void UpdateBossHealth(int current, int max, bool isVulnerable, string phaseLabel)
    {
        EnsureUIBuilt();
        if (fillImage != null)
        {
            float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            fillImage.fillAmount = ratio;
            fillImage.color = isVulnerable
                ? new Color(0.25f, 0.92f, 1f, 1f)
                : new Color(0.92f, 0.18f, 0.18f, 1f);
        }

        if (bossTitleText != null)
        {
            bossTitleText.text = $"DRAGÓN GUARDIÁN DE ARCANIA ({Mathf.Max(0, current)}/{max})";
        }

        if (phaseStatusText != null && !string.IsNullOrEmpty(phaseLabel))
        {
            phaseStatusText.text = phaseLabel;
        }
    }
}

