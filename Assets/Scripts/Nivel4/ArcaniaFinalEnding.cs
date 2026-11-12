using System.Collections;
using System.Collections.Generic;
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
    public string pickupMessage = "¡Has obtenido la Gema de Arcanía! Su poder despierta en tu interior...";
    [TextArea]
    public string transformationMessage = "¡Te has convertido en el nuevo Mago del mundo de Arcanía!";
    [TextArea]
    public string altarIntroMessage = "El portal estuvo a punto de infectar todo el reino. Gracias a la Gema que obtuviste, ¡ahora se puede cerrar!";
    [TextArea]
    public string extendingPortalMessage = "¡Has salvado al reino! El portal se cerrará gracias a tu poder como Mago de Arcanía.";
    [TextArea]
    public string closingMessage = "¡El Portal ha sido cerrado para siempre! Arcanía vuelve a brillar en paz.";

    [Header("Atuendo de Mago para Kai (Solo Ropa)")]
    public Sprite mageClothBody;
    public Sprite mageClothLeft;
    public Sprite mageClothRight;
    public Sprite mageArmorBody;
    public Sprite mageArmorLeft;
    public Sprite mageArmorRight;
    public Sprite magePantLeft;
    public Sprite magePantRight;
    public Sprite mageCapeSprite;
    public Sprite mageHatSprite;
    public Sprite celebrationHatSprite;

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
        player.SetCutsceneWeapon(true);
        player.ForceAnimation(PlayerState.IDLE);
        yield return new WaitForSeconds(0.20f);

        // 1.5 Acercar cámara a Kai, hacerlo girar cambiando solo su ropa al atuendo de Mago de Arcanía
        EnsureCinematicSprites();
        CameraFollow camFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : FindAnyObjectByType<CameraFollow>();
        if (camFollow != null)
        {
            Vector2 closeTarget = (Vector2)player.transform.position + new Vector2(0f, 0.95f);
            camFollow.FocusCutscene(closeTarget, 2.65f);
            yield return new WaitForSeconds(0.55f);
        }

        yield return StartCoroutine(PlayKaiMageTransformationRoutine(player, dirToPortal));

        if (MessageBanner.Instance != null && !string.IsNullOrEmpty(transformationMessage))
        {
            MessageBanner.Instance.Show(transformationMessage, 4.0f);
        }
        yield return StartCoroutine(ShowCelebrationHatPopup(2.1f));

        // Reencuadrar cámara hacia Kai y el altar del portal antes de cerrarlo
        if (camFollow != null)
        {
            Vector2 altarFocus = Vector2.Lerp((Vector2)player.transform.position, (Vector2)portalPos, 0.45f) + new Vector2(0f, 0.85f);
            camFollow.FocusCutscene(altarFocus, 4.85f);
            yield return new WaitForSeconds(0.45f);
        }

        if (MessageBanner.Instance != null && !string.IsNullOrEmpty(altarIntroMessage))
        {
            MessageBanner.Instance.Show(altarIntroMessage, 4.2f);
            yield return new WaitForSeconds(2.35f);
        }

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

        // 4. Mensaje de victoria optimista y secuencia ilustrada donde Kai lanza pulsos para sellar el portal final
        if (MessageBanner.Instance != null && !string.IsNullOrEmpty(extendingPortalMessage))
        {
            MessageBanner.Instance.Show(extendingPortalMessage, 5.2f);
        }

        yield return new WaitForSeconds(1.25f);

        yield return StartCoroutine(PlayFullscreenPortalSealSequence(player));

        // 5. Mensaje de cierre sobre Arcanía restaurada
        if (MessageBanner.Instance != null)
        {
            MessageBanner.Instance.Show(closingMessage, 5.5f);
        }

        yield return new WaitForSeconds(2.4f);

        // 6. Personalizar y mostrar la Pantalla Final con opción de volver al menú
        CustomizeFinalVictoryScreen();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteLevel();
        }
    }

    private IEnumerator PlayKaiMageTransformationRoutine(PlayerController player, float finalFacing)
    {
        Transform unitRoot = player.FindUnitRoot();
        Vector3 origLocalPos = unitRoot.localPosition;
        Quaternion origLocalRot = unitRoot.localRotation;

        Vector3 centerPos = player.transform.position + new Vector3(0f, 0.55f, 0f);
        MagicFX.SpawnShockwave(centerPos, new Color(0.75f, 0.35f, 1f, 0.9f), 2.5f, 0.45f);
        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.magicBolt : null);

        bool outfitSwapped = false;
        const float duration = 1.85f;
        const int spinTurns = 4;
        float elapsed = 0f;
        float sparkleTimer = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = Mathf.SmoothStep(0f, 1f, t);

            float angleY = ease * 360f * spinTurns;
            float liftY = Mathf.Sin(t * Mathf.PI) * 0.32f;

            unitRoot.localRotation = Quaternion.Euler(0f, angleY, 0f);
            unitRoot.localPosition = origLocalPos + new Vector3(0f, liftY, 0f);

            sparkleTimer -= Time.deltaTime;
            if (sparkleTimer <= 0f)
            {
                sparkleTimer = 0.22f;
                Vector3 fxPos = player.transform.position + new Vector3(0f, 0.55f + liftY, 0f);
                MagicFX.Burst(fxPos, Color.Lerp(portalColor, gemGlowColor, t), 14, 0.35f);
            }

            if (!outfitSwapped && t >= 0.48f)
            {
                outfitSwapped = true;
                player.TransformToMageOutfit(
                    mageClothBody, mageClothLeft, mageClothRight,
                    mageArmorBody, mageArmorLeft, mageArmorRight,
                    magePantLeft, magePantRight,
                    mageCapeSprite, mageHatSprite, 1.48f);

                Vector3 burstPos = player.transform.position + new Vector3(0f, 0.55f + liftY, 0f);
                MagicFX.SpawnShockwave(burstPos, new Color(1f, 0.92f, 0.45f, 0.95f), 3.4f, 0.50f);
                MagicFX.Burst(burstPos, new Color(1f, 0.92f, 0.45f, 1f), 42, 0.55f);
                MagicFX.Burst(burstPos, portalColor, 34, 0.50f);
                AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.gemFragmentPickup : null);
            }

            yield return null;
        }

        unitRoot.localRotation = origLocalRot;
        unitRoot.localPosition = origLocalPos;
        player.FaceDirection(finalFacing);
        player.ForceAnimation(PlayerState.IDLE);
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

        const string clothPath = "Assets/Sprites/KaiMageRobe_Cloth.png";
        const string armorPath = "Assets/Sprites/KaiMageRobe_Armor.png";
        const string pantPath = "Assets/SPUM/Resources/Addons/Ver300/0_Unit/0_Sprite/5_Pant/New_Pant_12.png";

        if (mageClothBody == null) mageClothBody = LoadSubSpriteAsset(clothPath, "Body");
        if (mageClothLeft == null) mageClothLeft = LoadSubSpriteAsset(clothPath, "Left");
        if (mageClothRight == null) mageClothRight = LoadSubSpriteAsset(clothPath, "Right");
        if (mageArmorBody == null) mageArmorBody = LoadSubSpriteAsset(armorPath, "Body");
        if (mageArmorLeft == null) mageArmorLeft = LoadSubSpriteAsset(armorPath, "Left");
        if (mageArmorRight == null) mageArmorRight = LoadSubSpriteAsset(armorPath, "Right");
        if (magePantLeft == null) magePantLeft = LoadSubSpriteAsset(pantPath, "Left");
        if (magePantRight == null) magePantRight = LoadSubSpriteAsset(pantPath, "Right");
        if (mageHatSprite == null) mageHatSprite = LoadSpriteAsset("Assets/Sprites/KaiMageHat.png");
        if (celebrationHatSprite == null) celebrationHatSprite = LoadSpriteAsset("Assets/Coin_Gems/assets/assets/sombrero.png");
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

    private static Sprite LoadSubSpriteAsset(string assetPath, string subName)
    {
        Object[] all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (Object obj in all)
        {
            if (obj is Sprite sprite && sprite.name == subName)
                return sprite;
        }
        return null;
    }
