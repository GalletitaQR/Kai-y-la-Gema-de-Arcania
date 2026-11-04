using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Small helpers shared by the scene builders (Tools > Kai > Setup Game).</summary>
public static class UiKit
{
    public const string FreeButtons = "Assets/FreeButtonSet/Textures/";
    public const string FillSpritePath = "Assets/Sprites/UI/bar_fill.png";

    public const string PanelHex = "0A3D4A";
    public const string CreamHex = "E8F4F4";
    public const string GoldHex = "F0C14B";
    public const string TitleCreamHex = "FFF1D0";
    public const string DarkHex = "0A3D4A";

    public static int UiLayer => LayerMask.NameToLayer("UI");

    // ------------------------------------------------------------------ basics

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }

    public static GameObject New(string name, Transform parent, params System.Type[] components)
    {
        List<System.Type> types = new List<System.Type> { typeof(RectTransform) };
        types.AddRange(components);

        GameObject go = new GameObject(name, types.ToArray());
        go.layer = UiLayer;
        if (parent != null)
            go.transform.SetParent(parent, false);
        return go;
    }

    public static RectTransform Rect(GameObject go)
    {
        return (RectTransform)go.transform;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>Single-point anchor: place and size an element relative to one anchor.</summary>
    public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    public static void AddLayout(GameObject go, float preferredWidth, float preferredHeight)
    {
        LayoutElement element = go.GetComponent<LayoutElement>();
        if (element == null)
            element = go.AddComponent<LayoutElement>();

        if (preferredWidth >= 0f)
            element.preferredWidth = preferredWidth;
        if (preferredHeight >= 0f)
            element.preferredHeight = preferredHeight;
    }

    public static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning("Sprite not found: " + path);
        return sprite;
    }

    public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, string colorHex,
        TextAlignmentOptions alignment, bool wrap = false, FontStyles style = FontStyles.Normal)
    {
        GameObject go = New(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = size;
        label.color = Hex(colorHex);
        label.alignment = alignment;
        label.fontStyle = style;
        label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }

    public static Canvas NewCanvas(string name, int sortingOrder)
    {
        GameObject go = New(name, null, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    /// <summary>Full-screen colored image that also blocks clicks to whatever is behind.</summary>
    public static Image Dim(Transform parent, string name, Color color)
    {
        GameObject go = New(name, parent, typeof(Image));
        Stretch(Rect(go));
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    public static VerticalLayoutGroup VerticalLayout(GameObject go, int padding, float spacing, TextAnchor alignment, bool controlChildren)
    {
        VerticalLayoutGroup layout = go.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.spacing = spacing;
        layout.childAlignment = alignment;
        layout.childControlWidth = controlChildren;
        layout.childControlHeight = controlChildren;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return layout;
    }

    // ------------------------------------------------------------------ buttons

    /// <summary>Button that is just one of the supplied PNGs (the text is part of the picture).</summary>
    public static Button ImageButton(Transform parent, string name, Sprite sprite, Vector2 size)
    {
        GameObject go = New(name, parent, typeof(Image), typeof(Button));
        Rect(go).sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        TintColors(button);
        return button;
    }

    /// <summary>Orange sliced button of the UI set with a dark label.</summary>
    public static Button TextButton(Transform parent, string name, string label, Vector2 size)
    {
        GameObject go = New(name, parent, typeof(Image), typeof(Button));
        Rect(go).sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.sprite = LoadSprite(FreeButtons + "buttons/button_200.png");
        image.type = Image.Type.Sliced;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        TintColors(button);

        TextMeshProUGUI text = Text(go.transform, "Text (TMP)", label, 44f, DarkHex, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        return button;
    }

    /// <summary>Normal is slightly dim so hovering (white) reads as brighter.</summary>
    private static void TintColors(Button button)
    {
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Hex("DDDDDD");
        colors.highlightedColor = Color.white;
        colors.pressedColor = Hex("A8A8A8");
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.55f);
        colors.fadeDuration = 0.1f;
        button.colors = colors;
    }

    // ------------------------------------------------------------------ slider / dropdown

    public static Slider NewSlider(Transform parent, string name, float width, float value)
    {
        Sprite bar = LoadSprite(FreeButtons + "controls/universal_panel_20.png");
        Sprite handleSprite = LoadSprite(FreeButtons + "controls/slider_handle.png");
        Sprite fillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FillSpritePath);

        GameObject root = New(name, parent, typeof(Slider));
        AddLayout(root, width, 48f);

        GameObject background = New("Background", root.transform, typeof(Image));
        RectTransform bgRt = Rect(background);
        bgRt.anchorMin = new Vector2(0f, 0.5f);
        bgRt.anchorMax = new Vector2(1f, 0.5f);
        bgRt.anchoredPosition = Vector2.zero;
        bgRt.sizeDelta = new Vector2(0f, 16f);
        Image bgImage = background.GetComponent<Image>();
        bgImage.sprite = bar;
        bgImage.type = Image.Type.Sliced;

        GameObject fillArea = New("Fill Area", root.transform);
        RectTransform fillAreaRt = Rect(fillArea);
        fillAreaRt.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRt.anchoredPosition = Vector2.zero;
        fillAreaRt.sizeDelta = new Vector2(-40f, 16f);

        GameObject fill = New("Fill", fillArea.transform, typeof(Image));
        RectTransform fillRt = Rect(fill);
        fillRt.anchoredPosition = Vector2.zero;
        fillRt.sizeDelta = Vector2.zero;
        Image fillImage = fill.GetComponent<Image>();
        fillImage.sprite = fillSprite;
        fillImage.type = Image.Type.Sliced;
        fillImage.color = Hex("3ECFCF");

        GameObject handleArea = New("Handle Slide Area", root.transform);
        RectTransform handleAreaRt = Rect(handleArea);
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.anchoredPosition = Vector2.zero;
        handleAreaRt.sizeDelta = new Vector2(-40f, 0f);

        GameObject handle = New("Handle", handleArea.transform, typeof(Image));
        RectTransform handleRt = Rect(handle);
        handleRt.sizeDelta = new Vector2(40f, 0f);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.sprite = handleSprite;
        handleImage.preserveAspect = true;

        Slider slider = root.GetComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = value;
        return slider;
    }

    public static TMP_Dropdown NewDropdown(Transform parent, string name, string[] options, int value, float width)
    {
        Sprite panel = LoadSprite(FreeButtons + "controls/universal_panel_20.png");

        GameObject go = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
        go.name = name;
        go.transform.SetParent(parent, false);
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = UiLayer;

        Image image = go.GetComponent<Image>();
        image.sprite = panel;
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        TMP_Dropdown dropdown = go.GetComponent<TMP_Dropdown>();
        dropdown.options.Clear();
        foreach (string option in options)
            dropdown.options.Add(new TMP_Dropdown.OptionData(option));

        dropdown.captionText.fontSize = 34f;
        dropdown.captionText.color = Hex(CreamHex);
        dropdown.captionText.rectTransform.offsetMin = new Vector2(20f, 6f);
        dropdown.captionText.rectTransform.offsetMax = new Vector2(-60f, -7f);

        Image arrow = go.transform.Find("Arrow").GetComponent<Image>();
        arrow.sprite = LoadSprite(FreeButtons + "icons/64x64/arrow_down.png");
        arrow.color = Hex(CreamHex);
        arrow.preserveAspect = true;
        arrow.rectTransform.sizeDelta = new Vector2(32f, 32f);
        arrow.rectTransform.anchoredPosition = new Vector2(-30f, 0f);

        dropdown.template.sizeDelta = new Vector2(0f, 200f);
        Transform item = dropdown.template.Find("Viewport/Content/Item");
        ((RectTransform)item).sizeDelta = new Vector2(0f, 45f);

        dropdown.itemText.text = options.Length > 0 ? options[0] : "";
        dropdown.itemText.fontSize = 30f;
        dropdown.itemText.rectTransform.offsetMin = new Vector2(50f, 1f);
        dropdown.itemText.rectTransform.offsetMax = new Vector2(-10f, -2f);

        Image check = item.Find("Item Checkmark").GetComponent<Image>();
        check.sprite = LoadSprite(FreeButtons + "icons/64x64/checkmark.png");
        check.color = Hex(DarkHex);
        check.preserveAspect = true;
        check.rectTransform.sizeDelta = new Vector2(28f, 28f);
        check.rectTransform.anchoredPosition = new Vector2(24f, 0f);

        dropdown.SetValueWithoutNotify(value);
        dropdown.RefreshShownValue();

        AddLayout(go, width, 55f);
        return dropdown;
    }

    // ------------------------------------------------------------------ options panel

    /// <summary>
    /// Options window: music volume, SFX volume, graphics quality and the controls list.
    /// Used by the main menu and by the pause menu. Returns the panel (inactive).
    /// </summary>
    public static OptionsMenuController OptionsPanel(Transform parent, GameObject returnPanel)
    {
        GameObject panel = New("OptionsPanel", parent);
        Stretch(Rect(panel));
        Image dim = panel.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.8f);

        GameObject window = New("Window", panel.transform, typeof(Image));
        Place(Rect(window), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 880f));
        Image windowImage = window.GetComponent<Image>();
        windowImage.color = Hex(PanelHex);
        VerticalLayout(window, 36, 14f, TextAnchor.UpperCenter, true);

        TextMeshProUGUI title = Text(window.transform, "Title", "OPCIONES", 64f, GoldHex, TextAlignmentOptions.Center, false, FontStyles.Bold);
        AddLayout(title.gameObject, -1f, 76f);

        Slider music = LabeledSlider(window.transform, "Volumen de la música", 0.7f);
        Slider sfx = LabeledSlider(window.transform, "Volumen de efectos (SFX)", 1f);

        TextMeshProUGUI qualityLabel = Text(window.transform, "QualityLabel", "Calidad gráfica", 34f, GoldHex, TextAlignmentOptions.MidlineLeft);
        AddLayout(qualityLabel.gameObject, 800f, 42f);
        TMP_Dropdown quality = NewDropdown(window.transform, "QualityDropdown", new[] { "Baja", "Media", "Alta", "Ultra" }, 2, 800f);

        TextMeshProUGUI controlsLabel = Text(window.transform, "ControlsLabel", "Controles", 34f, GoldHex, TextAlignmentOptions.MidlineLeft);
        AddLayout(controlsLabel.gameObject, 800f, 42f);
        TextMeshProUGUI controls = Text(window.transform, "ControlsText",
            "Mover: flechas ← → o A / D\nSaltar: ESPACIO\nAtacar: X\nPausa: ESC",
            32f, CreamHex, TextAlignmentOptions.TopLeft, true);
        AddLayout(controls.gameObject, 800f, 150f);

        Button back = TextButton(window.transform, "BackButton", "Volver", new Vector2(320f, 80f));
        AddLayout(back.gameObject, 320f, 80f);

        OptionsMenuController controller = panel.AddComponent<OptionsMenuController>();
        controller.musicSlider = music;
        controller.sfxSlider = sfx;
        controller.qualityDropdown = quality;
        controller.backButton = back;
        controller.returnPanel = returnPanel;

        panel.SetActive(false);
        return controller;
    }

    private static Slider LabeledSlider(Transform parent, string label, float value)
    {
        TextMeshProUGUI text = Text(parent, label.Replace(" ", "") + "Label", label, 34f, GoldHex, TextAlignmentOptions.MidlineLeft);
        AddLayout(text.gameObject, 800f, 42f);
        return NewSlider(parent, label.Replace(" ", "") + "Slider", 800f, value);
    }
}
