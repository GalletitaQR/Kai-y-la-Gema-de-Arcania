using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Cierre del Nivel 4 ("Templo de Arcania") y final de la historia:
/// - La Gema de Arcania aparece al derrotar al Dragón Guardián y al Mage Esqueleto.
/// - Cuando Kai la recoge, camina frente al Portal de Arcania en el Altar del Trono,
///   canaliza el poder de la Gema para cerrar el portal que se abrió al inicio del juego,
///   aparece el mensaje de que Kai se convirtió en un verdadero mago y se muestra
///   la pantalla final con opción de volver al menú principal.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ArcaniaFinalEnding : MonoBehaviour
{
    [Header("Referencias del Cierre en la Sala del Trono")]
    public Transform portalTransform;
    public int scoreValue = 300;
    public Color gemGlowColor = new Color(0.35f, 0.95f, 1f, 0.9f);
    public Color portalColor = new Color(0.75f, 0.30f, 1f, 0.9f);

    [Header("Mensajes de Historia")]
    [TextArea]
    public string pickupMessage = "¡Has recuperado la Gema de Arcanía! Canalizando su poder para sellar el Portal...";
    [TextArea]
    public string extendingPortalMessage = "¡Cuidado! ¡Aunque el altar cayó, el portal principal sobre Arcanía se está extendiendo!";
    [TextArea]
    public string closingMessage = "¡El Portal ha sido cerrado para siempre! Kai se ha convertido en el nuevo Mago de Arcanía.";

    [Header("Cinemática Final del Portal en Arcanía")]
    public Sprite infectedWorldSprite;
    public Sprite expandingPortalSprite;
    public Sprite healedWorldSprite;

    private bool sequenceStarted = false;
    private SpriteRenderer gemRenderer;
    private Collider2D gemCollider;
    private Vector3 initialGemPos;

    private void Awake()
    {
        gemRenderer = GetComponent<SpriteRenderer>();
        gemCollider = GetComponent<Collider2D>();
        if (gemCollider != null) gemCollider.isTrigger = true;
        initialGemPos = transform.position;
    }

    private void OnEnable()
    {
        initialGemPos = transform.position;
        MagicFX.AddGlow(transform, gemGlowColor, 2.2f);
        MagicFX.AddSparkles(transform, gemGlowColor, 0.35f, 16f);
        MagicFX.Burst(transform.position, gemGlowColor, 36, 0.55f);

        if (portalTransform != null)
        {
            MagicFX.AddGlow(portalTransform, portalColor, 2.8f);
            MagicFX.AddSparkles(portalTransform, portalColor, 0.45f, 20f);
        }

        if (MessageBanner.Instance != null)
        {
            MessageBanner.Instance.Show("¡La Gema de Arcanía ha aparecido en el Altar! Tómala para cerrar el portal.", 4.0f);
        }
    }

    private void Update()
    {
        if (!sequenceStarted)
        {
            // Levitación suave de la Gema de Arcania mientras espera a Kai
            transform.position = initialGemPos + new Vector3(0f, Mathf.Sin(Time.time * 2.6f) * 0.18f, 0f);
        }

        if (portalTransform != null && portalTransform.gameObject.activeSelf)
        {
            portalTransform.Rotate(0f, 0f, 95f * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTriggerEnding(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryTriggerEnding(other);
    }

    private void TryTriggerEnding(Collider2D other)
    {
        if (sequenceStarted || other == null) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) player = other.GetComponent<PlayerController>();
        if (player == null || player.IsDead) return;

        sequenceStarted = true;
        if (gemCollider != null) gemCollider.enabled = false;
        StartCoroutine(ClosingPortalCutsceneRoutine(player));
    }

    private IEnumerator ClosingPortalCutsceneRoutine(PlayerController player)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameState.Cutscene);
            GameManager.Instance.nextSceneName = "MainMenu";
            GameManager.Instance.victoryAutoDelay = 15f;
        }

        if (gemCollider != null) gemCollider.enabled = false;
        if (gemRenderer != null) gemRenderer.enabled = false;
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.enabled = false;
        }
        foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        player.SetControlEnabled(false);
        player.StopHorizontalMotion();
        player.ForceAnimation(PlayerState.IDLE);
        player.SetCutsceneWeapon(true);
        player.PlayAbsorbFeedback(gemGlowColor);

        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.gemFragmentPickup : null);
        MagicFX.Burst(transform.position, gemGlowColor, 50, 0.55f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
        }

        if (MessageBanner.Instance != null)
        {
            string bannerText = string.IsNullOrEmpty(pickupMessage) || pickupMessage.Contains("Canalizando su poder para sellar")
                ? "¡Has recuperado la Gema de Arcanía!"
                : pickupMessage;
            MessageBanner.Instance.Show(bannerText, 3.8f);
        }

        // Mantener a Kai detenido en reposo mientras absorbe el aura de la Gema
        float absorbWait = 1.05f;
        while (absorbWait > 0f)
        {
            absorbWait -= Time.deltaTime;
            player.StopHorizontalMotion();
            yield return null;
        }

        // 1. Kai camina frente al Portal de Arcania en el Altar del Trono
        Vector3 portalPos = portalTransform != null
            ? portalTransform.position
            : new Vector3(132.5f, 1.35f, 0f);

        float targetKaiX = portalPos.x - 2.0f;
        Rigidbody2D body = player.Body;
        float walkTimeout = 1.8f;
        float lastX = player.transform.position.x;
        float stuckTimer = 0f;

        while (body != null && Mathf.Abs(player.transform.position.x - targetKaiX) > 0.18f && walkTimeout > 0f)
        {
            float dt = Time.deltaTime;
            walkTimeout -= dt;
            float dir = Mathf.Sign(targetKaiX - player.transform.position.x);
            player.FaceDirection(dir);
            player.SetAnimation(PlayerState.MOVE);
            body.linearVelocity = new Vector2(dir * 3.0f, body.linearVelocity.y);
            yield return null;

            if (Mathf.Abs(player.transform.position.x - lastX) < 0.005f)
            {
                stuckTimer += dt;
                if (stuckTimer >= 0.30f) break;
            }
            else
            {
                stuckTimer = 0f;
            }
            lastX = player.transform.position.x;
        }

        player.StopHorizontalMotion();
        float dirToPortal = portalPos.x >= player.transform.position.x ? 1f : -1f;
        player.FaceDirection(dirToPortal);
        player.ForceAnimation(PlayerState.IDLE);
        yield return new WaitForSeconds(0.25f);

        // 2. Kai canaliza la Gema de Arcania y lanza 3 pulsos mágicos hacia el portal
        for (int i = 0; i < 3; i++)
        {
            player.StopHorizontalMotion();
            dirToPortal = portalPos.x >= player.transform.position.x ? 1f : -1f;
            player.FaceDirection(dirToPortal);
            player.ForceAnimation(PlayerState.ATTACK);

            AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.playerAttack : null);
            AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.magicBolt : null);

            Vector3 castOrigin = player.transform.position + new Vector3(dirToPortal * 0.65f, 0.55f, 0f);
            Color pulseColor = Color.Lerp(portalColor, new Color(1f, 0.92f, 0.45f, 1f), (i + 1) / 3f);

            MagicFX.SpawnShockwave(castOrigin, gemGlowColor, 2.2f, 0.35f);
            MagicFX.Burst(castOrigin, gemGlowColor, 22, 0.35f);
            StartCoroutine(LaunchArcanePulse(castOrigin, portalPos, pulseColor));

            yield return new WaitForSeconds(0.45f);
            player.ForceAnimation(PlayerState.IDLE);
            yield return new WaitForSeconds(0.22f);
        }

        // 3. El Portal de Arcania se contrae y se sella definitivamente
        if (portalTransform != null)
        {
            Vector3 startScale = portalTransform.localScale;
            float closeDuration = 1.4f;
            float elapsed = 0f;
            while (elapsed < closeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / closeDuration);
                portalTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                portalTransform.Rotate(0f, 0f, 420f * Time.deltaTime);
                yield return null;
            }
            portalTransform.gameObject.SetActive(false);
        }

        // Gran destello final al cerrarse el portal del altar
        MagicFX.SpawnShockwave(portalPos, new Color(1f, 0.92f, 0.45f, 0.95f), 4.0f, 0.6f);
        MagicFX.Burst(portalPos, new Color(1f, 0.92f, 0.45f, 1f), 65, 0.7f);
        MagicFX.Burst(portalPos, gemGlowColor, 55, 0.65f);

        yield return new WaitForSeconds(0.45f);

        // 4. Aviso de que el portal principal sobre Arcanía se está extendiendo y secuencia ilustrada de cierre
        if (MessageBanner.Instance != null && !string.IsNullOrEmpty(extendingPortalMessage))
        {
            MessageBanner.Instance.Show(extendingPortalMessage, 4.5f);
        }

        yield return new WaitForSeconds(1.35f);

        yield return StartCoroutine(PlayFullscreenPortalSealSequence());

        // 5. Mensaje de cierre sobre Arcanía restaurada
        if (MessageBanner.Instance != null)
        {
            MessageBanner.Instance.Show(closingMessage, 5.5f);
        }

        yield return new WaitForSeconds(2.6f);

        // 6. Personalizar y mostrar la Pantalla Final con opción de volver al menú
        CustomizeFinalVictoryScreen();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteLevel();
        }
    }

    private void OnValidate()
    {
        EnsureCinematicSprites();
    }

    private void EnsureCinematicSprites()
    {
#if UNITY_EDITOR
        if (infectedWorldSprite == null)
            infectedWorldSprite = LoadSpriteAsset("Assets/Coin_Gems/assets/assets/portalinfecta.png");
        if (expandingPortalSprite == null)
            expandingPortalSprite = LoadSpriteAsset("Assets/Coin_Gems/assets/assets/porfal_sin_fondo.png");
        if (healedWorldSprite == null)
            healedWorldSprite = LoadSpriteAsset("Assets/Coin_Gems/assets/assets/fondomenu.png");
#endif
    }