#endif

    private IEnumerator PlayFullscreenPortalSealSequence(PlayerController player)
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

        // 4.5 Kai con su nuevo atuendo de Mago de Arcanía en el risco derecho mirando al portal
        RectTransform kaiUiRect = null;
        Image staffGlowImg = null;
        if (player != null)
        {
            player.FaceDirection(-1f);
            player.SetCutsceneWeapon(true);
            player.ForceAnimation(PlayerState.IDLE);
            yield return null;
            BuildUiKaiPortrait(worldRect, player, out kaiUiRect, out staffGlowImg);
        }

        Canvas.ForceUpdateCanvases();

        // 5. Destello final de pantalla completa
        Image flashImg = CreateFullscreenImage("FlashOverlay", rootRect, null, new Color(1f, 0.95f, 0.72f, 0f));

        // Fase A: Fundido de entrada hacia la escena del portal
        float fadeInDuration = 0.55f;
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            rootGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        rootGroup.alpha = 1f;

        // Fase B: El portal vibra mientras Kai carga energía en su bastón
        float chargeDuration = 1.05f;
        elapsed = 0f;
        while (elapsed < chargeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / chargeDuration);
            if (expandingPortalRect != null)
            {
                float scale = Mathf.Lerp(0.96f, 1.06f, t) + Mathf.Sin(t * Mathf.PI * 3f) * 0.016f;
                expandingPortalRect.localScale = new Vector3(scale, scale, 1f);
            }
            if (staffGlowImg != null)
            {
                staffGlowImg.color = new Color(gemGlowColor.r, gemGlowColor.g, gemGlowColor.b, 0.35f + 0.55f * Mathf.Sin(t * Mathf.PI * 2f));
                staffGlowImg.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.25f, t);
            }
            if (kaiUiRect != null)
            {
                kaiUiRect.anchoredPosition = new Vector2(0f, Mathf.Sin(t * Mathf.PI * 2f) * 6f);
            }
            glowImg.color = new Color(portalColor.r, portalColor.g, portalColor.b, Mathf.Sin(t * Mathf.PI) * 0.38f);
            glowRect.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.20f, t);
            worldRect.anchoredPosition = Random.insideUnitCircle * 3.5f;
            yield return null;
        }
        worldRect.anchoredPosition = Vector2.zero;
        if (kaiUiRect != null) kaiUiRect.anchoredPosition = Vector2.zero;

        // Fase C: Kai lanza 3 pulsos mágicos desde su bastón hacia el portal principal
        for (int i = 0; i < 3; i++)
        {
            AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.playerAttack : null);
            AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.magicBolt : null);

            Color pulseTint = Color.Lerp(gemGlowColor, new Color(1f, 0.94f, 0.55f, 1f), (i + 1) / 3f);

            if (kaiUiRect != null)
            {
                kaiUiRect.anchoredPosition = new Vector2(-18f, 10f);
                kaiUiRect.localRotation = Quaternion.Euler(0f, 0f, 6f);
            }
            if (staffGlowImg != null)
            {
                staffGlowImg.color = new Color(pulseTint.r, pulseTint.g, pulseTint.b, 1f);
                staffGlowImg.rectTransform.localScale = Vector3.one * 1.45f;
            }

            Vector2 startLocal = staffGlowImg != null
                ? (Vector2)worldRect.InverseTransformPoint(staffGlowImg.rectTransform.position)
                : new Vector2(410f, -150f);
            Vector2 endLocal = (Vector2)worldRect.InverseTransformPoint(glowRect.position);

            yield return StartCoroutine(LaunchUiArcanePulse(worldRect, startLocal, endLocal, pulseTint));

            if (kaiUiRect != null)
            {
                kaiUiRect.anchoredPosition = Vector2.zero;
                kaiUiRect.localRotation = Quaternion.identity;
            }
            if (staffGlowImg != null)
            {
                staffGlowImg.color = new Color(gemGlowColor.r, gemGlowColor.g, gemGlowColor.b, 0.45f);
                staffGlowImg.rectTransform.localScale = Vector3.one * 0.85f;
            }

            StartCoroutine(SpawnUiShockwave(worldRect, endLocal, pulseTint));

            float impactDuration = 0.36f;
            elapsed = 0f;
            while (elapsed < impactDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / impactDuration);
                float wave = Mathf.Sin(t * Mathf.PI);

                glowImg.color = new Color(pulseTint.r, pulseTint.g, pulseTint.b, wave * 0.90f);
                glowRect.localScale = Vector3.one * Mathf.Lerp(0.50f, 1.90f, t);

                if (expandingPortalImg != null)
                {
                    expandingPortalImg.color = Color.Lerp(Color.white, new Color(0.75f, 1f, 1f, 1f), wave * 0.7f);
                    float recoil = Mathf.Lerp(1.05f, 0.97f, t) - wave * 0.045f;
                    expandingPortalRect.localScale = new Vector3(recoil, recoil, 1f);
                }

                worldRect.anchoredPosition = Random.insideUnitCircle * (7.5f * (1f - t));
                yield return null;
            }

            if (expandingPortalImg != null)
                expandingPortalImg.color = Color.white;
            worldRect.anchoredPosition = Vector2.zero;
            yield return new WaitForSeconds(0.10f);
        }

        if (staffGlowImg != null)
            staffGlowImg.color = new Color(gemGlowColor.r, gemGlowColor.g, gemGlowColor.b, 0f);

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
                tmp.text = "¡PORTAL SELLADO!\nKAI ES EL NUEVO MAGO DE ARCANÍA";
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

    private void BuildUiKaiPortrait(RectTransform worldRect, PlayerController player, out RectTransform kaiUiRect, out Image staffGlowImg)
    {
        GameObject kaiGo = new GameObject("UiKaiMage", typeof(RectTransform));
        kaiUiRect = kaiGo.GetComponent<RectTransform>();
        kaiUiRect.SetParent(worldRect, false);
        kaiUiRect.anchorMin = new Vector2(0.76f, 0.21f);
        kaiUiRect.anchorMax = new Vector2(0.76f, 0.21f);
        kaiUiRect.pivot = new Vector2(0.5f, 0f);
        kaiUiRect.sizeDelta = Vector2.zero;
        kaiUiRect.anchoredPosition = Vector2.zero;

        // Aura mágica bajo los pies de Kai en el risco
        GameObject auraGo = new GameObject("KaiMageGroundAura", typeof(RectTransform), typeof(Image));
        RectTransform auraRect = auraGo.GetComponent<RectTransform>();
        auraRect.SetParent(kaiUiRect, false);
        auraRect.anchorMin = new Vector2(0.5f, 0.5f);
        auraRect.anchorMax = new Vector2(0.5f, 0.5f);
        auraRect.pivot = new Vector2(0.5f, 0.5f);
        auraRect.sizeDelta = new Vector2(280f, 140f);
        auraRect.anchoredPosition = new Vector2(0f, 20f);

        Image auraImg = auraGo.GetComponent<Image>();
        auraImg.sprite = MagicFX.GlowSprite;
        auraImg.color = new Color(portalColor.r, portalColor.g, portalColor.b, 0.48f);
        auraImg.raycastTarget = false;

        List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        foreach (SpriteRenderer sr in player.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!sr.enabled || !sr.gameObject.activeInHierarchy || sr.sprite == null)
                continue;
            string n = sr.gameObject.name;
            if (n.Contains("Shockwave") || n.Contains("Particle") || n.Contains("Trail"))
                continue;
            renderers.Add(sr);
        }

        renderers.Sort((a, b) =>
        {
            int keyA = SortingLayer.GetLayerValueFromID(a.sortingLayerID) * 10000 + a.sortingOrder;
            int keyB = SortingLayer.GetLayerValueFromID(b.sortingLayerID) * 10000 + b.sortingOrder;
            return keyA.CompareTo(keyB);
        });

        const float pixelsPerWorldUnit = 190f;
        Vector3 originWorld = player.transform.position;
        Vector2 staffTipUi = new Vector2(-80f, 125f);
        bool foundStaffTip = false;

        foreach (SpriteRenderer sr in renderers)
        {
            Sprite sp = sr.sprite;
            Vector2 spSize = sp.rect.size;
            if (spSize.x <= 0f || spSize.y <= 0f || sp.pixelsPerUnit <= 0f)
                continue;

            GameObject partGo = new GameObject("Ui_" + sr.gameObject.name, typeof(RectTransform), typeof(Image));
            RectTransform partRect = partGo.GetComponent<RectTransform>();
            partRect.SetParent(kaiUiRect, false);
            partRect.anchorMin = new Vector2(0.5f, 0.5f);
            partRect.anchorMax = new Vector2(0.5f, 0.5f);
            partRect.pivot = new Vector2(sp.pivot.x / spSize.x, sp.pivot.y / spSize.y);
            partRect.sizeDelta = (spSize / sp.pixelsPerUnit) * pixelsPerWorldUnit;

            Vector3 relWorld = sr.transform.position - originWorld;
            partRect.anchoredPosition = new Vector2(relWorld.x, relWorld.y) * pixelsPerWorldUnit;

            Vector2 rightVec = sr.transform.TransformVector(Vector3.right);
            Vector2 upVec = sr.transform.TransformVector(Vector3.up);
            if (sr.flipX) rightVec = -rightVec;
            if (sr.flipY) upVec = -upVec;

            float cross = rightVec.x * upVec.y - rightVec.y * upVec.x;
            float scaleX = rightVec.magnitude * (cross < 0f ? -1f : 1f);
            float scaleY = upVec.magnitude;
            float angleDeg = cross < 0f
                ? Mathf.Atan2(-rightVec.y, -rightVec.x) * Mathf.Rad2Deg
                : Mathf.Atan2(rightVec.y, rightVec.x) * Mathf.Rad2Deg;

            partRect.localScale = new Vector3(scaleX, scaleY, 1f);
            partRect.localRotation = Quaternion.Euler(0f, 0f, angleDeg);

            Image img = partGo.GetComponent<Image>();
            img.sprite = sp;
            img.color = sr.color;
            img.preserveAspect = false;
            img.raycastTarget = false;

            if (sr.gameObject.name.Contains("Weapon") || sr.gameObject.name.Contains("Staff") ||
                (sp.name != null && sp.name.ToLowerInvariant().Contains("staff")))
            {
                Vector3 tipWorld = sr.bounds.center + new Vector3(-sr.bounds.extents.x * 0.6f, sr.bounds.extents.y * 0.7f, 0f);
                staffTipUi = (Vector2)(tipWorld - originWorld) * pixelsPerWorldUnit;
                foundStaffTip = true;
            }
        }

        if (!foundStaffTip)
            staffTipUi = new Vector2(-82f, 128f);

        GameObject tipGlowGo = new GameObject("UiKaiStaffTipGlow", typeof(RectTransform), typeof(Image));
        RectTransform tipRect = tipGlowGo.GetComponent<RectTransform>();
        tipRect.SetParent(kaiUiRect, false);
        tipRect.anchorMin = new Vector2(0.5f, 0.5f);
        tipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tipRect.pivot = new Vector2(0.5f, 0.5f);
        tipRect.sizeDelta = new Vector2(88f, 88f);
        tipRect.anchoredPosition = staffTipUi;

        staffGlowImg = tipGlowGo.GetComponent<Image>();
        staffGlowImg.sprite = MagicFX.GlowSprite;
        staffGlowImg.color = new Color(gemGlowColor.r, gemGlowColor.g, gemGlowColor.b, 0f);
        staffGlowImg.raycastTarget = false;
    }

    private IEnumerator LaunchUiArcanePulse(RectTransform worldRect, Vector2 startLocal, Vector2 endLocal, Color pulseColor)
    {
        GameObject boltGo = new GameObject("UiArcanePulse", typeof(RectTransform), typeof(Image));
        RectTransform boltRect = boltGo.GetComponent<RectTransform>();
        boltRect.SetParent(worldRect, false);
        boltRect.anchorMin = new Vector2(0.5f, 0.5f);
        boltRect.anchorMax = new Vector2(0.5f, 0.5f);
        boltRect.pivot = new Vector2(0.5f, 0.5f);
        boltRect.sizeDelta = new Vector2(115f, 115f);
        boltRect.anchoredPosition = startLocal;

        Image boltImg = boltGo.GetComponent<Image>();
        boltImg.sprite = MagicFX.GlowSprite;
        boltImg.color = pulseColor;
        boltImg.raycastTarget = false;

        GameObject coreGo = new GameObject("Core", typeof(RectTransform), typeof(Image));
        RectTransform coreRect = coreGo.GetComponent<RectTransform>();
        coreRect.SetParent(boltRect, false);
        coreRect.anchorMin = new Vector2(0.5f, 0.5f);
        coreRect.anchorMax = new Vector2(0.5f, 0.5f);
        coreRect.pivot = new Vector2(0.5f, 0.5f);
        coreRect.sizeDelta = new Vector2(46f, 46f);
        coreRect.anchoredPosition = Vector2.zero;

        Image coreImg = coreGo.GetComponent<Image>();
        coreImg.sprite = MagicFX.GlowSprite;
        coreImg.color = Color.white;
        coreImg.raycastTarget = false;

        const float travelTime = 0.28f;
        float elapsed = 0f;
        float trailCooldown = 0f;

        while (elapsed < travelTime && boltRect != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelTime);
            Vector2 arcOffset = Vector2.up * (Mathf.Sin(t * Mathf.PI) * 34f);
            Vector2 pos = Vector2.Lerp(startLocal, endLocal, t) + arcOffset;
            boltRect.anchoredPosition = pos;
            boltRect.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.35f, t);

            trailCooldown -= Time.deltaTime;
            if (trailCooldown <= 0f)
            {
                trailCooldown = 0.045f;
                StartCoroutine(SpawnUiSparkleDot(worldRect, pos, pulseColor));
            }

            yield return null;
        }

        if (boltGo != null)
            Destroy(boltGo);
    }

    private static IEnumerator SpawnUiSparkleDot(RectTransform worldRect, Vector2 localPos, Color color)
    {
        GameObject dotGo = new GameObject("UiPulseTrail", typeof(RectTransform), typeof(Image));
        RectTransform rt = dotGo.GetComponent<RectTransform>();
        rt.SetParent(worldRect, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(48f, 48f);
        rt.anchoredPosition = localPos + Random.insideUnitCircle * 10f;

        Image img = dotGo.GetComponent<Image>();
        img.sprite = MagicFX.GlowSprite;
        img.color = color;
        img.raycastTarget = false;

        const float life = 0.22f;
        float t = 0f;
        while (t < life && rt != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / life);
            rt.localScale = Vector3.one * (1f - k * 0.8f);
            img.color = new Color(color.r, color.g, color.b, (1f - k) * 0.7f);
            yield return null;
        }

        if (dotGo != null)
            Destroy(dotGo);
    }

    private static IEnumerator SpawnUiShockwave(RectTransform worldRect, Vector2 localPos, Color color)
    {
        GameObject ringGo = new GameObject("UiPortalShockwave", typeof(RectTransform), typeof(Image));
        RectTransform rt = ringGo.GetComponent<RectTransform>();
        rt.SetParent(worldRect, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(180f, 180f);
        rt.anchoredPosition = localPos;

        Image img = ringGo.GetComponent<Image>();
        img.sprite = MagicFX.GlowSprite;
        img.color = color;
        img.raycastTarget = false;

        const float life = 0.36f;
        float t = 0f;
        while (t < life && rt != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / life);
            rt.localScale = Vector3.one * Mathf.Lerp(0.4f, 3.1f, k);
            img.color = new Color(color.r, color.g, color.b, (1f - k) * 0.9f);
            yield return null;
        }

        if (ringGo != null)
            Destroy(ringGo);
    }

    private IEnumerator ShowCelebrationHatPopup(float duration)
    {
        if (celebrationHatSprite == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        Canvas targetCanvas = MessageBanner.Instance != null
            ? MessageBanner.Instance.GetComponentInParent<Canvas>()
            : FindAnyObjectByType<Canvas>();

        if (targetCanvas == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        GameObject popupGo = new GameObject("CelebrationHatPopup", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform popupRt = popupGo.GetComponent<RectTransform>();
        popupRt.SetParent(targetCanvas.transform, false);
        popupRt.SetAsLastSibling();
        popupRt.anchorMin = new Vector2(0.5f, 0.68f);
        popupRt.anchorMax = new Vector2(0.5f, 0.68f);
        popupRt.pivot = new Vector2(0.5f, 0.5f);
        popupRt.sizeDelta = new Vector2(420f, 290f);
        popupRt.anchoredPosition = Vector2.zero;

        CanvasGroup cg = popupGo.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        GameObject glowGo = new GameObject("HatGlow", typeof(RectTransform), typeof(Image));
        RectTransform glowRt = glowGo.GetComponent<RectTransform>();
        glowRt.SetParent(popupRt, false);
        glowRt.anchorMin = new Vector2(0.5f, 0.5f);
        glowRt.anchorMax = new Vector2(0.5f, 0.5f);
        glowRt.sizeDelta = new Vector2(460f, 360f);
        Image glowImg = glowGo.GetComponent<Image>();
        glowImg.sprite = MagicFX.GlowSprite;
        glowImg.color = new Color(0.85f, 0.48f, 1f, 0.65f);
        glowImg.raycastTarget = false;

        GameObject hatGo = new GameObject("HatImage", typeof(RectTransform), typeof(Image));
        RectTransform hatRt = hatGo.GetComponent<RectTransform>();
        hatRt.SetParent(popupRt, false);
        StretchFull(hatRt);
        Image hatImg = hatGo.GetComponent<Image>();
        hatImg.sprite = celebrationHatSprite;
        hatImg.preserveAspect = true;
        hatImg.raycastTarget = false;

        float inTime = 0.28f;
        float outTime = 0.28f;
        float holdTime = Mathf.Max(0.2f, duration - inTime - outTime);

        float elapsed = 0f;
        while (elapsed < inTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / inTime);
            float overshoot = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.08f;
            popupRt.localScale = Vector3.one * overshoot;
            cg.alpha = t;
            yield return null;
        }
        popupRt.localScale = Vector3.one;
        cg.alpha = 1f;

        elapsed = 0f;
        while (elapsed < holdTime)
        {
            elapsed += Time.deltaTime;
            popupRt.anchoredPosition = new Vector2(0f, Mathf.Sin(elapsed * 4.5f) * 10f);
            popupRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 3.5f) * 4f);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < outTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / outTime);
            popupRt.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, t);
            cg.alpha = 1f - t;
            yield return null;
        }

        Destroy(popupGo);
    }
}

