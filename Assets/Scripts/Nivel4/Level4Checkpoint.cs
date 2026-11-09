using UnityEngine;

/// <summary>
/// Punto de control (Checkpoint) del Nivel 4 ("Templo de Arcania").
/// Se coloca en los Tramos 3, 4 y 5.
/// Al tocarlo Kai:
/// - Guarda su posición de reaparición (si Kai muere, reaparece en el último checkpoint activado).
/// - Restaura parte de su vida.
/// - Enciende la antorcha/cristal mágico del pedestal y muestra un aviso en pantalla.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Level4Checkpoint : MonoBehaviour
{
    [Header("Configuración del Checkpoint")]
    public int checkpointIndex = 1;
    public Vector3 respawnOffset = new Vector3(0f, 0.55f, 0f);
    public int healAmount = 35;
    public GameObject activeFlameObject;

    [Header("Efecto Visual")]
    public Color activeGlowColor = new Color(0.3f, 0.92f, 1f, 0.85f);

    private bool activated = false;
    private SpriteRenderer pedestalRenderer;

    private void Awake()
    {
        pedestalRenderer = GetComponent<SpriteRenderer>();
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (activeFlameObject != null)
        {
            activeFlameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated || other == null) return;
        if (!other.CompareTag("Player")) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        ActivateCheckpoint(player);
    }

    private void ActivateCheckpoint(PlayerController player)
    {
        activated = true;

        Vector3 spawnPos = transform.position + respawnOffset;
        player.SetCheckpoint(spawnPos);
        player.Heal(healAmount);

        if (activeFlameObject != null)
        {
            activeFlameObject.SetActive(true);
        }

        if (pedestalRenderer != null)
        {
            pedestalRenderer.color = new Color(0.85f, 0.98f, 1f, 1f);
        }

        MagicFX.AddGlow(transform, activeGlowColor, 1.8f);
        MagicFX.AddSparkles(transform, activeGlowColor, 0.25f, 10f);
        MagicFX.Burst(transform.position + Vector3.up * 0.8f, activeGlowColor, 24, 0.4f);

        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.heartPickup : null);

        if (MessageBanner.Instance != null)
        {
            MessageBanner.Instance.Show($"¡Punto de Control {checkpointIndex} alcanzado! Vida restaurada.", 3.0f);
        }
    }
}
