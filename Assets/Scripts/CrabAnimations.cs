using UnityEngine;
using System.Collections;

public class CrabAnimations : MonoBehaviour
{
    [Header("Patrulla")]
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

    private Vector3 patrolPointA;
    private Vector3 patrolPointB;
    private bool goingToB = true;
    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool canAttack = true;

    // Guardamos la dirección/decisión calculada en Update
    // para aplicarla como física en FixedUpdate.
    private enum CrabState { PATROL, CHASE, ATTACK_STOP }
    private CrabState physicsState = CrabState.PATROL;
    private float desiredDirectionX = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Capturamos la posición MUNDIAL de los puntos una sola vez,
        // antes de que cualquier rotación (flip) los desplace.
        // Así, aunque PointA/PointB sean hijos del cangrejo, su
        // objetivo de patrulla queda fijo para siempre.
        if (pointA != null) patrolPointA = pointA.position;
        if (pointB != null) patrolPointB = pointB.position;

        currentHealth = maxHealth;
    }

    void Update()
    {
        if (isDead) return;

        // Durante el ataque no recalculamos movimiento,
        // la corrutina Attack() ya controla el estado físico.
        if (isAttacking) return;

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
        if (isDead) return;

        // Si está atacando, la corrutona ya fijó la velocidad en 0,
        // no la pisamos aquí.
        if (isAttacking) return;

        switch (physicsState)
        {
            case CrabState.PATROL:
            case CrabState.CHASE:
                float speed = physicsState == CrabState.CHASE ? chaseSpeed : patrolSpeed;
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
        if (pointA == null || pointB == null)
        {
            physicsState = CrabState.ATTACK_STOP;
            return;
        }

        Vector3 targetPos = goingToB ? patrolPointB : patrolPointA;
        Vector2 direction = (targetPos - transform.position);
        desiredDirectionX = Mathf.Sign(direction.x);
        physicsState = CrabState.PATROL;

        FlipTowards(direction.x);
        animator.SetBool("IsMoving", true);

        if (Vector2.Distance(transform.position, targetPos) < 0.2f)
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
            physicsState = CrabState.CHASE;

            FlipTowards(direction.x);
            animator.SetBool("IsMoving", true);
        }
        else
        {
            physicsState = CrabState.ATTACK_STOP;
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
        physicsState = CrabState.ATTACK_STOP;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Frena al atacar
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
        if (directionX > 0)
            transform.localRotation = Quaternion.Euler(0, 0, 0);
        else if (directionX < 0)
            transform.localRotation = Quaternion.Euler(0, 180, 0);
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"[DEBUG] {gameObject.name} recibió {amount} de daño. Vida restante: {currentHealth}/{maxHealth}");
        animator.SetTrigger("Hit");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"[DEBUG] {gameObject.name} murió.");
        isDead = true;
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