using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Joystick virtual en pantalla + botones táctiles/mouse (Saltar, Atacar, Cambiar Arma).
/// Se construye automáticamente por código sin necesidad de configurar prefabs manualmente.
/// </summary>
public class VirtualControlsUI : MonoBehaviour
{
    public static VirtualControlsUI Instance { get; private set; }

    public float HorizontalAxis => joystick != null ? joystick.Horizontal : 0f;

    public bool JumpPressedThisFrame { get; private set; }
    public bool AttackPressedThisFrame { get; private set; }
    public bool SwitchWeaponPressedThisFrame { get; private set; }
    public bool InteractPressedThisFrame { get; private set; }

    private CanvasGroup canvasGroup;
    private VirtualJoystickZone joystick;
    private GameObject weaponButtonObj;
    private GameObject interactButtonObj;
    private TextMeshProUGUI interactButtonLabel;
    private int interactInterestCount = 0;
    private static Sprite circleSprite;

    public static VirtualControlsUI EnsureExists()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject("VirtualControlsCanvas");
        return go.AddComponent<VirtualControlsUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureEventSystem();
        BuildUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void LateUpdate()
    {
        // Limpiar los pulsos de un solo frame al final del ciclo
        JumpPressedThisFrame = false;
        AttackPressedThisFrame = false;
        SwitchWeaponPressedThisFrame = false;
        InteractPressedThisFrame = false;

        // Mostrar los controles solo cuando Kai está jugando activamente
        bool visible = true;
        if (GameManager.Instance != null)
        {
            visible = GameManager.Instance.State == GameState.Playing && !GameManager.Instance.IsStartMenuOpen;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        if (weaponButtonObj != null)
        {
            weaponButtonObj.SetActive(SaveSystem.HasStaff);
        }

        if (interactButtonObj != null)
        {
            interactButtonObj.SetActive(interactInterestCount > 0);
        }
    }

    /// <summary>
    /// Muestra o solicita el botón táctil de interacción (puertas, palancas, etc.).
    /// </summary>
    public static void RequestInteractButton(bool show, string customLabel = "ENTRAR")
    {
        if (Instance == null) return;

        if (show)
        {
            Instance.interactInterestCount++;
            if (Instance.interactButtonLabel != null && !string.IsNullOrEmpty(customLabel))
            {
                Instance.interactButtonLabel.text = customLabel;
            }
        }
        else
        {
            Instance.interactInterestCount = Mathf.Max(0, Instance.interactInterestCount - 1);
        }
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90; // Debajo de pausas/menús pero encima del juego

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Sprite knobSprite = GetCircleSprite();

        // ---------------- 1. JOYSTICK IZQUIERDO ----------------
        GameObject joyBgObj = new GameObject("JoystickBackground");
        joyBgObj.transform.SetParent(transform, false);
        RectTransform joyBgRect = joyBgObj.AddComponent<RectTransform>();
        joyBgRect.anchorMin = new Vector2(0f, 0f);
        joyBgRect.anchorMax = new Vector2(0f, 0f);
        joyBgRect.pivot = new Vector2(0.5f, 0.5f);
        joyBgRect.anchoredPosition = new Vector2(220f, 200f);
        joyBgRect.sizeDelta = new Vector2(240f, 240f);

        Image joyBgImg = joyBgObj.AddComponent<Image>();
        joyBgImg.sprite = knobSprite;
        joyBgImg.color = new Color(0.12f, 0.14f, 0.22f, 0.55f);

        // Borde sutil interior
        GameObject joyRingObj = new GameObject("JoystickRing");
        joyRingObj.transform.SetParent(joyBgObj.transform, false);
        RectTransform joyRingRect = joyRingObj.AddComponent<RectTransform>();
        joyRingRect.anchorMin = new Vector2(0.5f, 0.5f);
        joyRingRect.anchorMax = new Vector2(0.5f, 0.5f);
        joyRingRect.sizeDelta = new Vector2(210f, 210f);
        Image joyRingImg = joyRingObj.AddComponent<Image>();
        joyRingImg.sprite = knobSprite;
        joyRingImg.color = new Color(0.45f, 0.55f, 0.85f, 0.18f);
        joyRingImg.raycastTarget = false;

        // Palanca central (Handle)
        GameObject handleObj = new GameObject("JoystickHandle");
        handleObj.transform.SetParent(joyBgObj.transform, false);
        RectTransform handleRect = handleObj.AddComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(105f, 105f);

        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.sprite = knobSprite;
        handleImg.color = new Color(0.85f, 0.90f, 1f, 0.80f);
        handleImg.raycastTarget = false;

        // Etiqueta indicadora "◄ ►"
        CreateLabel(handleObj.transform, "◄ ►", 26f, new Color(0.15f, 0.2f, 0.35f, 0.9f));

        joystick = joyBgObj.AddComponent<VirtualJoystickZone>();
        joystick.background = joyBgRect;
        joystick.handle = handleRect;
        joystick.maxRadius = 80f;

        // ---------------- 2. BOTONES DE ACCIÓN (DERECHA) ----------------
        // Botón ATACAR (X)
        CreateActionButton(
            "BtnAttack",
            "ATACAR\n(X)",
            new Vector2(-330f, 170f),
            new Vector2(150f, 150f),
            new Color(0.85f, 0.25f, 0.30f, 0.70f),
            () => AttackPressedThisFrame = true
        );

        // Botón SALTAR (Espacio)
        CreateActionButton(
            "BtnJump",
            "SALTAR",
            new Vector2(-150f, 230f),
            new Vector2(150f, 150f),
            new Color(0.20f, 0.65f, 0.95f, 0.70f),
            () => JumpPressedThisFrame = true
        );

        // Botón CAMBIAR ARMA (C)
        weaponButtonObj = CreateActionButton(
            "BtnWeapon",
            "ARMA\n(C)",
            new Vector2(-210f, 395f),
            new Vector2(115f, 115f),
            new Color(0.65f, 0.30f, 0.90f, 0.70f),
            () => SwitchWeaponPressedThisFrame = true
        );

        // Botón INTERACTUAR / ACCIÓN (Táctil para Puertas y Palancas)
        interactButtonObj = CreateActionButton(
            "BtnInteract",
            "ENTRAR\n(E)",
            new Vector2(-360f, 345f),
            new Vector2(120f, 120f),
            new Color(0.20f, 0.82f, 0.45f, 0.85f),
            () => InteractPressedThisFrame = true
        );
        interactButtonLabel = interactButtonObj.GetComponentInChildren<TextMeshProUGUI>();
        interactButtonObj.SetActive(false);
    }

    private GameObject CreateActionButton(
        string name,
        string label,
        Vector2 anchoredPos,
        Vector2 size,
        Color baseColor,
         System.Action onPress)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(transform, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.sprite = GetCircleSprite();
        img.color = baseColor;

        VirtualActionButton btnTrigger = btnObj.AddComponent<VirtualActionButton>();
        btnTrigger.targetGraphic = img;
        btnTrigger.normalColor = baseColor;
        btnTrigger.pressedColor = new Color(
            Mathf.Clamp01(baseColor.r + 0.2f),
            Mathf.Clamp01(baseColor.g + 0.2f),
            Mathf.Clamp01(baseColor.b + 0.2f),
            0.95f
        );
        btnTrigger.onPointerDown = onPress;

        CreateLabel(btnObj.transform, label, 24f, Color.white);
        return btnObj;
    }

    private void CreateLabel(Transform parent, string text, float fontSize, Color color)
    {
        GameObject txtObj = new GameObject("Label");
        txtObj.transform.SetParent(parent, false);
        RectTransform rect = txtObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
            return circleSprite;

        const int res = 128;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = (res - 1) * 0.5f;
        float radius = center - 2f;

        Color[] pixels = new Color[res * res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(radius - dist + 1f);
                pixels[y * res + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100f);
        return circleSprite;
    }
}

/// <summary>
/// Zona interactiva de arrastre para el joystick virtual.
/// </summary>
public class VirtualJoystickZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform background;
    public RectTransform handle;
    public float maxRadius = 80f;

    public Vector2 InputVector { get; private set; }
    public float Horizontal => Mathf.Abs(InputVector.x) > 0.15f ? Mathf.Sign(InputVector.x) : 0f;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null) return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
        {
            Vector2 clamped = Vector2.ClampMagnitude(localPoint, maxRadius);
            handle.anchoredPosition = clamped;
            InputVector = clamped / maxRadius;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        InputVector = Vector2.zero;
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }

    private void OnDisable()
    {
        InputVector = Vector2.zero;
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }
}

/// <summary>
/// Botón táctil inmediato al presionar (OnPointerDown) en vez de esperar a soltar el clic.
/// </summary>
public class VirtualActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Image targetGraphic;
    public Color normalColor = Color.white;
    public Color pressedColor = Color.gray;
    public System.Action onPointerDown;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (targetGraphic != null)
            targetGraphic.color = pressedColor;
        onPointerDown?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (targetGraphic != null)
            targetGraphic.color = normalColor;
    }

    private void OnDisable()
    {
        if (targetGraphic != null)
            targetGraphic.color = normalColor;
    }
}

