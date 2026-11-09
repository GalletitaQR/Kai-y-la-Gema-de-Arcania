using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public string closingMessage = "¡El Portal ha sido cerrado para siempre! Kai se ha convertido en un Verdadero Mago de Arcanía.";

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
        if (!other.CompareTag("Player")) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        sequenceStarted = true;
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

        player.SetControlEnabled(false);
        player.AddDamageBonus(25);
        player.PlayAbsorbFeedback(gemGlowColor);

        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.gemFragmentPickup : null);
        MagicFX.Burst(transform.position, gemGlowColor, 50, 0.55f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
        }

        if (MessageBanner.Instance != null)
        {
            MessageBanner.Instance.Show(pickupMessage, 3.6f);
        }

        yield return new WaitForSeconds(1.1f);

        // 1. Kai camina frente al Portal de Arcania en el Altar del Trono
        Vector3 portalPos = portalTransform != null
            ? portalTransform.position
            : new Vector3(132.5f, 1.35f, 0f);

        float targetKaiX = portalPos.x - 1.9f;
        Rigidbody2D body = player.Body;
        float walkTimeout = 2.2f;

        while (body != null && Mathf.Abs(player.transform.position.x - targetKaiX) > 0.15f && walkTimeout > 0f)
        {
            walkTimeout -= Time.deltaTime;
            float dir = Mathf.Sign(targetKaiX - player.transform.position.x);
            player.FaceDirection(dir);
            player.SetAnimation(PlayerState.MOVE);
            body.linearVelocity = new Vector2(dir * 2.5f, body.linearVelocity.y);
            yield return null;
        }

        if (body != null)
        {
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }

        player.FaceDirection(1f);
        player.SetAnimation(PlayerState.IDLE);
        yield return new WaitForSeconds(0.35f);

        // 2. Kai canaliza la Gema de Arcania y lanza 3 pulsos mágicos hacia el portal
        for (int i = 0; i < 3; i++)
        {
            player.SetAnimation(PlayerState.ATTACK);
            AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.playerAttack : null);

            Vector3 castOrigin = player.transform.position + new Vector3(0.6f, 0.45f, 0f);
            MagicFX.SpawnShockwave(castOrigin, gemGlowColor, 2.2f, 0.35f);
            MagicFX.Burst(castOrigin, gemGlowColor, 22, 0.35f);
            MagicFX.Burst(portalPos, Color.Lerp(portalColor, gemGlowColor, (i + 1) / 3f), 32, 0.45f);

            yield return new WaitForSeconds(0.55f);
            player.SetAnimation(PlayerState.IDLE);
            yield return new WaitForSeconds(0.2f);
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

        // Gran destello final al cerrarse el portal
        MagicFX.SpawnShockwave(portalPos, new Color(1f, 0.92f, 0.45f, 0.95f), 4.0f, 0.6f);
        MagicFX.Burst(portalPos, new Color(1f, 0.92f, 0.45f, 1f), 65, 0.7f);
        MagicFX.Burst(portalPos, gemGlowColor, 55, 0.65f);

        if (MessageBanner.Instance != null)
        {
            MessageBanner.Instance.Show(closingMessage, 5.5f);
        }

        yield return new WaitForSeconds(2.6f);

        // 4. Personalizar y mostrar la Pantalla Final con opción de volver al menú
        CustomizeFinalVictoryScreen();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteLevel();
        }
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
}
