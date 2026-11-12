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
    public bool completesLevel = false;
    [TextArea]
    public string message = "¡Has obtenido el primer fragmento de la Gema de Arcanía!";
    public float messageSeconds = 4f;

    [Header("Recompensa")]
    public int damageBonus = 4;
    public int scoreValue = 50;

    [Header("Efecto de luz")]
    public Color glowColor = new Color(0.45f, 1f, 0.65f, 0.8f);
    public float glowSize = 1.8f;

    private bool collected;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            if (col is BoxCollider2D box && (box.size.x < 0.8f || box.size.y < 0.8f))
            {
                box.size = new Vector2(Mathf.Max(box.size.x, 0.9f), Mathf.Max(box.size.y, 0.9f));
            }
        }

        MagicFX.AddGlow(transform, glowColor, glowSize);
        MagicFX.AddSparkles(transform, glowColor, 0.3f, 14f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void TryCollect(Collider2D other)
    {
        if (collected)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            player = other.GetComponent<PlayerController>();
        if (player == null)
            return;

        if (Vector2.Distance(transform.position, player.transform.position) > 3.0f)
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

        if ((completesLevel || fragmentNumber >= 3) && GameManager.Instance != null)
        {
            GameManager.Instance.CompleteLevel();
        }

        Destroy(gameObject);
    }
}