using System.Collections;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Serpiente de Caverna: Enemigo terrestre con spritesheet completo (Assets/Sprites/snake_spritesheet(64x64).png).
/// Cuenta con animación de acecho/espera (Idle), reptado (Slither), mordisco frontal con colmillos abiertos (Attack),
/// retroceso al recibir daño (Hurt) y colapso al morir (Death).
/// Se orienta hacia el jugador, descansa firmemente sobre el suelo sin flotar y tiene tamaño amenazante.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class CaveSnake : MonoBehaviour, IEnemigo
{
    public enum State { Idle, Slither, Attack, Hurt, Dead }

    [Header("Vida")]
    public int maxHealth = 30;
    public int currentHealth;

    [Header("Movimiento")]
    public float patrolSpeed = 1.2f;
    public float chaseSpeed = 2.4f;
    public float patrolDistance = 2.5f;

    [Header("Combate")]
    public int contactDamage = 15;
    public float detectionRange = 4.5f;
    public float attackRange = 1.3f;
    public float attackCooldown = 1.2f;

    [Header("Puntaje")]
    public int scoreValue = 25;

    [Header("Animaciones por Spritesheet (64x64)")]
    public Sprite[] idleSprites;    // Fila 0: Erguida observando
    public Sprite[] slitherSprites; // Fila 4: Reptando en el suelo
    public Sprite[] attackSprites;  // Fila 1: Lanzamiento frontal y mordisco
    public Sprite[] hurtSprites;    // Fila 2: Sacudida al recibir daño
    public Sprite[] deathSprites;   // Fila 5: Colapso y caída

    [Header("Velocidad de Animación")]
    public float animFps = 9f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;
    private Vector2 startPos;
    private float direction = -1f; // Sprite original mira a la izquierda
    private Transform player;
    private State currentState = State.Idle;
    private float stateTimer = 0f;
    private float lastAttackTime = -10f;
    private int animIndex = 0;
    private float animTimer = 0f;
    private bool isDead = false;

    private void Awake()
    {
        gameObject.tag = "Enemigo";
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        if (sr != null)
        {
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 5;
        }

        // Asegurar que el collider descanse exactamente sobre el suelo sin flotar:
        // Con pivot en {0.5, 0}, la base del sprite es Y = 0.
        // Un BoxCollider2D con tamaño Y = 0.22 y offset Y = 0.11 tiene su base exactamente en Y = 0.
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size = new Vector2(0.42f, 0.22f);
            box.offset = new Vector2(0f, 0.11f);
        }

        if (rb != null)
        {
            rb.gravityScale = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // Tamaño más imponente y amenazante como pidió el usuario
        if (transform.localScale.x < 2f)
        {
            transform.localScale = new Vector3(3.0f, 3.0f, 1f);
        }

        currentHealth = maxHealth;
        startPos = transform.position;

        // Auto-cargar sprites desde el spritesheet si no están configurados en el inspector
        EnsureSpritesLoaded();
    }

    private void Start()
    {
        startPos = transform.position;
        BuscarJugador();
    }

    public void EnsureSpritesLoaded()
    {
#if UNITY_EDITOR
        if (idleSprites == null || idleSprites.Length == 0 ||
            attackSprites == null || attackSprites.Length == 0 ||
            slitherSprites == null || slitherSprites.Length == 0)
        {
            Sprite[] all = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/snake_spritesheet(64x64).png")
                .OfType<Sprite>()
                .OrderBy(s => ExtractSpriteIndex(s.name))
                .ToArray();

            if (all.Length >= 36)
            {
                // Fila 0: 0..5 (Idle erguida)
                idleSprites = all.Take(6).ToArray();
                // Fila 1: 6..12 (Ataque con mordisco). Omitimos el frame corrupto 8 si existe
                attackSprites = new Sprite[] { all[6], all[7], all[9], all[10], all[11], all[12] };
                // Fila 2: 13..18 (Daño recibido)
                hurtSprites = all.Skip(13).Take(6).ToArray();
                // Fila 4: 24..29 (Reptado sobre el piso)
                slitherSprites = all.Skip(24).Take(6).ToArray();
                // Fila 5: 30..35 (Muerte)
                deathSprites = all.Skip(30).Take(6).ToArray();
            }
        }
#endif
    }

    private int ExtractSpriteIndex(string name)
    {
        int lastUnderscore = name.LastIndexOf('_');
        if (lastUnderscore >= 0 && int.TryParse(name.Substring(lastUnderscore + 1), out int idx))
            return idx;
        return 0;
    }

    private void Update()
    {
        if (isDead)
        {
            AnimateCurrentState();
            return;
        }

        if (player == null)
        {
            BuscarJugador();
        }

        float distToPlayer = player != null ? Vector2.Distance(transform.position, player.position) : 999f;

        // Orientación de la serpiente: mira hacia donde camina o hacia Kai
        if (sr != null && currentState != State.Attack && currentState != State.Hurt)
        {
            // Sprite por defecto mira a la izquierda (flipX = false). Si va a la derecha, flipX = true.
            sr.flipX = direction > 0f;
        }

        // Lógica de estados
        switch (currentState)
        {
            case State.Idle:
            case State.Slither:
                // Si el jugador está dentro de attackRange y ya pasó el cooldown, atacar
                if (distToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
                {
                    StartAttack();
                }
                break;

            case State.Attack:
                // El estado Attack se maneja por su animación
                break;

            case State.Hurt:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    SetState(State.Slither);
                }
                break;
        }

        AnimateCurrentState();
    }

    private void FixedUpdate()
    {
        if (isDead || rb == null || rb.bodyType != RigidbodyType2D.Dynamic) return;

        if (currentState == State.Attack || currentState == State.Hurt)
        {
            // Durante ataque o dolor, detiene su avance horizontal pero mantiene gravedad
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        bool canChase = player != null && Vector2.Distance(transform.position, player.position) <= detectionRange;

        if (canChase)
        {
            float diffX = player.position.x - transform.position.x;
            if (Mathf.Abs(diffX) > 0.15f)
                direction = Mathf.Sign(diffX);

            SetState(State.Slither);
            rb.linearVelocity = new Vector2(direction * chaseSpeed, rb.linearVelocity.y);
        }
        else
        {
            float currentDist = transform.position.x - startPos.x;
            if (currentDist > patrolDistance)
                direction = -1f;
            else if (currentDist < -patrolDistance)
                direction = 1f;

            SetState(State.Slither);
            rb.linearVelocity = new Vector2(direction * patrolSpeed, rb.linearVelocity.y);
        }
    }

    private void SetState(State newState)
    {
        if (currentState == newState || isDead) return;
        currentState = newState;
        animIndex = 0;
        animTimer = 0f;
    }

    private void StartAttack()
    {
        SetState(State.Attack);
        lastAttackTime = Time.time;

        // Asegurarse de mirar a Kai al atacar
        if (player != null)
        {
            direction = Mathf.Sign(player.position.x - transform.position.x);
            if (sr != null) sr.flipX = direction > 0f;
        }
    }

    private void AnimateCurrentState()
    {
        if (sr == null) return;

        Sprite[] currentSprites = null;
        float currentFps = animFps;

        switch (currentState)
        {
            case State.Idle:
                currentSprites = idleSprites;
                break;
            case State.Slither:
                currentSprites = slitherSprites != null && slitherSprites.Length > 0 ? slitherSprites : idleSprites;
                currentFps = animFps * 1.1f;
                break;
            case State.Attack:
                currentSprites = attackSprites;
                currentFps = animFps * 1.3f;
                break;
            case State.Hurt:
                currentSprites = hurtSprites;
                break;
            case State.Dead:
                currentSprites = deathSprites;
                currentFps = animFps * 0.9f;
                break;
        }

        if (currentSprites == null || currentSprites.Length == 0) return;

        animTimer += Time.deltaTime;
        if (animTimer >= (1f / currentFps))
        {
            animTimer = 0f;
            animIndex++;

            if (currentState == State.Attack)
            {
                // En el frame álgido de la embestida (abrir mandíbula y morder), aplicar daño
                if (animIndex == 2 || animIndex == 3)
                {
                    TryBitePlayer();
                }

                if (animIndex >= currentSprites.Length)
                {
                    // Fin del ataque, regresar a Idle/Slither
                    SetState(State.Idle);
                    return;
                }
            }
            else if (currentState == State.Dead)
            {
                if (animIndex >= currentSprites.Length)
                {
                    animIndex = currentSprites.Length - 1; // Quedar en el suelo
                    return;
                }
            }
            else
            {
                animIndex %= currentSprites.Length;
            }

            sr.sprite = currentSprites[animIndex];
        }
    }

    private void TryBitePlayer()
    {
        if (player == null || isDead) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackRange + 0.35f)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc == null) pc = player.GetComponentInParent<PlayerController>();

            if (pc != null)
            {
                pc.TakeDamage(contactDamage, transform);
                MagicFX.Burst(transform.position + Vector3.up * 0.2f, new Color(0.85f, 0.1f, 0.1f, 0.9f), 10, 0.25f);
            }
        }
    }

    private void BuscarJugador()
    {
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) player = pc.transform;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryContactDamage(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryContactDamage(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        TryContactDamage(collider.gameObject);
    }

    private void TryContactDamage(GameObject target)
    {
        if (isDead) return;

        PlayerController pc = target.GetComponentInParent<PlayerController>();
        if (pc == null) pc = target.GetComponent<PlayerController>();

        if (pc != null && Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            pc.TakeDamage(contactDamage, transform);
            MagicFX.Burst(transform.position + Vector3.up * 0.2f, new Color(0.85f, 0.1f, 0.1f, 0.9f), 10, 0.25f);
            StartAttack();
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.enemyHit : null);
        StartCoroutine(FlashDamage());

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            SetState(State.Hurt);
            stateTimer = 0.25f;
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
        SetState(State.Dead);

        if (col != null) col.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.enemyDeath : null);

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreValue);

        MagicFX.Burst(transform.position, new Color(0.2f, 0.8f, 0.3f, 1f), 20, 0.4f);
        Destroy(gameObject, 0.9f);
    }
}
