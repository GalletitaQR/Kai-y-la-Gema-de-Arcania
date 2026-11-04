using UnityEngine;
using System.Collections;

public class CrabAnimations : MonoBehaviour, IEnemigo
{
    [Header("Barra de vida")]
    public BarraVida barraVida;

    [Header("Patrulla (Hijos con posiciones fijas al iniciar)")]
    public Transform pointA;
    public Transform pointB;
    public float patrolSpeed = 1.5f;

    [Header("Detección y persecución")]
    public float detectionRange = 4f;
    public float chaseSpeed = 3f;
    public LayerMask playerLayer;

    [Header("Ataque")]
    public float attackRange = 0.8f;
    public int attackDamage = 15;
    public float attackCooldown = 1.2f;

    [Header("Vida")]
    public int maxHealth = 50;
    public int currentHealth;

    [Header("Puntaje")]
    public int scoreValue = 25;

    // Posiciones mundiales fijas para que los hijos no se muevan con el padre
    private Vector3 worldPointA;
    private Vector3 worldPointB;
    private bool goingToB = true;

    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool canAttack = true;

    private enum CrabState { PATROL, CHASE, ATTACK_STOP }
    private CrabState currentState = CrabState.PATROL;
    private float desiredDirectionX = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // IMPORTANTE: Guardamos la posición absoluta en el mundo de los puntos hijos
        // para que, aunque sean hijos, el cangrejo patrulle hacia coordenadas fijas del mapa.
        if (pointA != null) worldPointA = pointA.position;
        else worldPointA = transform.position + Vector3.left * 2f;

        if (pointB != null) worldPointB = pointB.position;
        else worldPointB = transform.position + Vector3.right * 2f;

        // Opcional: una vez guardadas las posiciones del mundo, podemos desconectarlos 
        // de la jerarquía para evitar que se muevan con el transform si lo prefieres,
        // o dejarlos así con las posiciones mundiales ya fijadas.

        currentHealth = maxHealth;
        if (barraVida != null) barraVida.Actualizar(currentHealth, maxHealth);
    }

    void Update()
    {
        if (isDead || isAttacking) return;

        DetectPlayer();

        if (player != null)
        {
            DecideChaseOrAttack();
        }
        else
        {
            DecidePatrol();
        }
    }

    void FixedUpdate()
    {
        if (isDead || isAttacking) return;

        switch (currentState)
        {
            case CrabState.PATROL:
            case CrabState.CHASE:
                float speed = currentState == CrabState.CHASE ? chaseSpeed : patrolSpeed;
                rb.linearVelocity = new Vector2(desiredDirectionX * speed, rb.linearVelocity.y);
                break;

            case CrabState.ATTACK_STOP:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;
        }
    }

    void DetectPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        player = hit != null ? hit.transform : null;
    }

    void DecidePatrol()
    {
        // Usamos las posiciones fijas del mundo que guardamos en Awake
        Vector3 targetPos = goingToB ? worldPointB : worldPointA;
        Vector2 direction = (targetPos - transform.position);
        
        if (Mathf.Abs(direction.x) > 0.05f)
        {
            desiredDirectionX = Mathf.Sign(direction.x);
            FlipTowards(desiredDirectionX);
        }

        currentState = CrabState.PATROL;
        animator.SetBool("IsMoving", true);

        // Si llegó al objetivo, cambiamos de dirección
        if (Vector2.Distance(transform.position, targetPos) < 0.3f)
        {
            goingToB = !goingToB;
        }
    }

    void DecideChaseOrAttack()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > attackRange)
        {
            Vector2 direction = (player.position - transform.position);
            desiredDirectionX = Mathf.Sign(direction.x);
            currentState = CrabState.CHASE;

            FlipTowards(direction.x);
            animator.SetBool("IsMoving", true);
        }
        else
        {
            currentState = CrabState.ATTACK_STOP;
            animator.SetBool("IsMoving", false);

            if (canAttack)
            {
                StartCoroutine(Attack());
            }
        }
    }

    IEnumerator Attack()
    {
        canAttack = false;
        isAttacking = true;
        currentState = CrabState.ATTACK_STOP;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        animator.SetBool("IsMoving", false);
        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(0.3f);

        if (player != null && Vector2.Distance(transform.position, player.position) <= attackRange + 0.2f)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.TakeDamage(attackDamage, transform);
            }
        }

        isAttacking = false;

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    void FlipTowards(float directionX)
    {
        if (spriteRenderer != null)
        {
            if (directionX > 0)
                spriteRenderer.flipX = false;
            else if (directionX < 0)
                spriteRenderer.flipX = true;
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (barraVida != null) barraVida.Actualizar(currentHealth, maxHealth);
        animator.SetTrigger("Hit");
        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.enemyHit : null);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.enemyDeath : null);
        if (GameManager.Instance != null) GameManager.Instance.AddScore(scoreValue);
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;
        animator.SetTrigger("Death");
        Destroy(gameObject, 2f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}