#if UNITY_EDITOR
    private static Sprite LoadSpriteAsset(string assetPath)
    {
        Sprite direct = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (direct != null) return direct;

        Object[] all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (Object obj in all)
        {
            if (obj is Sprite sprite)
                return sprite;
        }
        return null;
    }
#endif

    private IEnumerator PlayFullscreenPortalSealSequence()
    {
        EnsureCinematicSprites();

        if (infectedWorldSprite == null && expandingPortalSprite == null && healedWorldSprite == null)
            yield break;

        Transform uiParent = null;
        int insertIndex = 0;

        if (MessageBanner.Instance != null && MessageBanner.Instance.transform.parent != null)
        {
            uiParent = MessageBanner.Instance.transform.parent;
            insertIndex = MessageBanner.Instance.transform.GetSiblingIndex();
        }
        else
        {
            GameObject canvasGo = new GameObject("PortalEndingCinematicCanvas");
            Canvas fallbackCanvas = canvasGo.AddComponent<Canvas>();
            fallbackCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fallbackCanvas.sortingOrder = 5;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            uiParent = canvasGo.transform;
        }

        GameObject root = new GameObject("PortalEndingCinematic", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(uiParent, false);
        rootRect.SetSiblingIndex(insertIndex);
        StretchFull(rootRect);

        CanvasGroup rootGroup = root.GetComponent<CanvasGroup>();
        rootGroup.alpha = 0f;
        rootGroup.blocksRaycasts = true;
        rootGroup.interactable = false;

        // Fondo oscuro sólido para cubrir el gameplay detrás de las ilustraciones
        Image backdrop = CreateFullscreenImage("Backdrop", rootRect, null, new Color(0.03f, 0.02f, 0.07f, 1f));
        backdrop.raycastTarget = true;

        // Contenedor del mundo para poder aplicar vibración sin mover el marco
        GameObject worldGo = new GameObject("WorldContainer", typeof(RectTransform));
        RectTransform worldRect = worldGo.GetComponent<RectTransform>();
        worldRect.SetParent(rootRect, false);
        StretchFull(worldRect);

        // 1. Fondo final restaurado (fondomenu.png)
        CreateFullscreenImage("HealedWorldImage", worldRect, healedWorldSprite, Color.white);

        // 2. Mundo infectado (portalinfecta.png) que se disolverá al sellarse el portal
        Image infectedWorldImg = CreateFullscreenImage("InfectedWorldImage", worldRect, infectedWorldSprite, Color.white);

        // 3. Portal abierto + portal expandiéndose (porfal_sin_fondo.png), alineado 1:1 al recorte (78,0,1006,765) sobre 1376x768
        RectTransform expandingPortalRect = null;
        Image expandingPortalImg = null;
        if (expandingPortalSprite != null)
        {
            GameObject portalGo = new GameObject("ExpandingPortalImage", typeof(RectTransform), typeof(Image));
            expandingPortalRect = portalGo.GetComponent<RectTransform>();
            expandingPortalRect.SetParent(worldRect, false);
            expandingPortalRect.anchorMin = new Vector2(78f / 1376f, 0f);
            expandingPortalRect.anchorMax = new Vector2((78f + 1006f) / 1376f, 765f / 768f);
            expandingPortalRect.pivot = new Vector2(0.5f, 0.5f);
            expandingPortalRect.offsetMin = Vector2.zero;
            expandingPortalRect.offsetMax = Vector2.zero;

            expandingPortalImg = portalGo.GetComponent<Image>();
            expandingPortalImg.sprite = expandingPortalSprite;
            expandingPortalImg.preserveAspect = false;
            expandingPortalImg.raycastTarget = false;
        }

        // 4. Halo energético centrado en el corazón del portal (x=0.4222, y=0.4974)
        GameObject glowGo = new GameObject("PortalEnergyGlow", typeof(RectTransform), typeof(Image));
        RectTransform glowRect = glowGo.GetComponent<RectTransform>();
        glowRect.SetParent(worldRect, false);
        glowRect.anchorMin = new Vector2(0.4222f, 0.4974f);
        glowRect.anchorMax = new Vector2(0.4222f, 0.4974f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.sizeDelta = new Vector2(520f, 520f);
        glowRect.anchoredPosition = Vector2.zero;

        Image glowImg = glowGo.GetComponent<Image>();
        glowImg.sprite = MagicFX.GlowSprite;
        glowImg.color = new Color(portalColor.r, portalColor.g, portalColor.b, 0f);
        glowImg.raycastTarget = false;

        // 5. Destello final de pantalla completa
        Image flashImg = CreateFullscreenImage("FlashOverlay", rootRect, null, new Color(1f, 0.95f, 0.72f, 0f));

        // Fase A: Fundido de entrada hacia Arcanía infectada
        float fadeInDuration = 0.55f;
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            rootGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        rootGroup.alpha = 1f;

        // Fase B: El portal se expande y late amenazantemente sobre el reino
        float threatDuration = 1.15f;
        elapsed = 0f;
        while (elapsed < threatDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / threatDuration);
            if (expandingPortalRect != null)
            {
                float scale = Mathf.Lerp(0.96f, 1.07f, t) + Mathf.Sin(t * Mathf.PI * 3f) * 0.018f;
                expandingPortalRect.localScale = new Vector3(scale, scale, 1f);
            }
            glowImg.color = new Color(portalColor.r, portalColor.g, portalColor.b, Mathf.Sin(t * Mathf.PI) * 0.42f);
            glowRect.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.25f, t);
            worldRect.anchoredPosition = Random.insideUnitCircle * 4.5f;
            yield return null;
        }
        worldRect.anchoredPosition = Vector2.zero;

        // Fase C: Tres descargas de la Gema de Arcanía impactan el portal principal
        for (int i = 0; i < 3; i++)
        {
            AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.playerAttack : null);
            AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.magicBolt : null);

            Color pulseTint = Color.Lerp(gemGlowColor, new Color(1f, 0.94f, 0.55f, 1f), (i + 1) / 3f);
            float pulseDuration = 0.55f;
            elapsed = 0f;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / pulseDuration);
                float wave = Mathf.Sin(t * Mathf.PI);

                glowImg.color = new Color(pulseTint.r, pulseTint.g, pulseTint.b, wave * 0.85f);
                glowRect.localScale = Vector3.one * Mathf.Lerp(0.45f, 1.85f, t);

                if (expandingPortalImg != null)
                {
                    expandingPortalImg.color = Color.Lerp(Color.white, new Color(0.75f, 1f, 1f, 1f), wave * 0.65f);
                    float recoil = Mathf.Lerp(1.06f, 0.98f, t) - wave * 0.04f;
                    expandingPortalRect.localScale = new Vector3(recoil, recoil, 1f);
                }

                worldRect.anchoredPosition = Random.insideUnitCircle * (6.5f * (1f - t));
                yield return null;
            }

            if (expandingPortalImg != null)
                expandingPortalImg.color = Color.white;
            worldRect.anchoredPosition = Vector2.zero;
            yield return new WaitForSeconds(0.12f);
        }

        // Fase D: Mismo efecto de giro y contracción hacia cero mientras el reino sana (portalinfecta -> fondomenu)
        Vector3 portalStartScale = expandingPortalRect != null ? expandingPortalRect.localScale : Vector3.one;
        float closeDuration = 1.65f;
        elapsed = 0f;
        while (elapsed < closeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / closeDuration);
            float shrinkT = t * t;

            if (expandingPortalRect != null)
            {
                expandingPortalRect.localScale = Vector3.Lerp(portalStartScale, Vector3.zero, shrinkT);
                expandingPortalRect.Rotate(0f, 0f, 420f * Time.deltaTime);
            }

            if (infectedWorldImg != null)
            {
                float healT = Mathf.Clamp01((t - 0.15f) / 0.85f);
                infectedWorldImg.color = new Color(1f, 1f, 1f, 1f - healT);
            }

            glowImg.color = new Color(1f, 0.94f, 0.58f, Mathf.Sin(t * Mathf.PI) * 0.65f);
            glowRect.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.25f, t);
            worldRect.anchoredPosition = Random.insideUnitCircle * (5f * (1f - t));
            yield return null;
        }

        worldRect.anchoredPosition = Vector2.zero;
        if (expandingPortalRect != null)
            expandingPortalRect.gameObject.SetActive(false);
        if (infectedWorldImg != null)
            infectedWorldImg.color = new Color(1f, 1f, 1f, 0f);

        // Fase E: Destello dorado-cian final al cerrarse por completo el portal
        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.victory : null);
        float flashDuration = 0.85f;
        elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flashDuration);
            flashImg.color = new Color(1f, 0.95f, 0.72f, Mathf.Lerp(0.92f, 0f, t));
            glowImg.color = new Color(gemGlowColor.r, gemGlowColor.g, gemGlowColor.b, Mathf.Lerp(0.7f, 0f, t));
            glowRect.localScale = Vector3.one * Mathf.Lerp(0.4f, 2.6f, t);
            yield return null;
        }

        flashImg.color = new Color(1f, 0.95f, 0.72f, 0f);
        glowImg.color = new Color(gemGlowColor.r, gemGlowColor.g, gemGlowColor.b, 0f);
    }

    private static Image CreateFullscreenImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        StretchFull(rt);

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.preserveAspect = false;
        img.raycastTarget = false;
        return img;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void CustomizeFinalVictoryScreen()
    {
        if (GameManager.Instance == null || GameManager.Instance.victoryPanel == null) return;

        Transform vp = GameManager.Instance.victoryPanel.transform;

        // Buscar textos dentro de VictoryPanel para mostrar el cierre de la historia de Kai
        TextMeshProUGUI[] texts = vp.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in texts)
        {
            if (tmp == GameManager.Instance.victoryScoreText || tmp == GameManager.Instance.victoryTimeText)
                continue;

            if (tmp.gameObject.name.Contains("Title") || tmp.fontSize >= 28f)
            {
                tmp.text = "¡PORTAL SELLADO!\nKAI ES UN VERDADERO MAGO DE ARCANÍA";
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 36f;
                tmp.fontSizeMax = 66f;
                tmp.rectTransform.sizeDelta = new Vector2(1040f, 175f);
            }
        }

        if (GameManager.Instance.nextLevelButton != null)
        {
            TextMeshProUGUI btnText = GameManager.Instance.nextLevelButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (btnText != null)
            {
                btnText.text = "Volver al Menú";
            }
        }
    }

    private IEnumerator LaunchArcanePulse(Vector3 startPos, Vector3 endPos, Color pulseColor)
    {
        GameObject orb = new GameObject("ArcaneSealPulse");
        orb.transform.position = startPos;

        SpriteRenderer sr = orb.AddComponent<SpriteRenderer>();
        sr.sprite = MagicFX.GlowSprite;
        sr.color = pulseColor;
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 45;
        orb.transform.localScale = Vector3.one * 0.75f;

        MagicFX.AddSparkles(orb.transform, pulseColor, 0.22f, 28f);

        const float travelTime = 0.28f;
        float elapsed = 0f;
        while (elapsed < travelTime && orb != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelTime);
            orb.transform.position = Vector3.Lerp(startPos, endPos, t);
            orb.transform.localScale = Vector3.one * Mathf.Lerp(0.75f, 1.15f, t);
            yield return null;
        }

        MagicFX.SpawnShockwave(endPos, pulseColor, 2.6f, 0.35f);
        MagicFX.Burst(endPos, pulseColor, 36, 0.45f);

        if (orb != null)
        {
            Destroy(orb);
        }
    }
}

