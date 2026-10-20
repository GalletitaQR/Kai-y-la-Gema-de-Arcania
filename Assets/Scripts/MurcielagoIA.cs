using UnityEngine;
using System.Collections;

public class MurcielagoIA : MonoBehaviour, IEnemigo
{
    [Header("Barra de vida")]
    public BarraVida barraVida;

    [Header("Patrulla")]
    public Transform pointA;
    public Transform pointB;
    public float patrolSpeed = 1.5f;

    [Header("Detección y ataque")]
    public float detectionRange = 3f;
    public float chaseSpeed = 2.5f;
    public float distanciaParada = 0.8f;   // a qué distancia se queda de Kai
    public float attackRange = 1f;         // debe ser MAYOR que distanciaParada
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;
    public LayerMask playerLayer;

    [Header("Vida")]
    public int maxHealth = 30;
    public int currentHealth;

    private Vector3 patrolPointA, patrolPointB;
    private bool goingToB = true;
    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D miCollider;
    private bool isDead = false;
    private bool canAttack = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        miCollider = GetComponent<Collider2D>();

        if (pointA != null) patrolPointA = pointA.position;
        if (pointB != null) patrolPointB = pointB.position;

        currentHealth = maxHealth;
        if (barraVida != null) barraVida.Actualizar(currentHealth, maxHealth);
    }

    void FixedUpdate()
    {
        if (isDead) return;

        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        player = hit != null ? hit.transform : null;

        Vector2 direccion;

        if (player != null)
        {
            // Flota un poco por encima de Kai
            Vector2 objetivo = (Vector2)player.position + Vector2.up * 0.6f;
            direccion = objetivo - (Vector2)transform.position;
            float distancia = direccion.magnitude;

            if (distancia > distanciaParada)
            {
                // Todavía lejos: se acerca
                rb.linearVelocity = direccion.normalized * chaseSpeed;
            }
            else
            {
                // Ya está cerca: se queda flotando con un leve vaivén
                rb.linearVelocity = new Vector2(0f, Mathf.Sin(Time.time * 4f) * 0.5f);
            }

            // Ataca si está dentro del rango
            if (distancia <= attackRange && canAttack)
                StartCoroutine(Atacar());

            // Mira hacia Kai
            Voltear(player.position.x - transform.position.x);
        }
        else
        {
            // Patrulla entre A y B
            if (pointA == null || pointB == null)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector3 destino = goingToB ? patrolPointB : patrolPointA;
            direccion = destino - transform.position;
            rb.linearVelocity = direccion.normalized * patrolSpeed;

            if (direccion.magnitude < 0.2f) goingToB = !goingToB;

            Voltear(direccion.x);
        }
    }

    IEnumerator Atacar()
    {
        canAttack = false;
        if (animator != null) animator.SetTrigger("Attack");

        // Pequeña espera para que el golpe coincida con la animación
        yield return new WaitForSeconds(0.2f);

        if (!isDead && player != null)
        {
            float distancia = Vector2.Distance(transform.position, player.position);
            if (distancia <= attackRange + 0.3f)
            {
                PlayerController jugador = player.GetComponent<PlayerController>();
                if (jugador != null) jugador.TakeDamage(attackDamage, transform);
            }
        }

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    void Voltear(float x)
    {
        if (x > 0.05f) transform.localRotation = Quaternion.Euler(0, 0, 0);
        else if (x < -0.05f) transform.localRotation = Quaternion.Euler(0, 180, 0);
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (barraVida != null) barraVida.Actualizar(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }

    void Die()
    {
        isDead = true;
        StopAllCoroutines();

        if (miCollider != null) miCollider.enabled = false;   // ya no daña ni se puede golpear
        if (barraVida != null) barraVida.gameObject.SetActive(false);

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 1f;                                  // cae al morir

        if (animator != null) animator.SetTrigger("Death");
        Destroy(gameObject, 2f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, distanciaParada);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}