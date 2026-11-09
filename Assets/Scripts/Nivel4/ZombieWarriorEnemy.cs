using System.Collections;
using UnityEngine;

/// <summary>
/// Enemigo terrestre para el Nivel 4 (ZombieWarrior).
/// Camina en patrulla, persigue a Kai si se acerca y lo daña por contacto o ataque cuerpo a cuerpo.
/// Usa los triggers del Animator de DungeonMonsters2D: MoveTrigger, AttackTrigger, StunedTrigger, DeathTrigger.
/// </summary>
public class ZombieWarriorEnemy : MonoBehaviour, IEnemigo
{
    [Header("Patrulla")]
    public Transform pointA;
    public Transform pointB;
    public float patrolDistance = 2.8f;
    public float patrolSpeed = 1.6f;

    [Header("Límites de Plataforma (Evita caer al vacío)")]
    public bool usePlatformBounds = true;
    public float minAllowedX = -100f;
    public float maxAllowedX = 200f;

    [Header("Detección y Persecución")]
    public float detectionRange = 4.8f;
    public float chaseSpeed = 2.7f;

    [Header("Ataque Cuerpo a Cuerpo y Contacto")]
    public float attackRange = 1.25f;
    public int attackDamage = 15;
    public float attackCooldown = 1.35f;
    public float contactDamageCooldown = 1.0f;

    [Header("Vida y Puntaje")]
    public int maxHealth = 60;
    public int currentHealth = 60;
    public int scoreValue = 30;
    public BarraVida barraVida;

    private Vector3 worldPointA;
    private Vector3 worldPointB;
    private bool goingToB = true;

    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D enemyCollider;

    private float baseScaleX = 0.28f;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool isHurt = false;
    private bool canAttack = true;
    private bool inMoveAnim = false;
    private float lastContactDamageTime = -10f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        enemyCollider = GetComponent<Collider2D>();

        baseScaleX = Mathf.Abs(transform.localScale.x);
        if (baseScaleX < 0.01f) baseScaleX = 0.28f;

        if (pointA != null && pointB != null)
        {
            worldPointA = pointA.position;
            worldPointB = pointB.position;
        }
        else
        {
            worldPointA = transform.position + Vector3.left * patrolDistance;
            worldPointB = transform.position + Vector3.right * patrolDistance;
        }

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
        if (isDead || isAttacking || isHurt) return;

        FindPlayerIfNeeded();

        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            float vertDist = Mathf.Abs(player.position.y - transform.position.y);

            if (dist <= detectionRange && vertDist < 2.2f)
            {
                if (dist <= attackRange)
                {
                    StopHorizontalMovement();
                    FaceDirection(player.position.x - transform.position.x);
                    if (canAttack)
                    {
                        StartCoroutine(MeleeAttackRoutine());
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

    private void FindPlayerIfNeeded()
    {
        if (player != null) return;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
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

        float dirX = Mathf.Sign(dx);
        MoveHorizontal(dirX, patrolSpeed);
    }

    private void ChasePlayer()
    {
        float dx = player.position.x - transform.position.x;
        float nextX = transform.position.x + Mathf.Sign(dx) * chaseSpeed * Time.deltaTime;

        if (usePlatformBounds && (nextX < minAllowedX || nextX > maxAllowedX))
        {
            StopHorizontalMovement();
            FaceDirection(dx);
            return;
        }

        if (Mathf.Abs(dx) > 0.1f)
        {
            MoveHorizontal(Mathf.Sign(dx), chaseSpeed);
        }
        else
        {
            StopHorizontalMovement();
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

    private void StopHorizontalMovement()
    {
        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (inMoveAnim && animator != null)
        {
            animator.Play("Idle");
            inMoveAnim = false;
        }
    }

    private void FaceDirection(float dirX)
    {
        // Los prefabs de DungeonMonsters2D miran hacia la IZQUIERDA (-X) cuando localScale.x > 0
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

    private IEnumerator MeleeAttackRoutine()
    {
        canAttack = false;
        isAttacking = true;
        inMoveAnim = false;

        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null)
        {
            animator.ResetTrigger("MoveTrigger");
            animator.SetTrigger("AttackTrigger");
        }

        yield return new WaitForSeconds(0.45f);

        if (!isDead && player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= attackRange + 0.45f)
            {
                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.TakeDamage(attackDamage, transform);
                    lastContactDamageTime = Time.time;
                }
            }
        }

        yield return new WaitForSeconds(0.45f);
        isAttacking = false;

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead || collision == null || collision.collider == null) return;
        if (!collision.collider.CompareTag("Player")) return;

        if (Time.time - lastContactDamageTime >= contactDamageCooldown)
        {
            PlayerController pc = collision.collider.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(attackDamage, transform);
                lastContactDamageTime = Time.time;
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
        else
        {
            StartCoroutine(HurtRoutine());
        }
    }

    private IEnumerator HurtRoutine()
    {
        isHurt = true;
        inMoveAnim = false;
        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null && !isAttacking)
        {
            animator.ResetTrigger("MoveTrigger");
            animator.SetTrigger("StunedTrigger");
        }

        yield return new WaitForSeconds(0.35f);
        isHurt = false;
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
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

