using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reconstruye completamente el Nivel 2: "Las Cavernas Perdidas".
/// Menú Unity: Tools > Kai > Crear Nivel 2 desde cero
/// 
/// Características:
/// - Arquitectura de 3 Zonas:
///   1. Entrada y descenso rocoso (tutorial práctico de magia con Bastón en salto y tierra).
///   2. La Encrucijada / Laberinto vertical:
///      - Ruta baja (fosos de lava, plataformas móviles, cristales vivientes y corazones).
///      - Ruta alta (cornisas elevadas, murciélagos, serpientes y el Altar de la Gema Morada).
///   3. El Gran Salón del Portal Arcano.
/// - Texturas de roca orgánica y acabados de cueva natural sin bloques planos flotantes.
/// - Fondo degradado continuo azul/púrpura profundo con iluminación 2D adecuada.
/// - Serialización completa en escena (visible en Scene view antes de pulsar Play).
/// </summary>
public static class Level2Builder
{
    private const string Level1Path = "Assets/Scenes/SampleScene.unity";
    private const string Level2Path = "Assets/Scenes/Nivel2.unity";
    private const string GeneratedDir = "Assets/Sprites/Generated/";

    private const string SlimeSpritePath = GeneratedDir + "cave_slime.png";
    private const string CrystalSpritePath = GeneratedDir + "living_crystal.png";
    private const string PurpleGemSpritePath = GeneratedDir + "purple_gem.png";
    private const string CaveRockTilePath = GeneratedDir + "cave_rock_tile.png";
    private const string CaveWallBgPath = GeneratedDir + "cave_wall_bg.png";
    private const string CaveLavaTilePath = GeneratedDir + "cave_lava_tile.png";
    private const string ArcanePortalPath = GeneratedDir + "arcane_portal.png";

    private const string SlimePrefabPath = "Assets/Prefabs/CaveSlime.prefab";
    private const string CrystalPrefabPath = "Assets/Prefabs/LivingCrystal.prefab";
    private const string BatPrefabPath = "Assets/Prefabs/Bat.prefab";

