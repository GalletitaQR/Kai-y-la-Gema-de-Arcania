using UnityEngine;

/// <summary>
/// Cristal de Poder Arcano (Nivel 4 - Sala del Trono).
/// Lo genera el Dragón Guardián durante la pelea. Igual que los fragmentos de la Gema,
/// al tomarlo Kai gana daño extra permanente (y recupera un poco de vida),
/// para que el combate contra el jefe no se haga eterno.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ArcanePowerCrystal : MonoBehaviour
{
    public int damageBonus = 15;
    public int healAmount = 10;
    public Color glowColor = new Color(1f, 0.62f, 0.15f, 0.9f);
    public float lifeTime = 14f;

    private bool collected;
    private Vector3 basePos;
    private float spawnTime;

    /// <summary>Crea un cristal en tiempo de ejecución (sin necesidad de prefab).</summary>
    public static ArcanePowerCrystal Spawn(Vector3 position, Sprite sprite, int bonus)
    {
        GameObject go = new GameObject("CristalPoderArcano");
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(1f, 0.75f, 0.35f, 1f);
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 12;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.75f;

        ArcanePowerCrystal crystal = go.AddComponent<ArcanePowerCrystal>();
        crystal.damageBonus = bonus;
        return crystal;
    }

    private void Start()
    {
        basePos = transform.position;
        spawnTime = Time.time;
        MagicFX.AddGlow(transform, glowColor, 1.6f);
        MagicFX.AddSparkles(transform, glowColor, 0.3f, 12f);
        MagicFX.Burst(transform.position, glowColor, 20, 0.4f);
    }

    private void Update()
    {
        transform.position = basePos + new Vector3(0f, Mathf.Sin(Time.time * 3f) * 0.15f, 0f);

        // Parpadea en los últimos 3 segundos y luego desaparece
        float remaining = lifeTime - (Time.time - spawnTime);
        if (remaining < 3f)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = Mathf.Repeat(Time.time * 8f, 1f) > 0.35f;
        }
        if (remaining <= 0f) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other) { TryCollect(other); }
    private void OnTriggerStay2D(Collider2D other) { TryCollect(other); }

    private void TryCollect(Collider2D other)
    {
        if (collected || other == null) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player.IsDead) return;

        collected = true;
        player.AddDamageBonus(damageBonus);
        player.Heal(healAmount);
        player.PlayAbsorbFeedback(glowColor);

        MagicFX.Burst(transform.position, glowColor, 35, 0.45f);
        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.gemFragmentPickup : null);

        if (MessageBanner.Instance != null)
            MessageBanner.Instance.Show($"¡Cristal de Poder Arcano! +{damageBonus} de daño", 2.5f);

        Destroy(gameObject);
    }
}
