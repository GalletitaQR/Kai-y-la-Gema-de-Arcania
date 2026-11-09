using System.Collections;
using UnityEngine;

/// <summary>
/// Enemigo arquero fijo para el Nivel 4 (SkeletonArcher).
/// Se queda en su lugar sobre plataformas o muros, mira hacia Kai y dispara flechas cada cierto tiempo.
/// Usa los triggers del Animator de DungeonMonsters2D: AttackTrigger, StunedTrigger, DeathTrigger.
/// </summary>
public class SkeletonArcherEnemy : MonoBehaviour, IEnemigo
{
    [Header("Disparo de Flechas")]
    public GameObject arrowPrefab;
    public Sprite fallbackArrowSprite;
    public Transform firePoint;
    public float attackRange = 11f;
    public float fireInterval = 2.6f;
    public float arrowSpeed = 7.2f;
    public int arrowDamage = 15;
    public float releaseDelay = 0.75f;

    [Header("Vida y Puntaje")]
    public int maxHealth = 45;
    public int currentHealth = 45;
    public int scoreValue = 35;
    public BarraVida barraVida;

    private Transform player;
    private Animator animator;
    private Rigidbody2D rb;
    private Collider2D enemyCollider;

    private float baseScaleX = 0.28f;
    private float fireTimer = 1.0f;
    private bool isDead = false;
    private bool isShooting = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        enemyCollider = GetComponent<Collider2D>();

        baseScaleX = Mathf.Abs(transform.localScale.x);
        if (baseScaleX < 0.01f) baseScaleX = 0.28f;

        currentHealth = maxHealth;
        if (barraVida != null) barraVida.Actualizar(currentHealth, maxHealth);
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

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackRange)
        {
            FaceDirection(player.position.x - transform.position.x);

            if (!isShooting)
            {
                fireTimer -= Time.deltaTime;
                if (fireTimer <= 0f)
                {
                    StartCoroutine(ShootArrowRoutine());
                }
            }
        }
    }

    private void FaceDirection(float dirX)
    {
        // En DungeonMonsters2D, localScale.x > 0 mira a la IZQUIERDA (-X)
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

    private IEnumerator ShootArrowRoutine()
    {
        isShooting = true;

        if (animator != null)
        {
            animator.SetTrigger("AttackTrigger");
        }

        yield return new WaitForSeconds(releaseDelay);

        if (!isDead && player != null)
        {
            SpawnArrowTowardsPlayer();
        }

        fireTimer = fireInterval;
        isShooting = false;
    }

    private void SpawnArrowTowardsPlayer()
    {
        Vector3 spawnPos = firePoint != null
            ? firePoint.position
            : transform.position + new Vector3(transform.localScale.x > 0 ? -0.45f : 0.45f, 0.72f, 0f);

        Vector3 targetPos = player.position + new Vector3(0f, 0.48f, 0f);
        Vector2 dir = (targetPos - spawnPos).normalized;
        if (dir.sqrMagnitude < 0.001f)
            dir = transform.localScale.x > 0 ? Vector2.left : Vector2.right;

        GameObject arrowObj;
        if (arrowPrefab != null)
        {
            arrowObj = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            arrowObj = CreateFallbackArrow(spawnPos);
        }

        EnemyArrowProjectile proj = arrowObj.GetComponent<EnemyArrowProjectile>();
        if (proj == null) proj = arrowObj.AddComponent<EnemyArrowProjectile>();
        proj.Initialize(dir, arrowSpeed, arrowDamage);
    }

    private GameObject CreateFallbackArrow(Vector3 spawnPos)
    {
        GameObject go = new GameObject("FlechaEnemiga_Runtime");
        go.transform.position = spawnPos;
        go.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = fallbackArrowSprite;
        sr.sortingOrder = 15;

        Rigidbody2D arrowRb = go.AddComponent<Rigidbody2D>();
        arrowRb.bodyType = RigidbodyType2D.Kinematic;
        arrowRb.gravityScale = 0f;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.1f, 0.22f);

        return go;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (barraVida != null) barraVida.Actualizar(currentHealth, maxHealth);
        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.enemyHit : null);

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (animator != null && !isShooting)
        {
            animator.SetTrigger("StunedTrigger");
        }
    }

    private void Die()
    {
        isDead = true;
        StopAllCoroutines();

        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.enemyDeath : null);
        if (GameManager.Instance != null) GameManager.Instance.AddScore(scoreValue);

        if (enemyCollider != null) enemyCollider.enabled = false;
        if (barraVida != null) barraVida.gameObject.SetActive(false);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        if (animator != null)
        {
            animator.ResetTrigger("AttackTrigger");
            animator.ResetTrigger("StunedTrigger");
            animator.SetTrigger("DeathTrigger");
        }

        Destroy(gameObject, 1.15f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
