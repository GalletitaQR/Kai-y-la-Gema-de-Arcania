using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the main menu (Canvas, four panels, settings controls and MenuController)
/// in the Menu scene. Run it from Tools > Kai > Build Main Menu.
/// Running it again replaces the previous Canvas and MenuController.
/// </summary>
public static class MainMenuBuilder
{
    private const string MenuScenePath = "Assets/Scenes/Menu.unity";
    private const string GameScenePath = "Assets/Scenes/Nivel1.unity";
    private const string GameSceneName = "Nivel1";

    private const string UiRoot = "Assets/FreeButtonSet/Textures/";
    private const string PrefabFolder = "Assets/Prefabs";
    private const string ButtonPrefabPath = PrefabFolder + "/MenuButton.prefab";
    private const string FillSpriteFolder = "Assets/Sprites/UI";
    private const string FillSpritePath = FillSpriteFolder + "/bar_fill.png";
    private const string MusicClipPath = "Assets/Audio/prettyjohn1-halloween-background-music-620625.mp3";

    // Palette
    private const string PanelHex = "0A3D4A";
    private const string CreamHex = "E8F4F4";
    private const string TitleCreamHex = "FFF1D0";
    private const string GoldHex = "F0C14B";
    private const string AccentHex = "3ECFCF";
    private const string PressedHex = "A0A0A0";
    private const string PlaceholderHex = "88AAAA";
    private const string IconDarkHex = "0A3D4A";

    private static readonly Vector2 FieldSize = new Vector2(560f, 55f);
    private static int UiLayer => LayerMask.NameToLayer("UI");

    [MenuItem("Tools/Kai/Build Main Menu")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play mode before building the menu.");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Build Main Menu",
                "This replaces the Canvas and MenuManager/MenuController of the Menu scene with the new menu, and saves the scene.\n\nContinue?",
                "Build", "Cancel"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

        PrepareSprites();
        Sprite fillSprite = EnsureFillSprite();
        RemoveOldMenu(scene);

        // ----- Canvas -----
        GameObject canvasGo = NewUI("Canvas", null, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Build Main Menu");
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // ----- Panels -----
        GameObject mainPanel = CreatePanel(canvasGo.transform, "MainPanel");
        GameObject settingsPanel = CreatePanel(canvasGo.transform, "SettingsPanel");
        GameObject scoresPanel = CreatePanel(canvasGo.transform, "ScoresPanel");
        GameObject creditsPanel = CreatePanel(canvasGo.transform, "CreditsPanel");

        Button[] menuButtons = BuildMainPanel(mainPanel);
        Button settingsBack = BuildSettingsPanel(settingsPanel, fillSprite,
            out TMP_InputField nameField, out TMP_Dropdown qualityDropdown,
            out Slider brightnessSlider, out Toggle fullscreenToggle);
        Button scoresBack = BuildScoresPanel(scoresPanel);
        Button creditsBack = BuildCreditsPanel(creditsPanel);

        // Brightness overlay: last sibling so it is drawn above the panels.
        GameObject overlayGo = NewUI("BrightnessOverlay", canvasGo.transform, typeof(Image));
        Stretch(overlayGo.GetComponent<RectTransform>());
        Image overlay = overlayGo.GetComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.15f);
        overlay.raycastTarget = false;

        // ----- Scene support objects -----
        EnsureEventSystem();
        EnsureMusicPlayer();

        // ----- MenuController -----
        GameObject controllerGo = new GameObject("MenuController");
        Undo.RegisterCreatedObjectUndo(controllerGo, "Build Main Menu");
        MenuController controller = controllerGo.AddComponent<MenuController>();
        controller.mainPanel = mainPanel;
        controller.settingsPanel = settingsPanel;
        controller.scoresPanel = scoresPanel;
        controller.creditsPanel = creditsPanel;
        controller.menuButtons = menuButtons;
        controller.backButtons = new[] { settingsBack, scoresBack, creditsBack };
        controller.playerNameField = nameField;
        controller.qualityDropdown = qualityDropdown;
        controller.brightnessSlider = brightnessSlider;
        controller.fullscreenToggle = fullscreenToggle;
        controller.brightnessOverlay = overlay;
        controller.gameSceneName = GameSceneName;

        foreach (GameObject panel in new[] { mainPanel, settingsPanel, scoresPanel, creditsPanel })
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel.GetComponent<RectTransform>());

        EnsureBuildScenes(MenuScenePath, GameScenePath);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = controllerGo;

