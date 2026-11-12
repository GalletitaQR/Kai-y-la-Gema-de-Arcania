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

        Canvas targetCanvas = null;
        if (MessageBanner.Instance != null)
            targetCanvas = MessageBanner.Instance.GetComponentInParent<Canvas>();
        if (targetCanvas == null)
        {
            foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c.renderMode != RenderMode.WorldSpace && c.isActiveAndEnabled)
                {
                    targetCanvas = c;
                    CanvasScaler cs = c.GetComponent<CanvasScaler>();
                    if (cs != null && cs.referenceResolution.x >= 1280f)
                        break;
                }
            }
        }
        if (targetCanvas == null) return;

        panelRoot = new GameObject("BossHealthBarPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        panelRoot.transform.SetParent(targetCanvas.transform, false);

        RectTransform panelRt = panelRoot.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 1f);
        panelRt.anchorMax = new Vector2(0.5f, 1f);
        panelRt.pivot = new Vector2(0.5f, 1f);
        panelRt.anchoredPosition = new Vector2(0f, -24f);
        panelRt.sizeDelta = new Vector2(920f, 122f);

        Image bgImg = panelRoot.GetComponent<Image>();
        bgImg.color = new Color(0.06f, 0.04f, 0.12f, 0.92f);
        Outline panelOutline = panelRoot.GetComponent<Outline>();
        panelOutline.effectColor = new Color(0.92f, 0.72f, 0.28f, 0.85f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        // Fondo de la barra
        GameObject barBg = new GameObject("BarBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        barBg.transform.SetParent(panelRoot.transform, false);
        RectTransform barBgRt = barBg.GetComponent<RectTransform>();
        barBgRt.anchorMin = new Vector2(0f, 0f);
        barBgRt.anchorMax = new Vector2(1f, 0f);
        barBgRt.pivot = new Vector2(0.5f, 0f);
        barBgRt.anchoredPosition = new Vector2(0f, 14f);
        barBgRt.sizeDelta = new Vector2(-44f, 30f);
        barBg.GetComponent<Image>().color = new Color(0.18f, 0.07f, 0.12f, 1f);
        Outline barOutline = barBg.GetComponent<Outline>();
        barOutline.effectColor = new Color(0.02f, 0.02f, 0.04f, 0.95f);
        barOutline.effectDistance = new Vector2(2f, -2f);

        // Relleno de la barra
        GameObject barFill = new GameObject("BarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barFill.transform.SetParent(barBg.transform, false);
        RectTransform fillRt = barFill.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.offsetMin = new Vector2(3f, 3f);
        fillRt.offsetMax = new Vector2(-3f, -3f);

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
        titleRt.anchoredPosition = new Vector2(0f, -8f);
        titleRt.sizeDelta = new Vector2(-30f, 34f);

        bossTitleText = titleGo.GetComponent<TextMeshProUGUI>();
        bossTitleText.text = "DRAGÓN GUARDIÁN DE ARCANIA";
        bossTitleText.fontSize = 28f;
        bossTitleText.fontStyle = FontStyles.Bold;
        bossTitleText.alignment = TextAlignmentOptions.Center;
        bossTitleText.color = new Color(1f, 0.90f, 0.50f, 1f);
        bossTitleText.outlineWidth = 0.22f;
        bossTitleText.outlineColor = new Color32(15, 8, 24, 230);

        // Subtítulo de Fase
        GameObject phaseGo = new GameObject("PhaseStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        phaseGo.transform.SetParent(panelRoot.transform, false);
        RectTransform phaseRt = phaseGo.GetComponent<RectTransform>();
        phaseRt.anchorMin = new Vector2(0f, 1f);
        phaseRt.anchorMax = new Vector2(1f, 1f);
        phaseRt.pivot = new Vector2(0.5f, 1f);
        phaseRt.anchoredPosition = new Vector2(0f, -43f);
        phaseRt.sizeDelta = new Vector2(-30f, 30f);

        phaseStatusText = phaseGo.GetComponent<TextMeshProUGUI>();
        phaseStatusText.text = "";
        phaseStatusText.fontSize = 22f;
        phaseStatusText.fontStyle = FontStyles.Bold;
        phaseStatusText.alignment = TextAlignmentOptions.Center;
        phaseStatusText.color = new Color(0.95f, 0.95f, 1f, 1f);
        phaseStatusText.outlineWidth = 0.20f;
        phaseStatusText.outlineColor = new Color32(10, 6, 18, 225);
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
            phaseStatusText.color = isVulnerable
                ? new Color(0.35f, 1f, 0.92f, 1f)
                : new Color(1f, 0.92f, 0.72f, 1f);
        }
    }
}

