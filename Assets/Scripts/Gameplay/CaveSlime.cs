using System.Collections;
using UnityEngine;

/// <summary>
/// Slime de Caverna: Enemigo terrestre exclusivo del Nivel 2.
/// Se mueve horizontalmente patrullando o persiguiendo a Kai si entra en su rango.
/// Causa daño por contacto y recibe daño con espada o magia.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class CaveSlime : MonoBehaviour, IEnemigo
{
    [Header("Vida")]
    public int maxHealth = 35;
    public int currentHealth;

    [Header("Movimiento")]
    public float patrolSpeed = 1.2f;
    public float chaseSpeed = 2.4f;
    public float patrolDistance = 3f;

    [Header("Detección y Combate")]
    public float detectionRange = 4.5f;
    public int contactDamage = 15;
    public float damageCooldown = 1f;
    public LayerMask playerLayer;

    [Header("Puntaje")]
    public int scoreValue = 30;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 startPos;
    private float direction = 1f;
    private Transform player;
    private bool isDead = false;
    private float lastDamageTime = 0f;
    private Vector3 originalScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 5;
        }
        currentHealth = maxHealth;
        startPos = transform.position;
        originalScale = transform.localScale;
    }

    private void Update()
    {
        if (isDead) return;

        // Detección de Kai por distancia o por capa
        if (player == null)
        {
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) player = pc.transform;
        }

        // Efecto visual de gelatinosidad al moverse
        float squish = 1f + Mathf.Sin(Time.time * 8f) * 0.12f;
        transform.localScale = new Vector3(originalScale.x * direction * (2f - squish), originalScale.y * squish, originalScale.z);
    }

    private void FixedUpdate()
    {
        if (isDead || rb.bodyType != RigidbodyType2D.Dynamic) return;

        bool canChase = player != null && Vector2.Distance(transform.position, player.position) <= detectionRange;

        if (canChase)
        {
            // Perseguir a Kai horizontalmente
            float diffX = player.position.x - transform.position.x;
            if (Mathf.Abs(diffX) > 0.1f)
                direction = Mathf.Sign(diffX);

            rb.linearVelocity = new Vector2(direction * chaseSpeed, rb.linearVelocity.y);
        }
        else
        {
            // Patrullar de izquierda a derecha respecto al punto de inicio
            float currentDist = transform.position.x - startPos.x;
            if (currentDist > patrolDistance)
                direction = -1f;
            else if (currentDist < -patrolDistance)
                direction = 1f;

            rb.linearVelocity = new Vector2(direction * patrolSpeed, rb.linearVelocity.y);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;

        PlayerController pc = collision.gameObject.GetComponentInParent<PlayerController>();
        if (pc != null && Time.time >= lastDamageTime + damageCooldown)
        {
            lastDamageTime = Time.time;
            pc.TakeDamage(contactDamage, transform);
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
            yield return new WaitForSeconds(0.12f);
            sr.color = orig;
        }
    }

    private void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        GetComponent<Collider2D>().enabled = false;

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreValue);

        MagicFX.Burst(transform.position + Vector3.up * 0.2f, new Color(0.3f, 0.9f, 0.8f, 0.9f), 25, 0.35f);
        Destroy(gameObject, 0.15f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}

