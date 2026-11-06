using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Portal de salida al final del nivel.
/// Al entrar Kai: camina al centro del portal, realiza un salto victorioso hacia arriba
/// (sin dar giros ni vueltas), suena la música/fanfarria de victoria, se muestra el mensaje,
/// y la pantalla se desvanece suavemente a negro para transportar a Kai al siguiente nivel.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelPortal : MonoBehaviour
{
    [Header("Destino")]
    [Tooltip("Nombre de la siguiente escena. Si se deja vacía, usará GameManager.nextSceneName o MainMenu.")]
    public string nextSceneName = "";

    [Header("Efectos Visuales")]
    public Color glowColor = new Color(0.75f, 0.35f, 1f, 0.9f);
    public float glowSize = 2.2f;

    [Header("Secuencia de Victoria")]
    public float walkSpeed = 2.2f;
    public float jumpForce = 6.0f;
    [TextArea]
    public string message = "¡Has completado Las Cavernas Perdidas! Avanzando al siguiente nivel...";
    public float messageSeconds = 4f;
    public int scoreValue = 100;
    public float fadeDuration = 2.0f;

    private bool triggered = false;

    private void Awake()
    {
        MagicFX.AddGlow(transform, glowColor, glowSize);
        MagicFX.AddSparkles(transform, glowColor, 0.4f, 18f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player.IsDead) return;

        triggered = true;
        StartCoroutine(PortalSequence(player));
    }

    private IEnumerator PortalSequence(PlayerController player)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Cutscene);

        player.SetControlEnabled(false);
        Rigidbody2D body = player.Body;

        // 1. Caminar suavemente hacia el centro del portal
        float timeout = 2.5f;
        while (Mathf.Abs(player.transform.position.x - transform.position.x) > 0.15f && timeout > 0f)
        {
            float dir = Mathf.Sign(transform.position.x - player.transform.position.x);
            player.FaceDirection(dir);
            player.SetAnimation(PlayerState.MOVE);
            body.linearVelocity = new Vector2(dir * walkSpeed, body.linearVelocity.y);
            timeout -= Time.deltaTime;
            yield return null;
        }

        body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        player.SetAnimation(PlayerState.IDLE);
        yield return new WaitForSeconds(0.2f);

        // 2. Salto de victoria (hacia arriba, sin dar vueltas ni rotaciones)
        body.linearVelocity = new Vector2(0f, jumpForce);
        player.SetAnimation(PlayerState.MOVE);

        // 3. Efectos de partículas arcanas y sonido de victoria
        MagicFX.Burst(transform.position, glowColor, 60, 0.6f);
        MagicFX.Burst(player.transform.position + Vector3.up * 0.5f, glowColor, 40, 0.5f);
        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.victory : null);

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreValue);

        if (MessageBanner.Instance != null)
            MessageBanner.Instance.Show(message, messageSeconds);

        // 4. Esperar que alcance el punto alto del salto
        yield return new WaitForSeconds(0.6f);

        // 5. Destino de la siguiente escena
        string targetScene = nextSceneName;
        if (string.IsNullOrEmpty(targetScene) && GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.nextSceneName))
        {
            targetScene = GameManager.Instance.nextSceneName;
        }

        if (string.IsNullOrEmpty(targetScene))
        {
            targetScene = "MainMenu";
        }

        SaveSystem.SaveScene(targetScene);

        // 6. Transición con desvanecimiento a negro
        ScreenFader.FadeAndLoad(targetScene, fadeDuration);
    }
}

