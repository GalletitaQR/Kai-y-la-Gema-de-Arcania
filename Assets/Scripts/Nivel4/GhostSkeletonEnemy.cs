using System.Collections;
using UnityEngine;

/// <summary>
/// Enemigo Fantasma Esqueleto (GhostKnight) para el Nivel 4.
/// Permanece semi-oculto esperando a Kai; cuando Kai se acerca, se materializa por sorpresa,
/// flota hacia él y ataca con una breve ventana de anticipación que obliga a moverse y atacar en el momento justo.
/// </summary>
public class GhostSkeletonEnemy : MonoBehaviour, IEnemigo
{
    [Header("Emboscada y Movimiento")]
    public float ambushRange = 5.5f;
    public float chaseRange = 8.5f;
    public float floatSpeed = 2.6f;
    public float hiddenAlpha = 0.75f;
    public float revealedAlpha = 1f;

    [Header("Ataque")]
    public float attackRange = 1.35f;
    public int attackDamage = 18;
    public float attackWindup = 0.38f;
    public float attackCooldown = 1.5f;

    [Header("Vida y Puntaje")]
    public int maxHealth = 75;
    public int currentHealth = 75;
    public int scoreValue = 40;
    public BarraVida barraVida;

    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D enemyCollider;
    private SpriteRenderer[] childRenderers;

    private Vector3 startPos;
    private float baseScaleX = 0.26f;
    private bool isAmbushing = false;
    private bool isAttacking = false;
    private bool isDead = false;
    private bool canAttack = true;
    private bool inMoveAnim = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        enemyCollider = GetComponent<Collider2D>();
        childRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        startPos = transform.position;
        baseScaleX = Mathf.Abs(transform.localScale.x);
        if (baseScaleX < 0.01f) baseScaleX = 0.26f;

        if (rb != null)
        {
            rb.gravityScale = 0f;
        }

        currentHealth = maxHealth;
        if (barraVida != null) barraVida.Actualizar(currentHealth, maxHealth);

        SetRenderersAlpha(hiddenAlpha);
    }

    void Update()
    {
        if (isDead || isAttacking) return;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            if (player == null) return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        if (!isAmbushing)
        {
            // Flota suavemente en su sitio mientras espera emboscar
            if (rb != null)
                rb.linearVelocity = new Vector2(0f, Mathf.Sin(Time.time * 2.5f) * 0.25f);

            if (dist <= ambushRange)
            {
                isAmbushing = true;
                SetRenderersAlpha(revealedAlpha);
                AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.enemyHit : null);
            }
            return;
        }

        // Si ya emboscó a Kai
        if (dist <= chaseRange)
        {
            FaceDirection(player.position.x - transform.position.x);

            if (dist <= attackRange)
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                if (canAttack)
                {
                    StartCoroutine(SurpriseStrikeRoutine());
                }
            }
            else
            {
                Vector2 targetPos = (Vector2)player.position + new Vector2(0f, 0.2f);
                Vector2 dir = (targetPos - (Vector2)transform.position).normalized;
                if (rb != null)
                    rb.linearVelocity = dir * floatSpeed;
                else
                    transform.position += (Vector3)(dir * floatSpeed * Time.deltaTime);

                if (!inMoveAnim && animator != null)
                {
                    animator.SetTrigger("MoveTrigger");
                    inMoveAnim = true;
                }
            }
        }
        else
        {
            // Regresa hacia su punto de guardia si Kai se aleja mucho
            Vector2 toHome = (Vector2)(startPos - transform.position);
            if (toHome.magnitude > 0.25f)
            {
                FaceDirection(toHome.x);
                if (rb != null) rb.linearVelocity = toHome.normalized * (floatSpeed * 0.75f);
            }
            else
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                isAmbushing = false;
                SetRenderersAlpha(hiddenAlpha);
            }
        }
    }

    private IEnumerator SurpriseStrikeRoutine()
    {
        canAttack = false;
        isAttacking = true;
        inMoveAnim = false;

        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.ResetTrigger("MoveTrigger");
            animator.SetTrigger("AttackTrigger");
        }

        // Ventana de anticipación para que Kai pueda esquivar o atacar en el momento justo
        yield return new WaitForSeconds(attackWindup);

        if (!isDead && player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= attackRange + 0.45f)
            {
                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.TakeDamage(attackDamage, transform);
                }
            }
        }

        yield return new WaitForSeconds(0.4f);
        isAttacking = false;

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
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

    private void SetRenderersAlpha(float alpha)
    {
        if (childRenderers == null) return;
        for (int i = 0; i < childRenderers.Length; i++)
        {
            SpriteRenderer sr = childRenderers[i];
            if (sr == null) continue;
            // No activar el SpriteRenderer vacío de la raíz ni efectos ocultos con alpha 0 original
            if (sr.gameObject == gameObject || sr.sprite == null) continue;
            if (sr.name.Contains("Effect") || sr.name.Contains("Cleave")) continue;

            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        // Si recibe un golpe antes de emboscar, se revela inmediatamente
        if (!isAmbushing)
        {
            isAmbushing = true;
            SetRenderersAlpha(revealedAlpha);
        }

        currentHealth -= amount;
        if (barraVida != null) barraVida.Actualizar(currentHealth, maxHealth);
        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.enemyHit : null);

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (animator != null && !isAttacking)
        {
            inMoveAnim = false;
            animator.ResetTrigger("MoveTrigger");
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
            animator.ResetTrigger("MoveTrigger");
            animator.ResetTrigger("AttackTrigger");
            animator.ResetTrigger("StunedTrigger");
            animator.SetTrigger("DeathTrigger");
        }

        Destroy(gameObject, 1.15f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, ambushRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