        Debug.Log("Main menu built and saved in " + MenuScenePath);
    }

    // =====================================================================
    // Panels
    // =====================================================================

    private static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = NewUI(name, parent, typeof(Image));
        Stretch(panel.GetComponent<RectTransform>());
        Image image = panel.GetComponent<Image>();
        image.sprite = null;
        image.color = Hex(PanelHex);
        return panel;
    }

    private static Button[] BuildMainPanel(GameObject panel)
    {
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 24f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI title = NewText(panel.transform, "Title", "Kai y la Gema de Arcania", 80f, GoldHex, TextAlignmentOptions.Center);
        title.rectTransform.sizeDelta = new Vector2(1400f, 130f);

        GameObject buttonPrefab = CreateButtonPrefab();

        string[] names = { "PlayButton", "SettingsButton", "ScoresButton", "CreditsButton", "QuitButton" };
        string[] labels = { "Jugar", "Ajustes", "Puntuaciones", "Créditos", "Salir" };
        string[] icons = { "play", "settings", "star", "info", "x" };

        Button[] buttons = new Button[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, panel.transform);
            instance.name = names[i];
            instance.GetComponentInChildren<TextMeshProUGUI>().text = labels[i];
            instance.transform.Find("Icon").GetComponent<Image>().sprite = LoadIcon(icons[i]);
            buttons[i] = instance.GetComponent<Button>();
        }

        return buttons;
    }

    private static Button BuildSettingsPanel(GameObject panel, Sprite fillSprite,
        out TMP_InputField nameField, out TMP_Dropdown qualityDropdown,
        out Slider brightnessSlider, out Toggle fullscreenToggle)
    {
        AddPanelLayout(panel);

        TextMeshProUGUI title = NewText(panel.transform, "Title", "Ajustes", 60f, CreamHex, TextAlignmentOptions.Center);
        AddLayout(title.gameObject, -1f, 80f);

        Button back = CreateBackButton(panel.transform);

        nameField = CreateNameField(panel.transform);
        qualityDropdown = CreateQualityDropdown(panel.transform);

        TextMeshProUGUI brightnessLabel = NewText(panel.transform, "BrightnessLabel", "Brillo", 36f, GoldHex, TextAlignmentOptions.MidlineLeft);
        AddLayout(brightnessLabel.gameObject, FieldSize.x, 50f);

        brightnessSlider = CreateBrightnessSlider(panel.transform, fillSprite);
        fullscreenToggle = CreateFullscreenToggle(panel.transform);

        return back;
    }

    private static Button BuildScoresPanel(GameObject panel)
    {
        AddPanelLayout(panel);

        TextMeshProUGUI title = NewText(panel.transform, "Title", "Mejores puntuaciones", 56f, TitleCreamHex, TextAlignmentOptions.Center);
        AddLayout(title.gameObject, -1f, 80f);

        Button back = CreateBackButton(panel.transform);

        string[] scores =
        {
            "1. JugadorA - 12500",
            "2. JugadorB - 9800",
            "3. JugadorC - 7600",
            "4. JugadorD - 5400"
        };

        for (int i = 0; i < scores.Length; i++)
        {
            TextMeshProUGUI line = NewText(panel.transform, "Score" + (i + 1), scores[i], 36f, CreamHex, TextAlignmentOptions.Center);
            AddLayout(line.gameObject, -1f, 50f);
        }

        return back;
    }

    private static Button BuildCreditsPanel(GameObject panel)
    {
        TextMeshProUGUI text = NewText(panel.transform, "CreditsText",
            "<size=72><color=#" + GoldHex + ">Kai y la Gema de Arcania</color></size>\nCreación de Videojuegos\nUTNG - Décimo cuatrimestre",
            50f, CreamHex, TextAlignmentOptions.Center, true);
        RectTransform rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(1400f, 500f);

        return CreateBackButton(panel.transform);
    }

    private static void AddPanelLayout(GameObject panel)
    {
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(100, 100, 70, 70);
        layout.spacing = 32f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    // =====================================================================
    // Buttons
    // =====================================================================

    private static GameObject CreateButtonPrefab()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        GameObject root = NewUI("MenuButton", null, typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup));
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(580f, 95f);

        Image image = root.GetComponent<Image>();
        image.sprite = LoadSprite("buttons/button_200.png");
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        Button button = root.GetComponent<Button>();
        button.targetGraphic = image;
        ApplyButtonColors(button);

        HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 0, 0);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // Icon goes before the text in the hierarchy.
        GameObject icon = NewUI("Icon", root.transform, typeof(Image));
        Image iconImage = icon.GetComponent<Image>();
        iconImage.sprite = LoadIcon("play");
        iconImage.color = Hex(IconDarkHex);
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        AddLayout(icon, 44f, 44f);

        NewText(root.transform, "Text (TMP)", "Jugar", 56f, IconDarkHex, TextAlignmentOptions.Center);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ButtonPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Button CreateBackButton(Transform parent)
    {
        GameObject go = NewUI("BackButton", parent, typeof(Image), typeof(Button), typeof(LayoutElement));
        go.GetComponent<LayoutElement>().ignoreLayout = true;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(65f, -65f);
        rt.sizeDelta = new Vector2(85f, 85f);

        Image image = go.GetComponent<Image>();
        image.sprite = LoadSprite("buttons/button_round_220.png");
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ApplyButtonColors(button);

        GameObject icon = NewUI("Icon", go.transform, typeof(Image));
        RectTransform iconRt = icon.GetComponent<RectTransform>();
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = Vector2.zero;
        iconRt.sizeDelta = new Vector2(44f, 44f);
        Image iconImage = icon.GetComponent<Image>();
        iconImage.sprite = LoadIcon("arrow_left");
        iconImage.color = Hex(IconDarkHex);
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        return button;
    }

    private static void ApplyButtonColors(Button button)
    {
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Hex(AccentHex);
        colors.pressedColor = Hex(PressedHex);
        colors.fadeDuration = 0.1f;
        button.colors = colors;
    }

    // =====================================================================
    // Settings controls
    // =====================================================================

    private static TMP_InputField CreateNameField(Transform parent)
    {
        GameObject go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        go.name = "PlayerNameInput";
        Adopt(go, parent);

        Image image = go.GetComponent<Image>();
        image.sprite = LoadSprite("controls/universal_panel_20.png");
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        TMP_InputField field = go.GetComponent<TMP_InputField>();
        field.contentType = TMP_InputField.ContentType.Standard;
        field.characterLimit = 16;
        field.customCaretColor = true;
        field.caretColor = Hex(CreamHex);

        field.textViewport.offsetMin = new Vector2(20f, 6f);
        field.textViewport.offsetMax = new Vector2(-20f, -7f);

        field.textComponent.fontSize = 36f;
        field.textComponent.color = Hex(CreamHex);

        TMP_Text placeholder = (TMP_Text)field.placeholder;
        placeholder.text = "Tu nombre...";
        placeholder.fontSize = 32f;
        placeholder.color = Hex(PlaceholderHex);

        AddLayout(go, FieldSize.x, FieldSize.y);
        return field;
    }

    private static TMP_Dropdown CreateQualityDropdown(Transform parent)
    {
        GameObject go = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
        go.name = "QualityDropdown";
        Adopt(go, parent);

        Image image = go.GetComponent<Image>();
        image.sprite = LoadSprite("controls/universal_panel_20.png");
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        TMP_Dropdown dropdown = go.GetComponent<TMP_Dropdown>();
        dropdown.options.Clear();
        foreach (string option in new[] { "Baja", "Media", "Alta", "Ultra" })
            dropdown.options.Add(new TMP_Dropdown.OptionData(option));

        // Caption
        dropdown.captionText.fontSize = 34f;
        dropdown.captionText.color = Hex(CreamHex);
        dropdown.captionText.rectTransform.offsetMin = new Vector2(20f, 6f);
        dropdown.captionText.rectTransform.offsetMax = new Vector2(-60f, -7f);

        // Arrow
        Image arrow = go.transform.Find("Arrow").GetComponent<Image>();
        arrow.sprite = LoadIcon("arrow_down");
        arrow.color = Hex(CreamHex);
        arrow.preserveAspect = true;
        arrow.rectTransform.sizeDelta = new Vector2(32f, 32f);
        arrow.rectTransform.anchoredPosition = new Vector2(-30f, 0f);

        // Template (stays inactive; the dropdown clones it when opened)
        dropdown.template.sizeDelta = new Vector2(0f, 200f);
        Transform item = dropdown.template.Find("Viewport/Content/Item");
        ((RectTransform)item).sizeDelta = new Vector2(0f, 45f);

        dropdown.itemText.text = "Baja";
        dropdown.itemText.fontSize = 30f;
        dropdown.itemText.rectTransform.offsetMin = new Vector2(50f, 1f);
        dropdown.itemText.rectTransform.offsetMax = new Vector2(-10f, -2f);

        Image check = item.Find("Item Checkmark").GetComponent<Image>();
        check.sprite = LoadIcon("checkmark");
        check.color = Hex(IconDarkHex);
        check.preserveAspect = true;
        check.rectTransform.sizeDelta = new Vector2(28f, 28f);
        check.rectTransform.anchoredPosition = new Vector2(24f, 0f);

        dropdown.SetValueWithoutNotify(2); // Alta
        dropdown.RefreshShownValue();

        AddLayout(go, FieldSize.x, FieldSize.y);
        return dropdown;
    }

    private static Slider CreateBrightnessSlider(Transform parent, Sprite fillSprite)
    {
        GameObject root = NewUI("BrightnessSlider", parent, typeof(Slider));
        AddLayout(root, FieldSize.x, 48f);

        // Background
        GameObject background = NewUI("Background", root.transform, typeof(Image));
        RectTransform bgRt = background.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.5f);
        bgRt.anchorMax = new Vector2(1f, 0.5f);
        bgRt.anchoredPosition = Vector2.zero;
        bgRt.sizeDelta = new Vector2(0f, 16f);
        Image bgImage = background.GetComponent<Image>();
        bgImage.sprite = LoadSprite("controls/universal_panel_20.png");
        bgImage.type = Image.Type.Sliced;
        bgImage.color = Color.white;

        // Fill (ends at the handle centre: 20 px margin on each side)
        GameObject fillArea = NewUI("Fill Area", root.transform);
        RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRt.anchoredPosition = Vector2.zero;
        fillAreaRt.sizeDelta = new Vector2(-40f, 16f);

        GameObject fill = NewUI("Fill", fillArea.transform, typeof(Image));
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchoredPosition = Vector2.zero;
        fillRt.sizeDelta = Vector2.zero;
        Image fillImage = fill.GetComponent<Image>();
        fillImage.sprite = fillSprite;
        fillImage.type = Image.Type.Sliced;
        fillImage.color = Hex(AccentHex);

        // Handle
        GameObject handleArea = NewUI("Handle Slide Area", root.transform);
        RectTransform handleAreaRt = handleArea.GetComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.anchoredPosition = Vector2.zero;
        handleAreaRt.sizeDelta = new Vector2(-40f, 0f);

        GameObject handle = NewUI("Handle", handleArea.transform, typeof(Image));
        RectTransform handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(40f, 0f);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.sprite = LoadSprite("controls/slider_handle.png");
        handleImage.preserveAspect = true;
        handleImage.color = Color.white;

        Slider slider = root.GetComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = 0.8f;

        return slider;
    }

    private static Toggle CreateFullscreenToggle(Transform parent)
    {
        GameObject root = NewUI("FullscreenToggle", parent, typeof(Toggle));
        AddLayout(root, FieldSize.x, 65f);

        GameObject background = NewUI("Background", root.transform, typeof(Image));
        RectTransform bgRt = background.GetComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = new Vector2(0f, 0.5f);
        bgRt.pivot = new Vector2(0f, 0.5f);
        bgRt.anchoredPosition = Vector2.zero;
        bgRt.sizeDelta = new Vector2(60f, 60f);
        Image bgImage = background.GetComponent<Image>();
        bgImage.sprite = LoadSprite("controls/toggle_bg_100.png");
        bgImage.preserveAspect = true;
        bgImage.color = Color.white;

        GameObject checkmark = NewUI("Checkmark", background.transform, typeof(Image));
        RectTransform checkRt = checkmark.GetComponent<RectTransform>();
        checkRt.anchorMin = checkRt.anchorMax = new Vector2(0.5f, 0.5f);
        checkRt.anchoredPosition = Vector2.zero;
        checkRt.sizeDelta = new Vector2(34f, 34f);
        Image checkImage = checkmark.GetComponent<Image>();
        checkImage.sprite = LoadIcon("checkmark");
        checkImage.preserveAspect = true;
        checkImage.color = Hex(IconDarkHex);

        TextMeshProUGUI label = NewText(root.transform, "Label", "Pantalla completa", 36f, GoldHex, TextAlignmentOptions.MidlineLeft);
        label.raycastTarget = true; // clicking the label also toggles
        RectTransform labelRt = label.rectTransform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(80f, 0f);
        labelRt.offsetMax = Vector2.zero;

        Toggle toggle = root.GetComponent<Toggle>();
        toggle.transition = Selectable.Transition.ColorTint;
        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;
        toggle.isOn = true;

        return toggle;
    }

    // =====================================================================
    // Scene support
    // =====================================================================

    private static void RemoveOldMenu(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Canvas" || root.name == "MenuManager" || root.name == "MenuController")
                Undo.DestroyObjectImmediate(root);
        }
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            EditorApplication.ExecuteMenuItem("GameObject/UI/Event System");
    }

    private static void EnsureMusicPlayer()
    {
        AudioSource source = UnityEngine.Object.FindAnyObjectByType<AudioSource>();
        if (source == null)
        {
            GameObject go = new GameObject("MusicPlayer");
            Undo.RegisterCreatedObjectUndo(go, "Build Main Menu");
            source = go.AddComponent<AudioSource>();
        }

        source.loop = true;
        source.playOnAwake = true;
        if (source.clip == null)
            source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicClipPath);
    }

    private static void EnsureBuildScenes(params string[] paths)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        for (int i = 0; i < paths.Length; i++)
        {
            string path = paths[i];
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(Mathf.Min(i, scenes.Count), new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // =====================================================================
    // Sprites
    // =====================================================================

    /// <summary>Makes sure the sliced sprites are Sprite (2D and UI) with their 9-slice borders.</summary>
    private static void PrepareSprites()
    {
        EnsureSprite("buttons/button_200.png", new Vector4(20f, 20f, 20f, 20f));
        EnsureSprite("buttons/button_round_220.png", new Vector4(20f, 20f, 20f, 20f));
        EnsureSprite("controls/universal_panel_20.png", new Vector4(25f, 25f, 25f, 25f));
        EnsureSprite("controls/toggle_bg_100.png", null);
        EnsureSprite("controls/slider_handle.png", null);

        foreach (string icon in new[] { "play", "settings", "star", "info", "x", "arrow_left", "arrow_down", "checkmark" })
            EnsureSprite("icons/64x64/" + icon + ".png", null);
    }

    private static void EnsureSprite(string relativePath, Vector4? border)
    {
        string path = UiRoot + relativePath;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("Sprite not found: " + path);
            return;
        }

        bool changed = false;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }

        if (border.HasValue && importer.spriteBorder != border.Value)
        {
            importer.spriteBorder = border.Value;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }

    /// <summary>Creates a small white rounded sprite (9-sliced) used for the slider fill, so it can be tinted.</summary>
    private static Sprite EnsureFillSprite()
    {
        if (!File.Exists(FillSpritePath))
        {
            const int size = 32;
            const float radius = 12f;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float cx = Mathf.Clamp(px, radius, size - radius);
                    float cy = Mathf.Clamp(py, radius, size - radius);
                    float distance = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                    float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            Directory.CreateDirectory(FillSpriteFolder);
            File.WriteAllBytes(FillSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(FillSpritePath);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(FillSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(12f, 12f, 12f, 12f);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(FillSpritePath);
    }

    private static Sprite LoadSprite(string relativePath)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiRoot + relativePath);
        if (sprite == null)
            Debug.LogWarning("Sprite not found: " + UiRoot + relativePath);
        return sprite;
    }

    private static Sprite LoadIcon(string name)
    {
        return LoadSprite("icons/64x64/" + name + ".png");
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private static GameObject NewUI(string name, Transform parent, params System.Type[] components)
    {
        List<System.Type> types = new List<System.Type> { typeof(RectTransform) };
        types.AddRange(components);

        GameObject go = new GameObject(name, types.ToArray());
        go.layer = UiLayer;
        if (parent != null)
            go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI NewText(Transform parent, string name, string text, float size,
        string colorHex, TextAlignmentOptions alignment, bool wrap = false)
    {
        GameObject go = NewUI(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = size;
        label.color = Hex(colorHex);
        label.alignment = alignment;
        label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }

    /// <summary>Re-parents a control created by TMP_DefaultControls and puts it on the UI layer.</summary>
    private static void Adopt(GameObject go, Transform parent)
    {
        go.transform.SetParent(parent, false);
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = UiLayer;
    }

    private static void AddLayout(GameObject go, float preferredWidth, float preferredHeight)
    {
        LayoutElement element = go.GetComponent<LayoutElement>();
        if (element == null)
            element = go.AddComponent<LayoutElement>();

        if (preferredWidth >= 0f)
            element.preferredWidth = preferredWidth;
        if (preferredHeight >= 0f)
            element.preferredHeight = preferredHeight;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }
}