    [MenuItem("Tools/Kai/Crear Nivel 2 desde cero")]
    public static void CreateLevel2FromScratch()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Sal del modo Play antes de generar el Nivel 2.");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Crear Nivel 2: Las Cavernas Perdidas",
            "¿Deseas construir el Nivel 2 con el nuevo diseño orgánico y laberíntico?\n\n" +
            "• Caverna estructurada en 3 zonas (Descenso, Encrucijada de dos rutas y Salón del Portal).\n" +
            "• Plataformas de roca natural con estalactitas, pilares y fosas de lava.\n" +
            "• Slimes de Caverna, Cristales Vivientes, Murciélagos y Serpientes acechando en cornisas.\n" +
            "• Altar Secreto con la Gema Morada de Arcanía (desbloquea la Capa del 20% de protección).\n" +
            "• NO altera SampleScene (Nivel 1) ni MainMenu.\n\n" +
            "¿Proceder?",
            "Crear Nivel 2", "Cancelar"))
        {
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        try
        {
            EnsureSpritesAndPrefabs();
            BuildLevel2Scene();
            EnsureBuildSettings();

            EditorSceneManager.OpenScene(Level2Path, OpenSceneMode.Single);
            Debug.Log("<color=green><b>[Level2Builder]</b></color> ¡Nivel 2 ('Las Cavernas Perdidas') reconstruido con éxito!");
            EditorUtility.DisplayDialog("Nivel 2", "¡Nivel 2 creado y serializado exitosamente en Assets/Scenes/Nivel2.unity!\nEl mapa orgánico y todos los enemigos ya son visibles en la escena.", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError("[Level2Builder] Error generando Nivel 2: " + ex);
            EditorUtility.DisplayDialog("Error", "Ocurrió un error al crear Nivel 2: " + ex.Message, "OK");
        }
    }

    private static void EnsureSpritesAndPrefabs()
    {
        Directory.CreateDirectory(GeneratedDir);
        Directory.CreateDirectory("Assets/Prefabs");

        GenerateSlimeTexture(SlimeSpritePath);
        ImportPixelSprite(SlimeSpritePath, 24f);

        GenerateCrystalTexture(CrystalSpritePath);
        ImportPixelSprite(CrystalSpritePath, 24f);

        GeneratePurpleGemTexture(PurpleGemSpritePath);
        ImportPixelSprite(PurpleGemSpritePath, 28f);

        GenerateCaveRockTileTexture(CaveRockTilePath);
        ImportPixelSprite(CaveRockTilePath, 32f);

        GenerateCaveWallBgTexture(CaveWallBgPath);
        ImportPixelSprite(CaveWallBgPath, 32f);

        GenerateCaveLavaTexture(CaveLavaTilePath);
        ImportPixelSprite(CaveLavaTilePath, 32f);

        GenerateArcanePortalTexture(ArcanePortalPath);
        ImportPixelSprite(ArcanePortalPath, 32f);

        AssetDatabase.Refresh();

        CreateEnemyPrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ImportPixelSprite(string path, float ppu)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Repeat;

            TextureImporterSettings tis = new TextureImporterSettings();
            ti.ReadTextureSettings(tis);
            tis.spriteMeshType = SpriteMeshType.FullRect;
            tis.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(tis);

            ti.SaveAndReimport();
        }
    }

    private static void GenerateCaveRockTileTexture(string path)
    {
        int w = 32, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

        Color baseRock = new Color(0.24f, 0.20f, 0.36f, 1f);
        Color darkRock = new Color(0.14f, 0.11f, 0.24f, 1f);
        Color midRock = new Color(0.32f, 0.27f, 0.48f, 1f);
        Color topCrust = new Color(0.55f, 0.44f, 0.78f, 1f);
        Color crystalGlint = new Color(0.75f, 0.60f, 0.98f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = baseRock;

                if (y >= h - 3)
                {
                    c = (y == h - 1) ? crystalGlint : topCrust;
                }
                else if (y <= 2)
                {
                    c = darkRock;
                }
                else
                {
                    int row = y / 8;
                    int offset = (row % 2) * 8;
                    int bx = (x + offset) % 16;
                    int by = y % 8;

                    if (bx == 0 || by == 0)
                        c = darkRock;
                    else if (by == 7 || bx == 1)
                        c = midRock;
                    else if ((x * 7 + y * 13) % 19 == 0)
                        c = midRock;
                    else if ((x * 11 + y * 5) % 23 == 0)
                        c = darkRock;
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    private static void GenerateCaveWallBgTexture(string path)
    {
        int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

        Color deepBg = new Color(0.09f, 0.07f, 0.16f, 1f);
        Color slate1 = new Color(0.13f, 0.10f, 0.22f, 1f);
        Color slate2 = new Color(0.17f, 0.13f, 0.28f, 1f);
        Color vein = new Color(0.36f, 0.22f, 0.54f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int pattern = ((x / 4) * 3 + (y / 4) * 5) % 7;
                Color c = deepBg;
                if (pattern == 0 || pattern == 3) c = slate1;
                else if (pattern == 5) c = slate2;

                if ((x + y * 2) % 29 == 0 && (x * 3 - y) % 17 == 0)
                    c = vein;

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    private static void GenerateCaveLavaTexture(string path)
    {
        int w = 32, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

        Color surfaceBright = new Color(1f, 0.50f, 0.95f, 0.95f);
        Color midLava = new Color(0.82f, 0.18f, 0.72f, 0.95f);
        Color deepLava = new Color(0.48f, 0.08f, 0.44f, 0.98f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float wave = Mathf.Sin((x / (float)w) * Mathf.PI * 2f) * 2f;
                if (y >= h - 4 + wave)
                    tex.SetPixel(x, y, surfaceBright);
                else if (y >= h / 2 + wave)
                    tex.SetPixel(x, y, midLava);
                else
                    tex.SetPixel(x, y, deepLava);
            }
        }

        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    private static void GenerateArcanePortalTexture(string path)
    {
        int w = 48, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color archStone = new Color(0.35f, 0.28f, 0.52f, 1f);
        Color archEdge = new Color(0.20f, 0.15f, 0.32f, 1f);
        Color portalOuter = new Color(0.70f, 0.25f, 0.98f, 0.95f);
        Color portalMid = new Color(0.38f, 0.80f, 1f, 0.95f);
        Color portalCore = new Color(0.95f, 0.98f, 1f, 1f);

        Vector2 center = new Vector2(w / 2f, h * 0.48f);
        float rx = w * 0.42f;
        float ry = h * 0.44f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x - center.x) / rx;
                float dy = (y - center.y) / ry;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (y < 6 && Mathf.Abs(x - center.x) < rx * 1.05f)
                {
                    tex.SetPixel(x, y, archStone);
                }
                else if (dist <= 1.05f && dist >= 0.78f)
                {
                    tex.SetPixel(x, y, (dist > 0.95f) ? archEdge : archStone);
                }
                else if (dist < 0.78f)
                {
                    if (dist < 0.25f) tex.SetPixel(x, y, portalCore);
                    else if (dist < 0.52f) tex.SetPixel(x, y, portalMid);
                    else tex.SetPixel(x, y, portalOuter);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }

        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    private static void GenerateSlimeTexture(string path)
    {
        int w = 28, h = 22;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color body = new Color(0.28f, 0.90f, 1f, 1f);
        Color darkBorder = new Color(0.10f, 0.38f, 0.55f, 1f);
        Color highlight = new Color(0.78f, 0.98f, 1f, 1f);
        Color eyeWhite = Color.white;
        Color eyePupil = new Color(0.12f, 0.05f, 0.22f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (x - w / 2f) / (w * 0.45f);
                float ny = (y - 3f) / (h * 0.78f);
                float dist = nx * nx + ny * ny;

                if (y >= 2 && dist <= 1f)
                {
                    if (dist > 0.76f || y == 2)
                        tex.SetPixel(x, y, darkBorder);
                    else if (nx < -0.2f && ny > 0.35f && dist < 0.55f)
                        tex.SetPixel(x, y, highlight);
                    else
                        tex.SetPixel(x, y, body);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }

        for (int dy = 0; dy < 3; dy++)
        {
            for (int dx = 0; dx < 3; dx++)
            {
                tex.SetPixel(9 + dx, 7 + dy, eyeWhite);
                tex.SetPixel(17 + dx, 7 + dy, eyeWhite);
            }
        }
        tex.SetPixel(10, 7, eyePupil);
        tex.SetPixel(11, 7, eyePupil);
        tex.SetPixel(10, 8, eyePupil);
        tex.SetPixel(18, 7, eyePupil);
        tex.SetPixel(19, 7, eyePupil);
        tex.SetPixel(18, 8, eyePupil);

        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    private static void GenerateCrystalTexture(string path)
    {
        int w = 26, h = 38;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color purple = new Color(0.80f, 0.30f, 1f, 1f);
        Color purpleDark = new Color(0.40f, 0.12f, 0.62f, 1f);
        Color purpleLight = new Color(0.96f, 0.75f, 1f, 1f);

        for (int y = 0; y < h; y++)
        {
            float normY = (float)y / h;
            int halfWidth = Mathf.RoundToInt(Mathf.Sin(normY * Mathf.PI) * (w * 0.44f));

            for (int x = 0; x < w; x++)
            {
                int diffX = Mathf.Abs(x - w / 2);
                if (diffX <= halfWidth && halfWidth > 0)
                {
                    if (diffX == halfWidth)
                        tex.SetPixel(x, y, purpleDark);
                    else if (x < w / 2 - 1)
                        tex.SetPixel(x, y, purpleLight);
                    else
                        tex.SetPixel(x, y, purple);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }

        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    private static void GeneratePurpleGemTexture(string path)
    {
        int w = 26, h = 30;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color magentaCore = new Color(0.90f, 0.28f, 1f, 1f);
        Color magentaEdge = new Color(0.44f, 0.09f, 0.58f, 1f);
        Color magentaGlint = new Color(1f, 0.88f, 1f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - w / 2f) / (w * 0.45f);
                float dy = Mathf.Abs(y - h / 2f) / (h * 0.48f);
                if (dx + dy <= 1f)
                {
                    if (dx + dy > 0.82f)
                        tex.SetPixel(x, y, magentaEdge);
                    else if (dx < 0.32f && dy < 0.32f)
                        tex.SetPixel(x, y, magentaGlint);
                    else
                        tex.SetPixel(x, y, magentaCore);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }

        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    private static void CreateEnemyPrefabs()
    {
        // 1. Prefab CaveSlime
        {
            GameObject slime = new GameObject("CaveSlime");
            slime.layer = 8; // Enemy layer

            SpriteRenderer sr = slime.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SlimeSpritePath);
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 5;
            sr.color = Color.white;

            CircleCollider2D col = slime.AddComponent<CircleCollider2D>();
            col.radius = 0.42f;
            col.offset = new Vector2(0f, 0.05f);

            Rigidbody2D rb = slime.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2.5f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            CaveSlime script = slime.AddComponent<CaveSlime>();
            script.playerLayer = 1 << 7;

            PrefabUtility.SaveAsPrefabAsset(slime, SlimePrefabPath);
            UnityEngine.Object.DestroyImmediate(slime);
        }

        // 2. Prefab LivingCrystal
        {
            GameObject crystal = new GameObject("LivingCrystal");
            crystal.layer = 8;

            SpriteRenderer sr = crystal.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CrystalSpritePath);
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 5;
            sr.color = Color.white;

            BoxCollider2D col = crystal.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.85f, 1.35f);
            col.isTrigger = true;

            crystal.AddComponent<LivingCrystal>();

            PrefabUtility.SaveAsPrefabAsset(crystal, CrystalPrefabPath);
            UnityEngine.Object.DestroyImmediate(crystal);
        }
    }

    private static void BuildLevel2Scene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Level2Path) != null)
        {
            AssetDatabase.DeleteAsset(Level2Path);
        }

        if (!AssetDatabase.CopyAsset(Level1Path, Level2Path))
            throw new InvalidOperationException("No se pudo copiar SampleScene a Nivel2.");

        Scene scene = EditorSceneManager.OpenScene(Level2Path, OpenSceneMode.Single);

        HashSet<string> allowedRootNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Main Camera",
            "Global Light 2D",
            "GameManager",
            "GameUI",
            "EventSystem",
            "Canvas"
        };

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            // Eliminar sin excepción cangrejos residuales del Nivel 1
            if (root.name.StartsWith("Crab", StringComparison.OrdinalIgnoreCase))
            {
                if (PrefabUtility.IsPartOfAnyPrefab(root))
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                UnityEngine.Object.DestroyImmediate(root);
                continue;
            }

            bool isPlayer = root.GetComponent<PlayerController>() != null || root.GetComponentInChildren<PlayerController>() != null;
            bool isUI = root.name == "Canvas" || root.name == "GameUI" || root.GetComponentInChildren<BarraVida>() != null;
            if (!isPlayer && !isUI && !allowedRootNames.Contains(root.name))
            {
                if (PrefabUtility.IsPartOfAnyPrefab(root))
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        ConfigureGlobalLight();
        CreateCaveBackgrounds();

        Transform cavernRoot = new GameObject("CavernGeometry").transform;
        BuildOrganizedCavernMap(cavernRoot);

        CreateHazards();

        PlayerController player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.transform.position = new Vector3(-8f, 1.0f, 0f);
            player.transform.rotation = Quaternion.identity;
            SaveSystem.HasStaff = true;
            player.currentWeapon = PlayerController.ActiveWeapon.Staff;
            player.moveSpeed = 4.6f;
            player.jumpForce = 8.5f;
            player.groundCheckRadius = 0.25f;

            // Sprites para espada y bastón (cambio visual en la mano con 'C')
            player.swordSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SPUM/Resources/Addons/Legacy/0_Unit/0_Sprite/6_Weapons/0_Sword/Sword_2.png");
            player.staffSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Generated/staff.png");

            // Asegurar que la barra de vida superior izquierda (BarraVida) está conectada
            if (player.barraVida == null)
            {
                BarraVida[] allBars = UnityEngine.Object.FindObjectsByType<BarraVida>(FindObjectsInactive.Include);
                foreach (BarraVida b in allBars)
                {
                    if (b.GetComponentInParent<MurcielagoIA>() == null)
                    {
                        player.barraVida = b;
                        break;
                    }
                }
            }

            if (player.textoDano == null)
            {
                TextMeshProUGUI[] texts = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
                foreach (TextMeshProUGUI t in texts)
                {
                    if (t.name == "TextoDano")
                    {
                        player.textoDano = t;
                        break;
                    }
                }
            }

            EditorUtility.SetDirty(player);
        }

        Camera cam = Camera.main;
        if (cam != null && player != null)
        {
            cam.transform.position = new Vector3(player.transform.position.x, player.transform.position.y + 1.2f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.05f, 0.12f, 1f);

            CameraFollow follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;
            follow.smoothSpeed = 5.5f;
            follow.offset = new Vector3(0f, 1.2f, -10f);
            EditorUtility.SetDirty(cam);
            EditorUtility.SetDirty(follow);
        }

        SpawnAllEnemies();
        SpawnRedGemFragment();
        SpawnPickupsAndChests();
        SpawnExitPortal();

        GameManager gm = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
            gm.nextSceneName = "";
            EnsureWeaponHudText(gm);
            EditorUtility.SetDirty(gm);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureGlobalLight()
    {
        GameObject globalLight = GameObject.Find("Global Light 2D");
        if (globalLight == null) return;

        foreach (Component comp in globalLight.GetComponents<Component>())
        {
            if (comp != null && comp.GetType().Name == "Light2D")
            {
                SerializedObject so = new SerializedObject(comp);

                SerializedProperty colProp = so.FindProperty("m_Color");
                if (colProp != null) colProp.colorValue = new Color(0.82f, 0.75f, 0.98f, 1f);

                SerializedProperty intProp = so.FindProperty("m_Intensity");
                if (intProp != null) intProp.floatValue = 0.95f;

                SerializedProperty layersProp = so.FindProperty("m_ApplyToSortingLayers");
                if (layersProp != null && layersProp.isArray)
                {
                    SortingLayer[] allLayers = SortingLayer.layers;
                    layersProp.arraySize = allLayers.Length;
                    for (int i = 0; i < allLayers.Length; i++)
                    {
                        layersProp.GetArrayElementAtIndex(i).intValue = allLayers[i].id;
                    }
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(comp);
            }
        }
    }

    private static void CreateCaveBackgrounds()
    {
        GameObject bgParent = new GameObject("Backgrounds");

        Sprite wallBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CaveWallBgPath);
        if (wallBgSprite != null)
        {
            GameObject solidBackdrop = new GameObject("CaveBackdrop_SolidRock");
            solidBackdrop.transform.SetParent(bgParent.transform);
            solidBackdrop.transform.position = new Vector3(35f, 2f, 10f);

            SpriteRenderer sr = solidBackdrop.AddComponent<SpriteRenderer>();
            sr.sprite = wallBgSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(140f, 48f);
            sr.sortingLayerName = "BG_Far";
            sr.sortingOrder = -30;
            sr.color = Color.white;
        }

        // Usar exclusivamente capas de caverna oscura subterránea (sin castillos de exterior)
        Sprite bgCaveMid = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Cave Platformer Tileset/Example stage layers/bg_05_cave_b.png");
        Sprite bgCaveInside = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Cave Platformer Tileset/Example stage layers/bg_06_caveinside_b.png");

        SpawnSeamlessBgRow(bgParent.transform, "Layer_CaveMid", bgCaveMid, "BG_Mid", -15, new Color(0.60f, 0.50f, 0.85f, 0.90f), 2.4f, 3.0f);
        SpawnSeamlessBgRow(bgParent.transform, "Layer_CaveInside", bgCaveInside, "BG_Low", -10, new Color(0.50f, 0.40f, 0.75f, 0.95f), 2.4f, 1.2f);
    }

    private static void SpawnSeamlessBgRow(Transform parent, string prefix, Sprite sprite, string sortingLayer, int order, Color tint, float scale, float posY)
    {
        if (sprite == null) return;

        float width = sprite.bounds.size.x * scale;
        if (width <= 1f) width = 15f;

        int count = Mathf.CeilToInt(120f / width) + 1;
        float startX = -20f;

        for (int i = 0; i < count; i++)
        {
            GameObject chunk = new GameObject($"{prefix}_{i}");
            chunk.transform.SetParent(parent);
            chunk.transform.position = new Vector3(startX + i * width, posY, 5f);
            chunk.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer sr = chunk.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
            sr.color = tint;
        }
    }

    private static void BuildOrganizedCavernMap(Transform parent)
    {
        // MARCO GENERAL DE LA CAVERNA
        CreatePlatform(parent, "Cave_Ceiling", new Vector3(35f, 13f, 0f), new Vector2(110f, 3f));
        CreatePlatform(parent, "Cave_Subfloor", new Vector3(35f, -9f, 0f), new Vector2(110f, 3f));
        CreatePlatform(parent, "Wall_Left", new Vector3(-18f, 2f, 0f), new Vector2(4f, 26f));
        CreatePlatform(parent, "Wall_Right", new Vector3(82f, 2f, 0f), new Vector2(4f, 26f));

        // ESTALACTITAS NATURALES
        CreateStalactite(parent, "Stalactite_Entry", new Vector3(-2f, 10f, 0f), new Vector2(2.4f, 3.2f));
        CreateStalactite(parent, "Stalactite_Chasm1", new Vector3(14f, 9.8f, 0f), new Vector2(3.0f, 3.5f));
        CreateStalactite(parent, "Stalactite_HighRoof", new Vector3(32f, 10.2f, 0f), new Vector2(2.5f, 2.8f));
        CreateStalactite(parent, "Stalactite_Abyss", new Vector3(45f, 9.6f, 0f), new Vector2(3.2f, 3.8f));
        CreateStalactite(parent, "Stalactite_Sanctuary", new Vector3(64f, 10f, 0f), new Vector2(2.6f, 3.0f));

        // ==========================================
        // ZONA 1: BOCA DE LA CAVERNA Y DESCENSO ROCOSO (x = -16 a 10)
        // ==========================================
        CreatePlatform(parent, "Entrance_HighLedge", new Vector3(-7f, -1.8f, 0f), new Vector2(16f, 3.5f));
        CreatePlatform(parent, "Entrance_Step_1", new Vector3(4f, -3.2f, 0f), new Vector2(5.5f, 2.2f));

        // ==========================================
        // ZONA 2: LA ENCRUCIJADA / LABERINTO (x = 10 a 52)
        // ==========================================
        // Plataforma central de bifurcación
        CreatePlatform(parent, "Chasm_Fork_Platform", new Vector3(12f, -3.5f, 0f), new Vector2(6f, 2.5f));

        // --- RUTA BAJA: TÚNEL DE RIESGO Y FOSAS DE LAVA ---
        CreatePlatform(parent, "LowTunnel_Floor_1", new Vector3(23f, -4.8f, 0f), new Vector2(11f, 3.5f));
        CreatePlatform(parent, "LowTunnel_Pillar_Mid", new Vector3(34f, -3.6f, 0f), new Vector2(4f, 4.5f));
        CreateMovingPlatform(parent, "MovingPlat_LowTunnel", new Vector3(39f, -2.6f, 0f), new Vector3(46f, -2.6f, 0f), 2.2f);
        CreatePlatform(parent, "LowTunnel_Exit_Ledge", new Vector3(51f, -4.2f, 0f), new Vector2(8f, 3.5f));

        // --- RUTA ALTA: LABERINTO VERTICAL Y ALTAR DE LA GEMA ---
        CreatePlatform(parent, "HighRoute_Ledge_1", new Vector3(16f, -0.2f, 0f), new Vector2(5.5f, 1.2f));
        CreatePlatform(parent, "HighRoute_Ledge_2", new Vector3(24f, 1.8f, 0f), new Vector2(6.5f, 1.2f));
        CreatePlatform(parent, "HighRoute_Ledge_3", new Vector3(33f, 3.6f, 0f), new Vector2(6f, 1.2f));

        CreateMovingPlatform(parent, "MovingPlat_HighAltar", new Vector3(39f, 4.2f, 0f), new Vector3(46f, 4.2f, 0f), 2.0f);

        // Altar Secreto de la Gema Morada en la cima
        CreatePlatform(parent, "HighRoute_GemSanctuary", new Vector3(52f, 5.0f, 0f), new Vector2(8.5f, 1.6f));
        // Cornisa de descenso de la ruta alta hacia el final
        CreatePlatform(parent, "HighRoute_DropLedge", new Vector3(58f, 1.2f, 0f), new Vector2(5.5f, 1.2f));

        // ==========================================
        // ZONA 3: EL GRAN SANTUARIO DEL PORTAL (x = 56 a 80)
        // ==========================================
        CreatePlatform(parent, "Sanctuary_Approach", new Vector3(64f, -3.8f, 0f), new Vector2(9f, 3.5f));
        CreatePlatform(parent, "Sanctuary_PortalChamber", new Vector3(74f, -2.5f, 0f), new Vector2(12f, 5.0f));
    }

    private static void CreatePlatform(Transform parent, string name, Vector3 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.layer = 6; // Ground

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetCavePlatformSprite();
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.sortingLayerName = "Tilemap";
        sr.sortingOrder = 0;
        sr.color = Color.white;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = size;
    }

    private static void CreateStalactite(Transform parent, string name, Vector3 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetCavePlatformSprite();
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.sortingLayerName = "Tilemap";
        sr.sortingOrder = -2;
        sr.color = new Color(0.68f, 0.60f, 0.88f, 1f);
        go.transform.localScale = new Vector3(1f, -1f, 1f);
    }

    private static void CreateMovingPlatform(Transform parent, string name, Vector3 pA, Vector3 pB, float speed)
    {
        GameObject container = new GameObject(name + "_Container");
        container.transform.SetParent(parent);
        container.transform.position = Vector3.zero;

        GameObject ptA = new GameObject(name + "_PointA");
        ptA.transform.SetParent(container.transform);
        ptA.transform.position = pA;

        GameObject ptB = new GameObject(name + "_PointB");
        ptB.transform.SetParent(container.transform);
        ptB.transform.position = pB;

        GameObject plat = new GameObject(name);
        plat.transform.SetParent(container.transform);
        plat.transform.position = pA;
        plat.layer = 6; // Ground

        Vector2 size = new Vector2(3.6f, 0.85f);

        SpriteRenderer sr = plat.AddComponent<SpriteRenderer>();
        sr.sprite = GetCavePlatformSprite();
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.sortingLayerName = "Tilemap";
        sr.sortingOrder = 5;
        sr.color = new Color(0.88f, 0.68f, 1f, 1f);

        BoxCollider2D col = plat.AddComponent<BoxCollider2D>();
        col.size = size;

        Rigidbody2D rb = plat.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        PlataformaMovil pm = plat.AddComponent<PlataformaMovil>();
        pm.puntoA = ptA.transform;
        pm.puntoB = ptB.transform;
        pm.velocidad = speed;

        EditorUtility.SetDirty(pm);
    }

    private static Sprite GetCavePlatformSprite()
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(CaveRockTilePath);
        if (s == null)
            s = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Cave Platformer Tileset/Individual PNG files/Tileset/cave_tileset/main_tiles_16x16_0.png");
        return s;
    }

    private static void CreateHazards()
    {
        GameObject hazardParent = new GameObject("Hazards_LavaPits");

        // Foso 1: Salto inicial hacia la encrucijada
        CreateAcidPit(hazardParent, "LavaPit_1", new Vector3(7.5f, -7.0f, 0f), 5.5f, 3f);

        // Foso 2: Abismo de lava del túnel bajo
        CreateAcidPit(hazardParent, "LavaPit_LowTunnel", new Vector3(41.5f, -7.0f, 0f), 13.0f, 3f);

        // Foso 3: Abismo antes del santuario
        CreateAcidPit(hazardParent, "LavaPit_Sanctuary", new Vector3(58.5f, -7.0f, 0f), 8.5f, 3f);
    }

    private static void CreateAcidPit(GameObject parent, string name, Vector3 pos, float width, float height)
    {
        GameObject pit = new GameObject(name);
        pit.transform.SetParent(parent.transform);
        pit.transform.position = pos;
        pit.tag = "Agua";

        BoxCollider2D col = pit.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(width, height);

        pit.AddComponent<AguaMortal>();

        Sprite lavaSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CaveLavaTilePath);
        if (lavaSprite != null)
        {
            SpriteRenderer sr = pit.AddComponent<SpriteRenderer>();
            sr.sprite = lavaSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(width, height);
            sr.sortingLayerName = "Tilemap";
            sr.sortingOrder = 8;
            sr.color = Color.white;
        }
    }

    private static void SpawnAllEnemies()
    {
        GameObject enemyGroup = new GameObject("Enemies_Cave");

        // 1. MURCIÉLAGOS DE CAVERNA (Usan exclusivamente el spritesheet 80x80 solicitado)
        Sprite[] batFrames = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/bat_spritesheet(80x80).png")
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        Vector3[] batPositions = new Vector3[]
        {
            new Vector3(4.5f, 1.8f, 0f),    // Sobre el tramo inicial
            new Vector3(14.0f, 2.2f, 0f),   // Sobre el chasm / bifurcación
            new Vector3(26.0f, 4.8f, 0f),   // En la ruta alta
            new Vector3(38.0f, 1.2f, 0f),   // En el tramo bajo de lava
            new Vector3(54.0f, 3.8f, 0f)    // Antes de la cámara del portal
        };

        for (int i = 0; i < batPositions.Length; i++)
        {
            GameObject bat = new GameObject("CaveBat_" + (i + 1));
            bat.transform.SetParent(enemyGroup.transform);
            bat.transform.position = batPositions[i];
            bat.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
            bat.layer = 8; // Enemy layer
            bat.tag = "Enemigo";

            SpriteRenderer sr = bat.AddComponent<SpriteRenderer>();
            sr.sprite = batFrames.Length > 0 ? batFrames[0] : null;
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 6;
            sr.color = Color.white;

            CircleCollider2D col = bat.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
            col.isTrigger = true;

            Rigidbody2D rb = bat.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            CaveBat batScript = bat.AddComponent<CaveBat>();
            batScript.flySprites = batFrames;
            batScript.playerLayer = 1 << 7;
            batScript.attackDamage = 12;
            batScript.maxHealth = 30;
            batScript.detectionRange = 5.8f;
            batScript.chaseSpeed = 3.2f;
            batScript.patrolRadius = 2.8f;
            EditorUtility.SetDirty(batScript);
        }

        // 2. SERPIENTES DE CAVERNA (Reemplazan a los cangrejos en el piso exacto donde camina Kai)
        Sprite[] allSnakeSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/snake_spritesheet(64x64).png")
            .OfType<Sprite>()
            .OrderBy(s => ExtractTrailingIndex(s.name))
            .ToArray();

        Sprite[] idleSprites = allSnakeSprites.Take(6).ToArray();
        Sprite[] attackSprites = allSnakeSprites.Length >= 13 ? new Sprite[] { allSnakeSprites[6], allSnakeSprites[7], allSnakeSprites[9], allSnakeSprites[10], allSnakeSprites[11], allSnakeSprites[12] } : idleSprites;
        Sprite[] hurtSprites = allSnakeSprites.Skip(13).Take(6).ToArray();
        Sprite[] slitherSprites = allSnakeSprites.Skip(24).Take(6).ToArray();
        Sprite[] deathSprites = allSnakeSprites.Skip(30).Take(6).ToArray();

        Vector3[] snakePositions = new Vector3[]
        {
            new Vector3(-2.50f, -0.05f, 0f),  // Cornisa inicial donde camina Kai (superficie en Y = -0.05)
            new Vector3(5.00f, -2.10f, 0f),   // Escalón de entrada (superficie en Y = -2.10)
            new Vector3(20.50f, -3.05f, 0f),  // Túnel bajo - a la izquierda del corazón (superficie en Y = -3.05)
            new Vector3(25.50f, -3.05f, 0f),  // Túnel bajo - a la derecha del corazón (superficie en Y = -3.05)
            new Vector3(64.00f, -2.05f, 0f)   // Aproximación final hacia el santuario y portal (superficie en Y = -2.05)
        };

        for (int i = 0; i < snakePositions.Length; i++)
        {
            GameObject snake = new GameObject("CaveSnake_" + (i + 1));
            snake.transform.SetParent(enemyGroup.transform);
            snake.transform.position = snakePositions[i];
            snake.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
            snake.layer = 8; // Enemy layer
            snake.tag = "Enemigo";

            SpriteRenderer sr = snake.AddComponent<SpriteRenderer>();
            sr.sprite = idleSprites.Length > 0 ? idleSprites[0] : null;
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 5;
            sr.color = Color.white;

            BoxCollider2D col = snake.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.42f, 0.22f);
            col.offset = new Vector2(0f, 0.11f);

            Rigidbody2D rb = snake.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            CaveSnake snakeScript = snake.AddComponent<CaveSnake>();
            snakeScript.idleSprites = idleSprites;
            snakeScript.slitherSprites = slitherSprites;
            snakeScript.attackSprites = attackSprites;
            snakeScript.hurtSprites = hurtSprites;
            snakeScript.deathSprites = deathSprites;
            snakeScript.contactDamage = 15;
            snakeScript.attackRange = 1.3f;
            snakeScript.maxHealth = 30;
            snakeScript.patrolDistance = 2.5f;
            EditorUtility.SetDirty(snakeScript);
        }

        // 3. CRISTALES VIVIENTES (Obstáculos mágicos pulsantes en puntos clave)
        GameObject crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalPrefabPath);
        Sprite crystalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CrystalSpritePath);

        Vector3[] crystalPositions = new Vector3[]
        {
            new Vector3(11f, -1.8f, 0f), // Custodiando la encrucijada
            new Vector3(45f, 6.2f, 0f)   // Frente a la entrada del altar de la gema morada
        };

        for (int i = 0; i < crystalPositions.Length; i++)
        {
            GameObject instance = (crystalPrefab != null)
                ? (GameObject)PrefabUtility.InstantiatePrefab(crystalPrefab)
                : new GameObject("LivingCrystal");

            instance.name = "LivingCrystal_" + (i + 1);
            instance.transform.SetParent(enemyGroup.transform);
            instance.transform.position = crystalPositions[i];
            instance.layer = 8;

            SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (sr.sprite == null) sr.sprite = crystalSprite;
                sr.sortingLayerName = "Player";
                sr.sortingOrder = 5;
            }
        }
    }

    private static Sprite GetFirstSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;
        foreach (UnityEngine.Object obj in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (obj is Sprite sp) return sp;
        }
        return null;
    }

    private static void SpawnRedGemFragment()
    {
        GameObject gem = new GameObject("RedGemFragment");
        gem.transform.position = new Vector3(72f, 0.6f, 0f);
        gem.transform.localScale = new Vector3(2f, 2f, 1f);
        gem.tag = "Gema";

        SpriteRenderer sr = gem.AddComponent<SpriteRenderer>();
        Sprite[] gemSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Coin_Gems/spr_coin_roj.png").OfType<Sprite>().ToArray();
        sr.sprite = gemSprites.Length > 0 ? gemSprites[0] : null;
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 15;
        sr.color = Color.white;

        CircleCollider2D col = gem.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;

        Bobbing bob = gem.AddComponent<Bobbing>();
        bob.amplitude = 0.15f;
        bob.speed = 2.5f;
        bob.pulse = 0.05f;

        GemFragment fragment = gem.AddComponent<GemFragment>();
        fragment.fragmentNumber = 2;
        fragment.unlocksCape = true;
        fragment.message = "¡Has obtenido la segunda parte de la Gema de Arcanía!";
        fragment.glowColor = new Color(1f, 0.25f, 0.25f, 0.85f);
        fragment.glowSize = 1.8f;
        fragment.damageBonus = 0;
        fragment.scoreValue = 100;
    }

    private static void SpawnPickupsAndChests()
    {
        GameObject pickupsParent = new GameObject("Pickups_And_Chests");
        GameObject heartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cereza.prefab");

        Vector3[] heartPositions = new Vector3[]
        {
            new Vector3(4f, -1.8f, 0f),
            new Vector3(23f, -2.6f, 0f),
            new Vector3(33f, 4.8f, 0f),
            new Vector3(66f, -1.6f, 0f)
        };

        foreach (Vector3 pos in heartPositions)
        {
            if (heartPrefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(heartPrefab);
                instance.transform.SetParent(pickupsParent.transform);
                instance.transform.position = pos;
            }
        }

        // Cofre de recompensa secreto en la ruta baja (otorga la Capa Arcana del 20% de protección)
        Sprite chestSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Cave Platformer Tileset/Individual PNG files/Tileset/objects_bigchest_animated/objects_bigchest_animated_0.png");
        if (chestSprite != null)
        {
            GameObject chest = new GameObject("SecretChest");
            chest.transform.SetParent(pickupsParent.transform);
            chest.transform.position = new Vector3(53f, -2.1f, 0f);

            SpriteRenderer sr = chest.AddComponent<SpriteRenderer>();
            sr.sprite = chestSprite;
            sr.sortingLayerName = "Tilemap";
            sr.sortingOrder = 5;

            CircleCollider2D col = chest.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.6f;

            SecretChest chestScript = chest.AddComponent<SecretChest>();
            List<Sprite> chestFrames = new List<Sprite>();
            for (int i = 0; i < 28; i++)
            {
                Sprite frame = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Cave Platformer Tileset/Individual PNG files/Tileset/objects_bigchest_animated/objects_bigchest_animated_{i}.png");
                if (frame != null) chestFrames.Add(frame);
            }
            chestScript.openSprites = chestFrames.ToArray();
            chestScript.rewardMessage = "¡HAS ENCONTRADO LA CAPA ARCANA!\nTe protegerá en tu aventura reduciendo un 20% todo el daño recibido.";
            EditorUtility.SetDirty(chestScript);
        }
    }

    private static void SpawnExitPortal()
    {
        GameObject portal = new GameObject("ExitPortal");
        portal.transform.position = new Vector3(76f, 0.5f, 0f);

        Sprite portalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArcanePortalPath);

        SpriteRenderer sr = portal.AddComponent<SpriteRenderer>();
        sr.sprite = portalSprite;
        sr.color = Color.white;
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 10;
        portal.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

        CircleCollider2D col = portal.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.9f;

        Bobbing bob = portal.AddComponent<Bobbing>();
        bob.amplitude = 0.1f;
        bob.speed = 2f;

        LevelPortal exitTrigger = portal.AddComponent<LevelPortal>();
        exitTrigger.message = "¡Has completado Las Cavernas Perdidas! Avanzando al siguiente nivel...";
        exitTrigger.glowColor = new Color(0.75f, 0.35f, 1f, 0.9f);
    }

    private static void EnsureWeaponHudText(GameManager gm)
    {
        if (gm.weaponText != null) return;

        GameObject hud = GameObject.Find("HUD");
        if (hud == null) return;

        Transform existing = hud.transform.Find("WeaponText");
        if (existing != null)
        {
            gm.weaponText = existing.GetComponent<TextMeshProUGUI>();
            return;
        }

        TextMeshProUGUI template = gm.timeText != null ? gm.timeText : gm.scoreText;
        GameObject wtObj = new GameObject("WeaponText", typeof(RectTransform));
        wtObj.transform.SetParent(hud.transform, false);

        RectTransform rt = wtObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(24f, -130f);
        rt.sizeDelta = new Vector2(360f, 40f);

        TextMeshProUGUI tmp = wtObj.AddComponent<TextMeshProUGUI>();
        if (template != null)
        {
            tmp.font = template.font;
            tmp.fontSize = template.fontSize;
            tmp.color = new Color(0.85f, 0.70f, 1f, 1f);
        }
        else
        {
            tmp.fontSize = 24f;
            tmp.color = Color.white;
        }
        tmp.text = "Arma: Bastón";
        gm.weaponText = tmp;
    }

    private static void EnsureBuildSettings()
    {
        EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
        bool hasNivel2 = false;
        foreach (var sc in currentScenes)
        {
            if (sc.path == Level2Path)
            {
                hasNivel2 = true;
                break;
            }
        }

        if (!hasNivel2)
        {
            EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[currentScenes.Length + 1];
            currentScenes.CopyTo(newScenes, 0);
            newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(Level2Path, true);
            EditorBuildSettings.scenes = newScenes;
        }
    }

    private static int ExtractTrailingIndex(string name)
    {
        int lastUnderscore = name.LastIndexOf('_');
        if (lastUnderscore >= 0 && int.TryParse(name.Substring(lastUnderscore + 1), out int idx))
            return idx;
        return 0;
    }
}
