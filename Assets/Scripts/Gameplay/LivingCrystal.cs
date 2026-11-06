using System.Collections;
using UnityEngine;

/// <summary>
/// Cristal Viviente: Enemigo místico inmóvil del Nivel 2.
/// Pulsa constantemente con energía arcana. Causa daño por choque o contacto a corta distancia.
/// Puede ser destruido con golpes de espada o impactos de MagicBolt.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LivingCrystal : MonoBehaviour, IEnemigo
{
    [Header("Vida")]
    public int maxHealth = 40;
    public int currentHealth;

    [Header("Pulsación y Efectos")]
    public float pulseSpeed = 3.5f;
    public float pulseScale = 0.15f;
    public Color crystalColor = new Color(0.75f, 0.3f, 1f, 1f);

    [Header("Combate")]
    public int contactDamage = 18;
    public float damageCooldown = 1f;

    [Header("Puntaje")]
    public int scoreValue = 40;

    private SpriteRenderer sr;
    private Vector3 baseScale;
    private bool isDead = false;
    private float lastDamageTime = 0f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 5;
        }
        currentHealth = maxHealth;
        baseScale = transform.localScale;

        MagicFX.AddGlow(transform, crystalColor, 1.4f);
        MagicFX.AddSparkles(transform, crystalColor, 0.25f, 8f);
    }

    private void Update()
    {
        if (isDead) return;

        // Animación de respiración/pulsación del cristal
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
        transform.localScale = baseScale * (1f + pulse);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        DamagePlayerIfPossible(other.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        DamagePlayerIfPossible(collision.gameObject);
    }

    private void DamagePlayerIfPossible(GameObject target)
    {
        if (isDead) return;

        PlayerController pc = target.GetComponentInParent<PlayerController>();
        if (pc != null && Time.time >= lastDamageTime + damageCooldown)
        {
            lastDamageTime = Time.time;
            pc.TakeDamage(contactDamage, transform);
            MagicFX.Burst(transform.position, crystalColor, 15, 0.2f);
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        StartCoroutine(FlashDamage());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashDamage()
    {
        if (sr != null)
        {
            Color orig = sr.color;
            sr.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            sr.color = orig;
        }
    }

    private void Die()
    {
        isDead = true;
        GetComponent<Collider2D>().enabled = false;

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreValue);

        MagicFX.Burst(transform.position, crystalColor, 35, 0.5f);
        Destroy(gameObject, 0.1f);
    }
}

