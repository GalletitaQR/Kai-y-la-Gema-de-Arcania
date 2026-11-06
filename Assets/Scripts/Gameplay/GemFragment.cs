using UnityEngine;

/// <summary>
/// A fragment of the Arcania Gem. Glows and sparkles while waiting; when Kai takes it he gets
/// the damage bonus, absorbs the energy (small growth + burst of light) and a message appears.
/// Put it on the gem object (trigger collider).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GemFragment : MonoBehaviour
{
    [Header("Fragmento")]
    public int fragmentNumber = 1;
    public bool unlocksCape = false;
    [TextArea]
    public string message = "¡Has obtenido el primer fragmento de la Gema de Arcanía!";
    public float messageSeconds = 4f;

    [Header("Recompensa")]
    public int damageBonus = 10;
    public int scoreValue = 50;

    [Header("Efecto de luz")]
    public Color glowColor = new Color(0.45f, 1f, 0.65f, 0.8f);
    public float glowSize = 1.8f;

    private bool collected;

    private void Awake()
    {
        MagicFX.AddGlow(transform, glowColor, glowSize);
        MagicFX.AddSparkles(transform, glowColor, 0.3f, 14f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        // Seguridad estricta: Kai debe estar físicamente junto a la gema (<= 2.2 unidades).
        // Si la distancia es mayor (ej. saltos o checks al inicio del nivel), se descarta de inmediato.
        if (Vector2.Distance(transform.position, player.transform.position) > 2.2f)
            return;

        collected = true;
        Collect(player);
    }

    private void Collect(PlayerController player)
    {
        MagicFX.Burst(transform.position, glowColor, 45, 0.4f);

        player.AddDamageBonus(damageBonus);
        player.PlayAbsorbFeedback(glowColor);

        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.gemFragmentPickup : null);

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreValue);

        SaveSystem.GemFragments = Mathf.Max(SaveSystem.GemFragments, fragmentNumber);
        if (unlocksCape)
        {
            SaveSystem.HasCape = true;
            player.EquipCape();
        }

        if (MessageBanner.Instance != null)
            MessageBanner.Instance.Show(message, messageSeconds);

        Destroy(gameObject);
    }
}
