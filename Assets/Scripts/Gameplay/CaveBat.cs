using System.Collections;
using UnityEngine;

/// <summary>
/// Murciélago de Caverna: Enemigo volador que usa 'bat_spritesheet(80x80)'.
/// Vuela por la cueva, acecha a Kai desde el aire, desciende en picada para morderlo,
/// y puede ser destruido en el aire con espada (salto + X) o magia (bastón + X).
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class CaveBat : MonoBehaviour, IEnemigo
{
    [Header("Vida")]
    public int maxHealth = 30;
    public int currentHealth;

    [Header("Barra de Vida Flotante")]
    public BarraVida barraVida;

    [Header("Vuelo y Patrulla")]
    public float patrolSpeed = 1.8f;
    public float patrolRadius = 2.8f;
    public float bobFrequency = 3.5f;
    public float bobHeight = 0.35f;

    [Header("Detección y Ataque")]
    public float detectionRange = 5.5f;
    public float chaseSpeed = 3.2f;
    public float attackRange = 1.1f;
    public int attackDamage = 12;
    public float attackCooldown = 1.2f;
    public LayerMask playerLayer;

    [Header("Sprites de Animación")]
    public Sprite[] flySprites;
    public float animationFps = 10f;

    [Header("Puntaje")]
    public int scoreValue = 25;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D miCollider;
    private Vector2 startPos;
    private Transform player;
    private bool isDead = false;
    private float lastDamageTime = 0f;
    private float animTimer = 0f;
    private int animIndex = 0;
    private float patrolDir = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        miCollider = GetComponent<Collider2D>();

        if (sr != null)
        {
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 6;
        }

        currentHealth = maxHealth;
        startPos = transform.position;

        if (barraVida != null)
            barraVida.Actualizar(currentHealth, maxHealth);

        if (playerLayer.value == 0)
            playerLayer = 1 << 7;
    }

    private void Start()
    {
        startPos = transform.position;
        BuscarJugador();
    }

    private void Update()
    {
        if (isDead) return;

        if (player == null)
            BuscarJugador();

        // 1. Animación continua de aleteo desde el spritesheet 80x80
        if (sr != null && flySprites != null && flySprites.Length > 0)
        {
            animTimer += Time.deltaTime;
            if (animTimer >= (1f / animationFps))
            {
                animTimer = 0f;
                animIndex = (animIndex + 1) % flySprites.Length;
                sr.sprite = flySprites[animIndex];
            }
        }

        // 2. Ataque directo a Kai si está en rango
        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= attackRange && Time.time >= lastDamageTime + attackCooldown)
            {
                Atacar();
            }
        }
    }

    private void FixedUpdate()
    {
        if (isDead || rb == null) return;

        bool canChase = player != null && Vector2.Distance(transform.position, player.position) <= detectionRange;

        if (canChase)
        {
            // Vuela y desciende hacia Kai
            Vector2 targetPos = (Vector2)player.position + Vector2.up * 0.4f;
            Vector2 dir = (targetPos - (Vector2)transform.position).normalized;

            rb.linearVelocity = dir * chaseSpeed;

            if (sr != null && Mathf.Abs(dir.x) > 0.05f)
                sr.flipX = dir.x < 0f;
        }
        else
        {
            // Patrulla aérea horizontal con oscilación senoidal
            float currentDist = transform.position.x - startPos.x;
            if (currentDist > patrolRadius)
                patrolDir = -1f;
            else if (currentDist < -patrolRadius)
                patrolDir = 1f;

            float vy = Mathf.Sin(Time.time * bobFrequency) * bobHeight * 3f;
            rb.linearVelocity = new Vector2(patrolDir * patrolSpeed, vy);

            if (sr != null)
                sr.flipX = patrolDir < 0f;
        }
    }

    private void BuscarJugador()
    {
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) player = pc.transform;
    }

    private void Atacar()
    {
        if (player == null || isDead) return;

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc == null) pc = player.GetComponentInParent<PlayerController>();

        if (pc != null)
        {
            lastDamageTime = Time.time;
            pc.TakeDamage(attackDamage, transform);
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        TryDamage(collider.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamage(collision.gameObject);
    }

    private void TryDamage(GameObject target)
    {
        if (isDead) return;

        PlayerController pc = target.GetComponentInParent<PlayerController>();
        if (pc == null) pc = target.GetComponent<PlayerController>();

        if (pc != null && Time.time >= lastDamageTime + attackCooldown)
        {
            lastDamageTime = Time.time;
            pc.TakeDamage(attackDamage, transform);
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (barraVida != null) barraVida.Actualizar(currentHealth, maxHealth);

        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.enemyHit : null);
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
            sr.color = Color.red;
            yield return new WaitForSeconds(0.12f);
            sr.color = orig;
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (miCollider != null) miCollider.enabled = false;
        if (barraVida != null) barraVida.gameObject.SetActive(false);

        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.enemyDeath : null);

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreValue);

        MagicFX.Burst(transform.position, new Color(0.7f, 0.3f, 0.9f, 1f), 20, 0.35f);

        // Cae al ser derrotado
        if (rb != null)
        {
            rb.gravityScale = 2.5f;
            rb.linearVelocity = new Vector2(0f, -2f);
        }

        Destroy(gameObject, 0.4f);
    }
}

