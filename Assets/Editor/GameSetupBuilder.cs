using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-click setup of everything in the rubric: Splash, Main Menu, Level 1 (HUD, pause, game over, victory,
/// prologue, tutorial, mentor, gem fragment, hearts, staff) and Level 2. Tools > Kai > Setup Game.
/// </summary>
public static class GameSetupBuilder
{
    // Scenes
    private const string SplashPath = "Assets/Scenes/Splash.unity";
    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    private const string Level1Path = "Assets/Scenes/SampleScene.unity";
    private const string Level2Path = "Assets/Scenes/Nivel2.unity";

    // Art supplied by the team
    private const string ArtDir = "Assets/assets/assets/";
    private const string GeneratedDir = "Assets/Sprites/Generated/";
    private const string HeartPath = GeneratedDir + "heart.png";
    private const string StaffPath = GeneratedDir + "staff.png";

    private static Sprite Art(string file) { return UiKit.LoadSprite(ArtDir + file); }

    // =====================================================================
    // Entry points
    // =====================================================================

    [MenuItem("Tools/Kai/Setup Game (todo el juego)")]
    public static void SetupAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Sal del modo Play antes de ejecutar el setup.");
            return;
        }

        if (!EditorUtility.DisplayDialog("Setup Game",
                "Esto va a:\n" +
                "- crear las escenas Splash y MainMenu,\n" +
                "- agregar a SampleScene (Nivel 1) el HUD, pausa, game over, victoria, prologo, tutorial, mago, bastón y fragmento de gema,\n" +
                "- cambiar las frutas por corazones,\n" +
                "- crear Nivel2 (solo si no existe) y registrar las escenas en Build Settings.\n\n" +
                "Se guardan las escenas. Haz commit antes si quieres poder volver atrás.\n\n¿Continuar?",
                "Continuar", "Cancelar"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        try
        {
            PrepareAssets();
            BuildSplash();
            BuildMainMenu();
            SetupLevel1();
            BuildLevel2(false);
            EnsureBuildScenes();

            EditorSceneManager.OpenScene(SplashPath, OpenSceneMode.Single);
            Debug.Log("Setup terminado. Abre Splash y pulsa Play. Arrastra tus sonidos a Assets/Resources/AudioLibrary.asset");
            EditorUtility.DisplayDialog("Setup Game", "Listo.\n\nAbre la escena Splash y pulsa Play.\nLos sonidos se arrastran en Assets/Resources/AudioLibrary.asset", "OK");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("Setup Game", "Algo falló: " + e.Message + "\n\nRevisa la Console.", "OK");
        }
    }

    [MenuItem("Tools/Kai/Rehacer Nivel 2 (borra Nivel2 y lo vuelve a generar)")]
    public static void RebuildLevel2()
    {
        if (!EditorUtility.DisplayDialog("Rehacer Nivel 2", "Se borra Nivel2.unity y se genera de nuevo a partir de SampleScene. ¿Continuar?", "Continuar", "Cancelar"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        BuildLevel2(true);
        EnsureBuildScenes();
    }

    // =====================================================================
    // Assets
    // =====================================================================

    private static void PrepareAssets()
    {
        foreach (string file in new[] { "fondomenu.png", "titulo.png", "Jugar.png", "continuar.png", "Opciones.png",
                     "creditos.png", "Reiniciar.png", "Salir.png", "portalinfecta.png", "portalSedancuenta.png" })
        {
            ImportSprite(ArtDir + file, 100f, null, FilterMode.Bilinear);
        }

        // The wizard sheet: pivot at the feet of the first frame is set at runtime by MentorNPC.
        ImportSprite(ArtDir + "mago (1).png", 100f, null, FilterMode.Bilinear);

        Directory.CreateDirectory(GeneratedDir);
        if (!File.Exists(HeartPath))
        {
            GenerateHeart(HeartPath);
            ImportSprite(HeartPath, 50f, null, FilterMode.Point);
        }

        if (!File.Exists(StaffPath))
        {
            GenerateStaff(StaffPath);
            ImportSprite(StaffPath, 64f, new Vector2(0.5f, 0.3f), FilterMode.Point);
        }

        if (!File.Exists(UiKit.FillSpritePath))
            GenerateBarFill(UiKit.FillSpritePath);

        CreateAudioLibrary();
        AssetDatabase.Refresh();
    }

    private static void ImportSprite(string path, float pixelsPerUnit, Vector2? pivot, FilterMode filter)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("No se pudo importar: " + path);
            return;
        }

        importer.textureType = TextureImporterType.Sprite;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spritePixelsPerUnit = pixelsPerUnit;
        settings.alphaIsTransparency = true;
        settings.mipmapEnabled = false;
        settings.filterMode = filter;

        if (pivot.HasValue)
        {
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot.Value;
        }

        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static void CreateAudioLibrary()
    {
        const string path = "Assets/Resources/AudioLibrary.asset";
        if (AssetDatabase.LoadAssetAtPath<AudioLibrary>(path) != null)
            return;

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<AudioLibrary>(), path);
    }

    private static void GenerateHeart(string path)
    {
        string[] pattern =
        {
            "..XX...XX..",
            ".XXXX.XXXX.",
            "XXXXXXXXXXX",
            "XXXXXXXXXXX",
            "XXXXXXXXXXX",
            ".XXXXXXXXX.",
            "..XXXXXXX..",
            "...XXXXX...",
            "....XXX....",
            ".....X....."
        };

        const int scale = 3;
        const int pad = 1;
        int rows = pattern.Length;
        int cols = pattern[0].Length;
        int width = (cols + pad * 2) * scale;
        int height = (rows + pad * 2) * scale;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] clear = new Color[width * height];
        texture.SetPixels(clear);

        Color fill = new Color(0.86f, 0.16f, 0.24f);
        Color shade = new Color(0.70f, 0.08f, 0.18f);
        Color light = new Color(1f, 0.62f, 0.68f);
        Color outline = new Color(0.35f, 0.03f, 0.10f);

        for (int r = -1; r <= rows; r++)
        {
            for (int c = -1; c <= cols; c++)
            {
                Color? color = null;
                if (IsFill(pattern, r, c))
                {
                    color = r >= 5 ? shade : fill;
                    if ((r == 1 && (c == 2 || c == 3)) || (r == 2 && c == 1))
                        color = light;
                }
                else if (IsFill(pattern, r - 1, c) || IsFill(pattern, r + 1, c) || IsFill(pattern, r, c - 1) || IsFill(pattern, r, c + 1))
                {
                    color = outline;
                }

                if (!color.HasValue)
                    continue;

                for (int dy = 0; dy < scale; dy++)
                {
                    for (int dx = 0; dx < scale; dx++)
                        texture.SetPixel((c + pad) * scale + dx, (rows - 1 - r + pad) * scale + dy, color.Value);
                }
            }
        }

        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static bool IsFill(string[] pattern, int r, int c)
    {
        return r >= 0 && r < pattern.Length && c >= 0 && c < pattern[0].Length && pattern[r][c] == 'X';
    }

    private static void GenerateStaff(string path)
    {
        const int width = 24;
        const int height = 96;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels(new Color[width * height]);

        Color wood = new Color(0.47f, 0.29f, 0.14f);
        Color woodLight = new Color(0.60f, 0.40f, 0.20f);
        Color gold = new Color(0.95f, 0.76f, 0.29f);

        for (int y = 0; y <= 74; y++)
        {
            for (int x = 10; x <= 13; x++)
                texture.SetPixel(x, y, x == 10 ? woodLight : wood);
        }

        for (int y = 72; y <= 76; y++)
        {
            for (int x = 8; x <= 15; x++)
                texture.SetPixel(x, y, gold);
        }

        Vector2 center = new Vector2(12f, 85f);
        Color deep = new Color(0.45f, 0.10f, 0.85f);
        Color bright = new Color(0.85f, 0.60f, 1f);
        Color edge = new Color(0.24f, 0.08f, 0.47f);

        for (int y = 74; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                if (distance > 10f)
                    continue;

                Color color = distance > 8.6f ? edge : Color.Lerp(bright, deep, distance / 8.6f);
                if (Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(9f, 89f)) < 2.6f)
                    color = Color.white;

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static void GenerateBarFill(string path)
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
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius + 0.5f - distance)));
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = new Vector4(12f, 12f, 12f, 12f);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    // =====================================================================
    // Splash
    // =====================================================================

    private static void BuildSplash()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        NewCamera(Color.black);

        Canvas canvas = UiKit.NewCanvas("Canvas", 0);
        UiKit.Dim(canvas.transform, "Background", Color.black);

        GameObject group = UiKit.New("LogoGroup", canvas.transform, typeof(CanvasGroup));
        UiKit.Stretch(UiKit.Rect(group));

        GameObject logo = UiKit.New("TeamLogo", group.transform, typeof(Image));
        UiKit.Place(UiKit.Rect(logo), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(460f, 460f));
        Image logoImage = logo.GetComponent<Image>();
        logoImage.color = new Color(1f, 1f, 1f, 0.12f);
        logoImage.preserveAspect = true;
        logoImage.raycastTarget = false;

        TextMeshProUGUI hint = UiKit.Text(logo.transform, "PlaceholderText", "LOGOTIPO\nDEL EQUIPO", 48f, UiKit.CreamHex, TextAlignmentOptions.Center, true);
        UiKit.Stretch(hint.rectTransform);

        TextMeshProUGUI title = UiKit.Text(group.transform, "GameTitle", "Kai y la Gema de Arcanía", 64f, UiKit.GoldHex, TextAlignmentOptions.Center, false, FontStyles.Bold);
        UiKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -280f), new Vector2(1400f, 90f));

        GameObject controllerGo = new GameObject("SplashController");
        SplashController splash = controllerGo.AddComponent<SplashController>();
        splash.logoGroup = group.GetComponent<CanvasGroup>();
        splash.nextScene = "MainMenu";

        EditorSceneManager.SaveScene(scene, SplashPath);
    }

    // =====================================================================
    // Main menu
    // =====================================================================

    private static void BuildMainMenu()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        NewCamera(Color.black);

        Canvas canvas = UiKit.NewCanvas("Canvas", 0);
        Transform root = canvas.transform;

        // Background art + a soft dark layer so the buttons stand out.
        GameObject background = UiKit.New("Background", root, typeof(Image));
        UiKit.Stretch(UiKit.Rect(background));
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.sprite = Art("fondomenu.png");
        backgroundImage.raycastTarget = false;
        UiKit.Dim(root, "Shade", new Color(0f, 0f, 0f, 0.2f)).raycastTarget = false;

        // Main panel
        GameObject mainPanel = UiKit.New("MainPanel", root);
        UiKit.Stretch(UiKit.Rect(mainPanel));
        UiKit.VerticalLayout(mainPanel, 0, 10f, TextAnchor.MiddleCenter, false);

        GameObject titleGo = UiKit.New("Title", mainPanel.transform, typeof(Image));
        UiKit.Rect(titleGo).sizeDelta = new Vector2(500f, 241f);
        Image titleImage = titleGo.GetComponent<Image>();
        titleImage.sprite = Art("titulo.png");
        titleImage.preserveAspect = true;
        titleImage.raycastTarget = false;

        Vector2 buttonSize = new Vector2(380f, 124f);
        Button play = UiKit.ImageButton(mainPanel.transform, "PlayButton", Art("Jugar.png"), buttonSize);
        Button continueButton = UiKit.ImageButton(mainPanel.transform, "ContinueButton", Art("continuar.png"), buttonSize);
        Button options = UiKit.ImageButton(mainPanel.transform, "OptionsButton", Art("Opciones.png"), buttonSize);
        Button credits = UiKit.ImageButton(mainPanel.transform, "CreditsButton", Art("creditos.png"), buttonSize);
        Button quit = UiKit.ImageButton(mainPanel.transform, "QuitButton", Art("Salir.png"), buttonSize);

        // Options (shared builder) and credits
        OptionsMenuController optionsController = UiKit.OptionsPanel(root, mainPanel);
        GameObject creditsPanel = BuildCreditsPanel(root, out Button creditsBack);

        GameObject controllerGo = new GameObject("MainMenuController");
        MainMenuController controller = controllerGo.AddComponent<MainMenuController>();
        controller.mainPanel = mainPanel;
        controller.optionsPanel = optionsController.gameObject;
        controller.creditsPanel = creditsPanel;
        controller.playButton = play;
        controller.continueButton = continueButton;
        controller.optionsButton = options;
        controller.creditsButton = credits;
        controller.quitButton = quit;
        controller.creditsBackButton = creditsBack;
        controller.firstLevelScene = "SampleScene";

        CreateEventSystem();
        EditorSceneManager.SaveScene(scene, MainMenuPath);
    }

    private static GameObject BuildCreditsPanel(Transform root, out Button back)
    {
        GameObject panel = UiKit.New("CreditsPanel", root, typeof(Image));
        UiKit.Stretch(UiKit.Rect(panel));
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);

        GameObject window = UiKit.New("Window", panel.transform, typeof(Image));
        UiKit.Place(UiKit.Rect(window), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 760f));
        window.GetComponent<Image>().color = UiKit.Hex(UiKit.PanelHex);
        UiKit.VerticalLayout(window, 40, 20f, TextAnchor.UpperCenter, true);

        TextMeshProUGUI title = UiKit.Text(window.transform, "Title", "CRÉDITOS", 64f, UiKit.GoldHex, TextAlignmentOptions.Center, false, FontStyles.Bold);
        UiKit.AddLayout(title.gameObject, -1f, 80f);

        TextMeshProUGUI body = UiKit.Text(window.transform, "Body",
            "<size=48><color=#" + UiKit.TitleCreamHex + ">Kai y la Gema de Arcanía</color></size>\n" +
            "Juego de plataformas y acción 2D\n\n" +
            "Creación de Videojuegos - UTNG\n" +
            "Décimo cuatrimestre\n\n" +
            "Equipo: (escribe aquí los nombres)",
            36f, UiKit.CreamHex, TextAlignmentOptions.Top, true);
        UiKit.AddLayout(body.gameObject, 900f, 400f);

        back = UiKit.TextButton(window.transform, "BackButton", "Volver", new Vector2(320f, 80f));
        UiKit.AddLayout(back.gameObject, 320f, 80f);

        panel.SetActive(false);
        return panel;
    }

    // =====================================================================
    // Level 1
    // =====================================================================

    private static void SetupLevel1()
    {
        Scene scene = EditorSceneManager.OpenScene(Level1Path, OpenSceneMode.Single);

        ReplaceFruitWithHearts();
        AddGemFragmentToPrefab();

        RemoveRoots(scene, name => name == "GameManager" || name == "GameUI" || name == "PrologueSystems"
                                   || name == "Mentor" || name == "StaffPickup");

        GameManager manager = BuildGameUi("Nivel2", out PrologueUi prologueUi);

        // Prologue + tutorial driver
        GameObject systems = new GameObject("PrologueSystems");
        DialogueController prologue = systems.AddComponent<DialogueController>();
        TutorialController tips = systems.AddComponent<TutorialController>();
        prologue.panel = prologueUi.dialoguePanel;
        prologue.speakerText = prologueUi.speaker;
        prologue.bodyText = prologueUi.body;
        prologue.portrait = prologueUi.portrait;
        prologue.illustration = prologueUi.illustration;
        prologue.tutorial = tips;
        tips.panel = prologueUi.tutorialPanel;
        tips.tipText = prologueUi.tip;

        // Portal illustrations from the beginning of the prologue.
        if (prologue.lines != null && prologue.lines.Length > 3)
        {
            prologue.lines[0].illustration = Art("portalSedancuenta.png");
            prologue.lines[1].illustration = Art("portalSedancuenta.png");
            prologue.lines[2].illustration = Art("portalSedancuenta.png");
            prologue.lines[3].illustration = Art("portalinfecta.png");
        }

        MentorNPC mentor = PlaceMentor();
        prologue.mentor = mentor;

        PlaceStaff();
        CreateEventSystemIfMissing();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Nivel 1 listo: " + manager.name);
    }

    private static void ReplaceFruitWithHearts()
    {
        Sprite heart = AssetDatabase.LoadAssetAtPath<Sprite>(HeartPath);
        if (heart == null)
            return;

        foreach (string name in new[] { "Cereza", "ManzanaAmarilla", "ManzanaRoja", "ManzanaVerde" })
        {
            string path = "Assets/Prefabs/" + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                continue;

            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            foreach (SpriteRenderer renderer in prefab.GetComponentsInChildren<SpriteRenderer>(true))
                renderer.sprite = heart;

            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    private static void AddGemFragmentToPrefab()
    {
        const string path = "Assets/Prefabs/GemaVerde.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            return;

        GameObject prefab = PrefabUtility.LoadPrefabContents(path);
        if (prefab.GetComponent<GemFragment>() == null)
            prefab.AddComponent<GemFragment>();

        PrefabUtility.SaveAsPrefabAsset(prefab, path);
        PrefabUtility.UnloadPrefabContents(prefab);
    }

    private static MentorNPC PlaceMentor()
    {
        Sprite sheet = Art("mago (1).png");

        GameObject go = new GameObject("Mentor");
        go.transform.position = new Vector3(-2.4f, -0.75f, 0f);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sheet;
        renderer.sortingLayerName = "Player";
        renderer.sortingOrder = 5;

        MentorNPC npc = go.AddComponent<MentorNPC>();
        npc.sheet = sheet;
        return npc;
    }

    private static void PlaceStaff()
    {
        GameObject zone = new GameObject("StaffPickup");
        zone.transform.position = new Vector3(54.4f, -14f, 0f);

        BoxCollider2D trigger = zone.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(3.4f, 3f);
        trigger.offset = new Vector2(0f, 1.6f);

        GameObject staff = new GameObject("Staff");
        staff.transform.SetParent(zone.transform, false);
        staff.transform.localPosition = new Vector3(0.95f, 1.9f, 0f);

        SpriteRenderer renderer = staff.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(StaffPath);
        renderer.sortingLayerName = "Player";
        renderer.sortingOrder = 20;

        Bobbing bobbing = staff.AddComponent<Bobbing>();
        bobbing.amplitude = 0.12f;
        bobbing.speed = 2f;

        StaffPickup pickup = zone.AddComponent<StaffPickup>();
        pickup.staff = staff.transform;
    }

    // =====================================================================
    // Level 2
    // =====================================================================

    private static void BuildLevel2(bool overwrite)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Level2Path) != null)
        {
            if (!overwrite)
            {
                Debug.Log("Nivel2 ya existe, no se modifica. (Tools > Kai > Rehacer Nivel 2 para regenerarlo)");
                return;
            }

            AssetDatabase.DeleteAsset(Level2Path);
        }

        if (!AssetDatabase.CopyAsset(Level1Path, Level2Path))
            throw new InvalidOperationException("No se pudo copiar SampleScene a Nivel2.");

        Scene scene = EditorSceneManager.OpenScene(Level2Path, OpenSceneMode.Single);

        // Level 1 content that does not belong underground.
        string[] removePrefixes = { "Crab", "Bat", "Cereza", "Manzana", "GemaVerde", "water-animation", "StaffPickup", "Mentor", "PrologueSystems" };
        RemoveRoots(scene, name => Array.Exists(removePrefixes, p => name.StartsWith(p, StringComparison.Ordinal)));

        // Forest scenery is hidden: this level is the cave below.
        string[] forest = { "BG_", "Parallax", "Arbol", "trunk", "Bosque", "Cercanos", "Lejanos", "Media" };
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (Array.Exists(forest, f => root.name.Contains(f)))
                root.SetActive(false);
        }

        // Kai starts in the cave corridor (lower part of level 1).
        PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            Vector3 position = player.transform.position;
            player.transform.position = new Vector3(21.5f, -12.4f, position.z);
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(21.5f, -11.5f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.08f);
        }

        // Darker caves: lower the intensity of the 2D global light (found by type name, no URP reference needed).
        GameObject globalLight = FindRoot(scene, "Global Light 2D");
        if (globalLight != null)
        {
            foreach (Component component in globalLight.GetComponents<Component>())
            {
                if (component == null || component.GetType().Name != "Light2D")
                    continue;

                SerializedObject serialized = new SerializedObject(component);
                SerializedProperty intensity = serialized.FindProperty("m_Intensity");
                if (intensity != null)
                {
                    intensity.floatValue = 0.6f;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        GameManager manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (manager != null)
            manager.nextSceneName = "";

        // Enemies and hearts along the cave floor (floor top is y = -13 for x 20..43).
        SpawnPrefab("Crab", new Vector3(28.8f, -12.5f, 0f));
        SpawnPrefab("Crab", new Vector3(34.6f, -12.5f, 0f));
        SpawnPrefab("Crab", new Vector3(40.2f, -12.5f, 0f));
        SpawnPrefab("Bat", new Vector3(25.5f, -9.2f, 0f));
        SpawnPrefab("Bat", new Vector3(36.5f, -9.2f, 0f));
        SpawnPrefab("Cereza", new Vector3(25f, -12.4f, 0f));
        SpawnPrefab("Cereza", new Vector3(31f, -12.4f, 0f));
        SpawnPrefab("Cereza", new Vector3(38.5f, -12.4f, 0f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SpawnPrefab(string prefabName, Vector3 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + prefabName + ".prefab");
        if (prefab == null)
            return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = position;
    }

    // =====================================================================
    // Game UI (HUD, pause, game over, victory, messages, dialogue, tutorial)
    // =====================================================================

    /// <summary>References to the prologue UI, wired to the DialogueController / TutorialController.</summary>
    private class PrologueUi
    {
        public GameObject dialoguePanel;
        public TextMeshProUGUI speaker;
        public TextMeshProUGUI body;
        public Image portrait;
        public Image illustration;
        public GameObject tutorialPanel;
        public TextMeshProUGUI tip;
    }

    private static GameManager BuildGameUi(string nextScene, out PrologueUi prologueUi)
    {
        GameObject managerGo = new GameObject("GameManager");
        GameManager manager = managerGo.AddComponent<GameManager>();
        manager.menuSceneName = "MainMenu";
        manager.nextSceneName = nextScene;

        Canvas canvas = UiKit.NewCanvas("GameUI", 10);
        Transform ui = canvas.transform;

        // ----- HUD (top right) -----
        GameObject hud = UiKit.New("HUD", ui);
        UiKit.Stretch(UiKit.Rect(hud));
        manager.lifeText = HudText(hud.transform, "LifeText", "Vida: 100/100", 0);
        manager.scoreText = HudText(hud.transform, "ScoreText", "Puntaje: 0", 1);
        manager.timeText = HudText(hud.transform, "TimeText", "Tiempo: 00:00", 2);

        // ----- Message banner -----
        GameObject bannerGo = UiKit.New("MessageBanner", ui, typeof(CanvasGroup), typeof(Image));
        UiKit.Place(UiKit.Rect(bannerGo), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1500f, 170f));
        Image bannerImage = bannerGo.GetComponent<Image>();
        bannerImage.color = new Color(0.04f, 0.1f, 0.16f, 0.9f);
        bannerImage.raycastTarget = false;
        CanvasGroup bannerGroup = bannerGo.GetComponent<CanvasGroup>();
        bannerGroup.alpha = 0f;
        bannerGroup.blocksRaycasts = false;
        bannerGroup.interactable = false;
        TextMeshProUGUI bannerText = UiKit.Text(bannerGo.transform, "Text", "", 42f, UiKit.TitleCreamHex, TextAlignmentOptions.Center, true, FontStyles.Bold);
        UiKit.Stretch(bannerText.rectTransform);
        bannerText.rectTransform.offsetMin = new Vector2(30f, 15f);
        bannerText.rectTransform.offsetMax = new Vector2(-30f, -15f);
        MessageBanner banner = bannerGo.AddComponent<MessageBanner>();
        banner.group = bannerGroup;
        banner.label = bannerText;

        // ----- Tutorial tip + dialogue box -----
        prologueUi = new PrologueUi();
        BuildTutorialPanel(ui, prologueUi);
        BuildDialoguePanel(ui, prologueUi);

        // ----- Pause -----
        Vector2 buttonSize = new Vector2(400f, 130f);
        GameObject pausePanel = MenuPanel(ui, "PausePanel", new Color(0f, 0f, 0f, 0.75f), "PAUSA", 110f, UiKit.TitleCreamHex);
        manager.resumeButton = UiKit.ImageButton(pausePanel.transform, "ResumeButton", Art("continuar.png"), buttonSize);
        manager.restartButton = UiKit.ImageButton(pausePanel.transform, "RestartButton", Art("Reiniciar.png"), buttonSize);
        manager.optionsButton = UiKit.ImageButton(pausePanel.transform, "OptionsButton", Art("Opciones.png"), buttonSize);
        manager.quitButton = UiKit.ImageButton(pausePanel.transform, "QuitButton", Art("Salir.png"), buttonSize);
        manager.pausePanel = pausePanel;
        manager.optionsPanel = UiKit.OptionsPanel(ui, pausePanel).gameObject;
        pausePanel.SetActive(false);

        // ----- Game over -----
        GameObject gameOver = MenuPanel(ui, "GameOverPanel", new Color(0.18f, 0f, 0f, 0.88f), "GAME OVER", 150f, "E53935");
        manager.retryButton = UiKit.ImageButton(gameOver.transform, "RetryButton", Art("Reiniciar.png"), buttonSize);
        manager.playAgainButton = UiKit.ImageButton(gameOver.transform, "PlayAgainButton", Art("Jugar.png"), buttonSize);
        manager.gameOverMenuButton = UiKit.ImageButton(gameOver.transform, "MenuButton", Art("Salir.png"), buttonSize);
        manager.gameOverPanel = gameOver;
        gameOver.SetActive(false);

        // ----- Victory -----
        GameObject victory = MenuPanel(ui, "VictoryPanel", new Color(0f, 0.06f, 0.12f, 0.88f), "¡NIVEL COMPLETADO!", 100f, UiKit.GoldHex);
        manager.victoryScoreText = UiKit.Text(victory.transform, "ScoreText", "Puntaje: 0", 64f, UiKit.CreamHex, TextAlignmentOptions.Center);
        manager.victoryScoreText.rectTransform.sizeDelta = new Vector2(1000f, 84f);
        manager.victoryTimeText = UiKit.Text(victory.transform, "TimeText", "Tiempo: 00:00", 64f, UiKit.CreamHex, TextAlignmentOptions.Center);
        manager.victoryTimeText.rectTransform.sizeDelta = new Vector2(1000f, 84f);
        manager.nextLevelButton = UiKit.ImageButton(victory.transform, "NextLevelButton", Art("continuar.png"), buttonSize);
        manager.victoryPanel = victory;
        victory.SetActive(false);

        return manager;
    }

    private static TextMeshProUGUI HudText(Transform parent, string name, string text, int row)
    {
        TextMeshProUGUI label = UiKit.Text(parent, name, text, 38f, UiKit.CreamHex, TextAlignmentOptions.MidlineRight, false, FontStyles.Bold);
        UiKit.Place(label.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -20f - row * 48f), new Vector2(520f, 44f));
        return label;
    }

    /// <summary>Dim background + big title; children are stacked in the center.</summary>
    private static GameObject MenuPanel(Transform parent, string name, Color dim, string title, float titleSize, string titleColor)
    {
        GameObject panel = UiKit.Dim(parent, name, dim).gameObject;
        UiKit.VerticalLayout(panel, 20, 14f, TextAnchor.MiddleCenter, false);

        TextMeshProUGUI text = UiKit.Text(panel.transform, "Title", title, titleSize, titleColor, TextAlignmentOptions.Center, false, FontStyles.Bold);
        text.rectTransform.sizeDelta = new Vector2(1500f, titleSize * 1.35f);
        return panel;
    }

    private static void BuildTutorialPanel(Transform ui, PrologueUi refs)
    {
        GameObject panel = UiKit.New("TutorialPanel", ui, typeof(Image));
        UiKit.Place(UiKit.Rect(panel), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1250f, 100f));
        Image image = panel.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.65f);
        image.raycastTarget = false;

        TextMeshProUGUI tip = UiKit.Text(panel.transform, "Tip", "", 34f, UiKit.CreamHex, TextAlignmentOptions.Center, true);
        UiKit.Stretch(tip.rectTransform);
        tip.rectTransform.offsetMin = new Vector2(25f, 8f);
        tip.rectTransform.offsetMax = new Vector2(-25f, -8f);

        refs.tutorialPanel = panel;
        refs.tip = tip;
        panel.SetActive(false);
    }

    private static void BuildDialoguePanel(Transform ui, PrologueUi refs)
    {
        GameObject panel = UiKit.New("DialoguePanel", ui);
        UiKit.Stretch(UiKit.Rect(panel));

        // Optional picture above the box (the portal illustrations)
        GameObject illustrationGo = UiKit.New("Illustration", panel.transform, typeof(Image));
        UiKit.Stretch(UiKit.Rect(illustrationGo));
        Image illustration = illustrationGo.GetComponent<Image>();
        illustration.preserveAspect = false;
        illustration.raycastTarget = false;
        illustrationGo.SetActive(false);

        GameObject box = UiKit.New("Box", panel.transform, typeof(Image));
        UiKit.Place(UiKit.Rect(box), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1700f, 300f));
        Image boxImage = box.GetComponent<Image>();
        boxImage.color = new Color(0.04f, 0.1f, 0.16f, 0.92f);
        boxImage.raycastTarget = false;

        GameObject portraitGo = UiKit.New("Portrait", box.transform, typeof(Image));
        UiKit.Place(UiKit.Rect(portraitGo), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(230f, 260f));
        Image portrait = portraitGo.GetComponent<Image>();
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;

        TextMeshProUGUI speaker = UiKit.Text(box.transform, "Speaker", "", 44f, UiKit.GoldHex, TextAlignmentOptions.MidlineLeft, false, FontStyles.Bold);
        UiKit.Place(speaker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, -18f), new Vector2(1300f, 56f));

        TextMeshProUGUI body = UiKit.Text(box.transform, "Body", "", 38f, UiKit.CreamHex, TextAlignmentOptions.TopLeft, true);
        UiKit.Stretch(body.rectTransform);
        body.rectTransform.offsetMin = new Vector2(300f, 55f);
        body.rectTransform.offsetMax = new Vector2(-40f, -80f);

        TextMeshProUGUI hint = UiKit.Text(box.transform, "Hint", "ESPACIO: continuar     TAB: saltar", 26f, "88AAAA", TextAlignmentOptions.MidlineRight);
        UiKit.Place(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 12f), new Vector2(700f, 36f));

        refs.dialoguePanel = panel;
        refs.speaker = speaker;
        refs.body = body;
        refs.portrait = portrait;
        refs.illustration = illustration;
        panel.SetActive(false);
    }

    // =====================================================================
    // Scene helpers
    // =====================================================================

    private static void RemoveRoots(Scene scene, Predicate<string> match)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (match(root.name))
                UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
                return root;
        }

        return null;
    }

    private static Camera NewCamera(Color background)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = background;
        go.AddComponent<AudioListener>();
        return camera;
    }

    private static void CreateEventSystemIfMissing()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            CreateEventSystem();
    }

    private static void CreateEventSystem()
    {
        GameObject go = new GameObject("EventSystem", typeof(EventSystem));

        // Same input module the rest of the project uses (new Input System), found by name so the
        // builder does not depend on the package assembly.
        Type moduleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (moduleType != null)
        {
            Component module = go.AddComponent(moduleType);
            var assign = moduleType.GetMethod("AssignDefaultActions");
            if (assign != null)
                assign.Invoke(module, null);
        }
        else
        {
            go.AddComponent<StandaloneInputModule>();
        }
    }

    private static void EnsureBuildScenes()
    {
        string[] first = { SplashPath, MainMenuPath, Level1Path, Level2Path };

        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < first.Length; i++)
        {
            string path = first[i];
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                continue;

            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(Mathf.Min(i, scenes.Count), new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
