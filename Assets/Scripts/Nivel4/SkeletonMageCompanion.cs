using System.Collections;
using UnityEngine;

/// <summary>
/// Mage Esqueleto acompañante del Dragón Guardián en la Sala del Trono (Nivel 4).
/// Lanza proyectiles/hechizos a distancia y se reposiciona entre los extremos del salón.
/// Kai puede derrotarlo o esquivarlo; al derrotarlo, el Dragón pierde su apoyo arcano.
/// </summary>
public class SkeletonMageCompanion : MonoBehaviour, IEnemigo
{
    [Header("Proyectiles / Hechizos")]
    public GameObject spellPrefab;
    public Sprite fallbackSpellSprite;
    public Transform castPoint;
    public float attackRange = 16f;
    public float castInterval = 3.1f;
    public float spellSpeed = 6.8f;
    public int spellDamage = 15;
    public Color spellColor = new Color(0.72f, 0.28f, 1f, 0.95f);

    [Header("Reposicionamiento en la Arena")]
    public bool teleportBetweenPerches = true;
    public Vector3 perchRight = new Vector3(131.5f, 0.85f, 0f);
    public Vector3 perchLeft = new Vector3(119.5f, 0.85f, 0f);
    public int castsBeforeTeleport = 2;

    [Header("Vida y Puntaje")]
    public int maxHealth = 120;
    public int currentHealth = 120;
    public int scoreValue = 80;
    public BarraVida barraVida;

    private Transform player;
    private Animator animator;
    private Rigidbody2D rb;
    private Collider2D mageCollider;

    private float baseScaleX = 0.28f;
    private float castTimer = 1.6f;
    private int castCount = 0;
    private bool onRightPerch = true;
    private bool isCasting = false;
    private bool isDead = false;
    private bool combatActive = false;

    public bool IsDead => isDead;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        mageCollider = GetComponent<Collider2D>();

        baseScaleX = Mathf.Abs(transform.localScale.x);
        if (baseScaleX < 0.01f) baseScaleX = 0.28f;

        currentHealth = maxHealth;
        if (barraVida != null) barraVida.Actualizar(currentHealth, maxHealth);
    }

    public void ActivateCombat()
    {
        combatActive = true;
    }

    void Update()
    {
        if (isDead) return;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            if (player == null) return;
        }

        if (!combatActive)
        {
            if (player.position.x >= 114.2f)
            {
                combatActive = true;
            }
            else
            {
                return;
            }
        }

        FaceDirection(player.position.x - transform.position.x);

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackRange && !isCasting)
        {
            castTimer -= Time.deltaTime;
            if (castTimer <= 0f)
            {
                StartCoroutine(CastSpellRoutine());
            }
        }
    }

    private IEnumerator CastSpellRoutine()
    {
        isCasting = true;

        if (animator != null)
        {
            animator.SetTrigger(castCount % 2 == 0 ? "AttackTrigger" : "SpecialATrigger");
        }

        Vector3 spawnPos = castPoint != null
            ? castPoint.position
            : transform.position + new Vector3(transform.localScale.x > 0 ? -0.55f : 0.55f, 0.85f, 0f);

        MagicFX.Burst(spawnPos, spellColor, 10, 0.25f);

        yield return new WaitForSeconds(0.65f);

        if (!isDead && player != null)
        {
            SpawnSpell(spawnPos);
        }

        castCount++;
        if (teleportBetweenPerches && castCount >= castsBeforeTeleport && !isDead)
        {
            castCount = 0;
            yield return new WaitForSeconds(0.55f);
            if (!isDead)
            {
                MagicFX.Burst(transform.position + Vector3.up * 0.6f, spellColor, 16, 0.35f);
                onRightPerch = !onRightPerch;
                transform.position = onRightPerch ? perchRight : perchLeft;
                MagicFX.Burst(transform.position + Vector3.up * 0.6f, spellColor, 16, 0.35f);
            }
        }

        castTimer = castInterval;
        isCasting = false;
    }

    private void SpawnSpell(Vector3 spawnPos)
    {
        Vector3 targetPos = player.position + new Vector3(0f, 0.45f, 0f);
        Vector2 dir = (targetPos - spawnPos).normalized;
        if (dir.sqrMagnitude < 0.001f) dir = Vector2.left;

        GameObject spellObj;
        if (spellPrefab != null)
        {
            spellObj = Instantiate(spellPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            spellObj = CreateFallbackSpell(spawnPos);
        }

        BossProjectile proj = spellObj.GetComponent<BossProjectile>();
        if (proj == null) proj = spellObj.AddComponent<BossProjectile>();
        proj.Initialize(dir, spellSpeed, spellDamage, spellColor);
    }

    private GameObject CreateFallbackSpell(Vector3 spawnPos)
    {
        GameObject go = new GameObject("HechizoMage_Runtime");
        go.transform.position = spawnPos;
        go.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = fallbackSpellSprite;
        sr.color = spellColor;
        sr.sortingOrder = 16;

        Rigidbody2D projRb = go.AddComponent<Rigidbody2D>();
        projRb.bodyType = RigidbodyType2D.Kinematic;
        projRb.gravityScale = 0f;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.28f;

        return go;
    }

    private void FaceDirection(float dirX)
    {
        if (dirX < -0.02f)
        {
            Vector3 s = transform.localScale;
            s.x = baseScaleX;
            transform.localScale = s;
        }
        else if (dirX > 0.02f)
        {
            Vector3 s = transform.localScale;
            s.x = -baseScaleX;
            transform.localScale = s;
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        combatActive = true;

        currentHealth -= amount;
        if (barraVida != null) barraVida.Actualizar(currentHealth, maxHealth);
        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.enemyHit : null);
        MagicFX.Burst(transform.position + Vector3.up * 0.6f, spellColor, 8, 0.22f);

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (animator != null && !isCasting)
        {
            animator.SetTrigger("StunedTrigger");
        }
    }

    public void ForceDefeatByBossDeath()
    {
        if (!isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        StopAllCoroutines();

        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.enemyDeath : null);
        if (GameManager.Instance != null) GameManager.Instance.AddScore(scoreValue);

        if (mageCollider != null) mageCollider.enabled = false;
        if (barraVida != null) barraVida.gameObject.SetActive(false);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        if (animator != null)
        {
            animator.ResetTrigger("AttackTrigger");
            animator.ResetTrigger("SpecialATrigger");
            animator.ResetTrigger("StunedTrigger");
            animator.SetTrigger("DeathTrigger");
        }

        Destroy(gameObject, 1.25f);
    }
}

