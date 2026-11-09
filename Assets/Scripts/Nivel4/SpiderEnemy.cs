using UnityEngine;

/// <summary>
/// Enemigo Araña (Spider) para el Nivel 4.
/// Patrulla rápidamente por el suelo, persigue a Kai al detectarlo y le va quitando vida
/// de forma continua (mordidas rápidas cada cierto intervalo) mientras esté en contacto o alcance.
/// </summary>
public class SpiderEnemy : MonoBehaviour, IEnemigo
{
    [Header("Patrulla y Persecución Rápida")]
    public float patrolDistance = 2.5f;
    public float patrolSpeed = 2.2f;
    public float detectionRange = 4.8f;
    public float chaseSpeed = 3.4f;

    [Header("Límites de Plataforma")]
    public bool usePlatformBounds = true;
    public float minAllowedX = -100f;
    public float maxAllowedX = 200f;

    [Header("Mordida Continua (Va quitando vida)")]
    public float biteRange = 1.05f;
    public int biteDamage = 8;
    public float biteInterval = 0.75f;

    [Header("Vida y Puntaje")]
    public int maxHealth = 35;
    public int currentHealth = 35;
    public int scoreValue = 25;
    public BarraVida barraVida;

    private Vector3 worldPointA;
    private Vector3 worldPointB;
    private bool goingToB = true;

    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D enemyCollider;

    private float baseScaleX = 0.25f;
    private float nextBiteTime = 0f;
    private bool isDead = false;
    private bool inMoveAnim = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        enemyCollider = GetComponent<Collider2D>();

        baseScaleX = Mathf.Abs(transform.localScale.x);
        if (baseScaleX < 0.01f) baseScaleX = 0.25f;

        worldPointA = transform.position + Vector3.left * patrolDistance;
        worldPointB = transform.position + Vector3.right * patrolDistance;

        if (usePlatformBounds)
        {
            worldPointA.x = Mathf.Clamp(worldPointA.x, minAllowedX, maxAllowedX);
            worldPointB.x = Mathf.Clamp(worldPointB.x, minAllowedX, maxAllowedX);
        }

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
        }

        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            float vertDist = Mathf.Abs(player.position.y - transform.position.y);

            if (dist <= detectionRange && vertDist < 1.8f)
            {
                if (dist <= biteRange)
                {
                    if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    inMoveAnim = false;
                    FaceDirection(player.position.x - transform.position.x);

                    // Va quitando vida periódicamente mientras Kai siga cerca
                    if (Time.time >= nextBiteTime)
                    {
                        nextBiteTime = Time.time + biteInterval;
                        if (animator != null) animator.SetTrigger("AttackTrigger");

                        PlayerController pc = player.GetComponent<PlayerController>();
                        if (pc != null)
                        {
                            pc.TakeDamage(biteDamage, transform);
                        }
                    }
                }
                else
                {
                    ChasePlayer();
                }
                return;
            }
        }

        Patrol();
    }

    private void Patrol()
    {
        Vector3 target = goingToB ? worldPointB : worldPointA;
        float dx = target.x - transform.position.x;

        if (Mathf.Abs(dx) <= 0.2f)
        {
            goingToB = !goingToB;
            target = goingToB ? worldPointB : worldPointA;
            dx = target.x - transform.position.x;
        }

        MoveHorizontal(Mathf.Sign(dx), patrolSpeed);
    }

    private void ChasePlayer()
    {
        float dx = player.position.x - transform.position.x;
        float nextX = transform.position.x + Mathf.Sign(dx) * chaseSpeed * Time.deltaTime;

        if (usePlatformBounds && (nextX < minAllowedX || nextX > maxAllowedX))
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            FaceDirection(dx);
            return;
        }

        if (Mathf.Abs(dx) > 0.08f)
        {
            MoveHorizontal(Mathf.Sign(dx), chaseSpeed);
        }
    }

    private void MoveHorizontal(float dirX, float speed)
    {
        FaceDirection(dirX);

        if (rb != null)
            rb.linearVelocity = new Vector2(dirX * speed, rb.linearVelocity.y);
        else
            transform.position += new Vector3(dirX * speed * Time.deltaTime, 0f, 0f);

        if (!inMoveAnim && animator != null)
        {
            animator.SetTrigger("MoveTrigger");
            inMoveAnim = true;
        }
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

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead || collision == null || collision.collider == null) return;
        if (!collision.collider.CompareTag("Player")) return;

        if (Time.time >= nextBiteTime)
        {
            nextBiteTime = Time.time + biteInterval;
            if (animator != null) animator.SetTrigger("AttackTrigger");

            PlayerController pc = collision.collider.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(biteDamage, transform);
            }
        }
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
        else if (animator != null)
        {
            inMoveAnim = false;
            animator.ResetTrigger("MoveTrigger");
            animator.SetTrigger("StunedTrigger");
        }
    }

    private void Die()
    {
        isDead = true;

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
            animator.ResetTrigger("MoveTrigger");
            animator.ResetTrigger("AttackTrigger");
            animator.ResetTrigger("StunedTrigger");
            animator.SetTrigger("DeathTrigger");
        }

        Destroy(gameObject, 1.15f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, biteRange);
    }
}
