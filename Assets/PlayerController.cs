using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float moveSpeed = 4f;
    public float jumpForce = 8f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("Vida")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Ataque")]
    public float attackDuration = 0.4f;

    [Header("Ataque - Detección")]
    public Transform attackPoint;
    public float attackRange = 0.6f;
    public int attackDamage = 50; // Igual o mayor a maxHealth del cangrejo (50) para matarlo de un golpe
    public LayerMask enemyLayer;

    [Header("Retroceso por Golpe (Knockback)")]
    public Vector2 knockbackForce = new Vector2(3f, 4f); // Fuerza hacia atrás y arriba
    public float knockbackDuration = 0.25f;

    private Rigidbody2D rb;
    private SPUM_Prefabs spumPrefabs;
    private PlayerState currentState = PlayerState.IDLE;
    private bool isGrounded;
    private bool isAttacking = false;
    private bool isKnockback = false;
    private bool isInvincible = false;
    private bool isDead = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spumPrefabs = GetComponent<SPUM_Prefabs>();
        spumPrefabs.OverrideControllerInit();
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (isDead) return;

        float moveX = Input.GetAxisRaw("Horizontal");
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Salto
        if (Input.GetButtonDown("Jump") && isGrounded && !isAttacking && !isKnockback)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Ataque
        if (Input.GetKeyDown(KeyCode.X) && !isAttacking && !isKnockback)
        {
            StartCoroutine(Attack());
        }

        // Flip (rotación)
        if (!isAttacking && !isKnockback)
        {
            if (moveX < 0)
            {
                transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
            else if (moveX > 0)
            {
                transform.localRotation = Quaternion.Euler(0, 180, 0);
            }
        }

        // Animación de movimiento / reposo
        if (!isAttacking && !isKnockback)
        {
            PlayerState newState = Mathf.Abs(moveX) > 0.01f ? PlayerState.MOVE : PlayerState.IDLE;
            if (newState != currentState)
            {
                currentState = newState;
                spumPrefabs.PlayAnimation(currentState, 0);
            }
        }
    }

    void FixedUpdate()
    {
        // Seguro extra: nunca tocar linearVelocity si el cuerpo ya no es Dynamic
        if (isDead || isKnockback || rb.bodyType != RigidbodyType2D.Dynamic) return;

        float moveX = isAttacking ? 0f : Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);
    }

    IEnumerator Attack()
    {
        if (attackPoint == null)
        {
            Debug.LogWarning("PlayerController: 'Attack Point' no está asignado en el Inspector. Cancelando ataque.");
            yield break; // No dejamos isAttacking en true si no podemos atacar
        }

        isAttacking = true;
        currentState = PlayerState.ATTACK;
        spumPrefabs.PlayAnimation(currentState, 0);

        yield return new WaitForSeconds(0.2f);

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        Debug.Log($"[DEBUG] Ataque conectó con {hitEnemies.Length} colliders en la capa Enemy.");
        foreach (Collider2D enemy in hitEnemies)
        {
            CrabAnimations crab = enemy.GetComponent<CrabAnimations>();
            if (crab != null)
            {
                crab.TakeDamage(attackDamage);
            }
            else
            {
                Debug.Log($"[DEBUG] Se detectó '{enemy.gameObject.name}' pero no tiene CrabAnimations.");
            }
        }

        yield return new WaitForSeconds(attackDuration - 0.2f);

        isAttacking = false;
        currentState = PlayerState.IDLE;
    }

    public void TakeDamage(int amount, Transform attackerTransform = null)
    {
        if (isDead || isInvincible) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(HurtRoutine(attackerTransform));
        }
    }

    IEnumerator HurtRoutine(Transform attacker)
    {
        isInvincible = true;
        isKnockback = true;
        isAttacking = false;

        currentState = PlayerState.DAMAGED;
        spumPrefabs.PlayAnimation(currentState, 0);

        // Aplica el impulso hacia atrás, solo en horizontal (sin lanzarte hacia arriba)
        if (attacker != null)
        {
            float directionX = transform.position.x < attacker.position.x ? -1f : 1f;
            rb.linearVelocity = new Vector2(directionX * knockbackForce.x, rb.linearVelocity.y);
        }

        yield return new WaitForSeconds(knockbackDuration);
        isKnockback = false;

        // Mantiene la invulnerabilidad un poco más para permitir escapar
        yield return new WaitForSeconds(0.4f);
        isInvincible = false;
        currentState = PlayerState.IDLE;
        spumPrefabs.PlayAnimation(currentState, 0);
    }

    void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;
        currentState = PlayerState.DEATH;
        spumPrefabs.PlayAnimation(currentState, 0);
    }
}