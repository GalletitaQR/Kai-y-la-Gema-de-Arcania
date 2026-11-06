using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float moveSpeed = 4f;
    public float jumpForce = 8f;

    [Header("Gema de fuerza")]
    public TextMeshProUGUI textoDano;
    public int danoPorGema = 10;
    private int bonusDano = 0;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("Vida")]
    public int maxHealth = 100;
    public int currentHealth;
    [Tooltip("Vida que recupera cada corazón")]
    public int vidaPorCorazon = 10;

    [Header("UI")]
    public BarraVida barraVida;

    [HideInInspector] 
    public Vector2 velocidadPlataforma;

    [Header("Ataque")]
    public float attackDuration = 0.4f;

    [Header("Ataque - Detección")]
    public Transform attackPoint;
    public float attackRange = 1f;
    public int attackDamage = 50; // Igual o mayor a maxHealth del cangrejo (50) para matarlo de un golpe
    public LayerMask enemyLayer;

    [Header("Retroceso por Golpe (Knockback)")]
    public Vector2 knockbackForce = new Vector2(3f, 4f); // Fuerza hacia atrás y arriba
    public float knockbackDuration = 0.25f;

    public enum ActiveWeapon
    {
        Sword,
        Staff
    }

    [Header("Armas")]
    public ActiveWeapon currentWeapon = ActiveWeapon.Sword;
    public float magicCooldown = 0.35f;
    private float nextMagicTime = 0f;
    [Header("Onda Expansiva del Bastón")]
    public float shockwaveRadius = 1.6f;
    public float shockwaveKnockback = 4.0f;

    [Header("Sprites de Armas (Visual)")]
    public Sprite swordSprite;
    public Sprite staffSprite;
    private SpriteRenderer weaponRenderer;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private SPUM_Prefabs spumPrefabs;
    private PlayerState currentState = PlayerState.IDLE;
    private bool isGrounded;
    private bool isAttacking = false;
    private bool isKnockback = false;
    private bool isInvincible = false;
    private bool isDead = false;
    private bool controlEnabled = true;
    private float facingDirection = 1f;
    private float attackPointDistance;
    private Vector3 originalScale;
    public int cristales = 0;
    private GameObject visualCape;

    public bool IsDead => isDead;
    public Rigidbody2D Body => rb;

    // Kai solo se mueve cuando el juego está en estado Playing (no durante diálogo, pausa o escenas guiadas).
    private bool CanAct
    {
        get
        {
            if (!controlEnabled) return false;
            return GameManager.Instance == null || GameManager.Instance.State == GameState.Playing;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spumPrefabs = GetComponent<SPUM_Prefabs>();
        spumPrefabs.OverrideControllerInit();
        currentHealth = maxHealth;
        originalScale = transform.localScale;
        UpdateHealthUI();
        if (textoDano != null) textoDano.text = "";
        sr = GetComponent<SpriteRenderer>();

        if (attackPoint != null)
            attackPointDistance = Mathf.Abs(attackPoint.localPosition.x);

        if (groundCheck == null)
        {
            Transform gc = transform.Find("GroundCheck");
            if (gc != null) groundCheck = gc;
            else groundCheck = transform;
        }

        // Buscar el renderer del arma en la mano derecha de Kai
        InitWeaponRenderer();

        // Si ya tenía el bastón o la capa desbloqueada por save, inicializar
        if (SaveSystem.HasCape)
        {
            EquipCape();
        }

        UpdateWeaponUI();
    }

    void Start()
    {
        InitWeaponRenderer();
        UpdateWeaponUI();
    }

    private void InitWeaponRenderer()
    {
        if (weaponRenderer == null)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "R_Weapon")
                {
                    weaponRenderer = t.GetComponent<SpriteRenderer>();
                    break;
                }
            }
        }

        if (weaponRenderer != null && swordSprite == null)
        {
            swordSprite = weaponRenderer.sprite;
        }

        if (staffSprite == null)
        {
            staffSprite = Resources.Load<Sprite>("staff");
#if UNITY_EDITOR
            if (staffSprite == null)
            {
                staffSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Generated/staff.png");
            }
#endif
        }
    }

    void Update()
    {
        if (isDead) return;

        if (!CanAct)
        {
            // En diálogo / pausa Kai se queda en reposo.
            if (controlEnabled && !isAttacking && !isKnockback && currentState != PlayerState.IDLE)
            {
                currentState = PlayerState.IDLE;
                spumPrefabs.PlayAnimation(currentState, 0);
            }
            return;
        }

        float moveX = Input.GetAxisRaw("Horizontal");
        Vector2 checkPos = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
        float checkRad = Mathf.Max(groundCheckRadius, 0.25f);
        isGrounded = Physics2D.OverlapCircle(checkPos, checkRad, groundLayer);
        if (!isGrounded)
        {
            isGrounded = Physics2D.OverlapBox((Vector2)transform.position + Vector2.down * 0.05f, new Vector2(0.35f, 0.15f), 0f, groundLayer);
        }

        // Salto
        if (Input.GetButtonDown("Jump") && isGrounded && !isAttacking && !isKnockback)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            velocidadPlataforma = Vector2.zero;
            PlaySfx(Lib != null ? Lib.playerJump : null);
        }

        // Cambio de arma con C (solo si el bastón está desbloqueado)
        if (Input.GetKeyDown(KeyCode.C) && !isAttacking && !isKnockback)
        {
            if (SaveSystem.HasStaff)
            {
                currentWeapon = (currentWeapon == ActiveWeapon.Sword) ? ActiveWeapon.Staff : ActiveWeapon.Sword;
                UpdateWeaponUI();
                PlaySfx(Lib != null ? Lib.buttonClick : null);
            }
        }

        // Uso de arma con X
        if (Input.GetKeyDown(KeyCode.X) && !isAttacking && !isKnockback)
        {
            if (currentWeapon == ActiveWeapon.Staff && SaveSystem.HasStaff)
            {
                if (Time.time >= nextMagicTime)
                {
                    nextMagicTime = Time.time + magicCooldown;
                    StartCoroutine(CastMagicRoutine());
                }
            }
            else
            {
                // Ataque con espada
                StartCoroutine(Attack());
            }
        }

        // Dirección lógica sin rotar a Kai: el sprite actual mira de frente.
        if (!isAttacking && !isKnockback)
        {
            if (moveX < 0f)
                facingDirection = -1f;
            else if (moveX > 0f)
                facingDirection = 1f;

            if (Mathf.Abs(moveX) > 0.01f)
                SetVisualFacing();
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
        if (isDead || isKnockback || !controlEnabled || rb.bodyType != RigidbodyType2D.Dynamic) return;

        if (!CanAct)
        {
            // Diálogo / fin de nivel: Kai no se desplaza por su cuenta.
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // En tierra se detiene durante el golpe cuerpo a cuerpo, pero en el aire conserva la inercia del salto
        float moveX = (isAttacking && isGrounded) ? 0f : Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveX * moveSpeed + velocidadPlataforma.x, rb.linearVelocity.y);
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
        PlaySfx(Lib != null ? Lib.playerAttack : null);

        yield return new WaitForSeconds(0.2f);

        Vector3 attackPosition = GetAttackPosition();

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPosition, attackRange);
        System.Collections.Generic.HashSet<IEnemigo> damaged = new System.Collections.Generic.HashSet<IEnemigo>();
        foreach (Collider2D col in hitEnemies)
        {
            IEnemigo enemigo = col.GetComponentInParent<IEnemigo>();
            if (enemigo == null)
                enemigo = col.GetComponent<IEnemigo>();

            if (enemigo != null && damaged.Add(enemigo))
                enemigo.TakeDamage(attackDamage);
        }

        yield return new WaitForSeconds(attackDuration - 0.2f);

        isAttacking = false;
        currentState = isGrounded ? PlayerState.IDLE : PlayerState.MOVE;
        spumPrefabs.PlayAnimation(currentState, 0);
    }

    IEnumerator CastMagicRoutine()
    {
        // 1. Onda Expansiva Arcana en 360° alrededor de Kai
        Vector3 shockwaveCenter = transform.position + Vector3.up * 0.35f;
        MagicFX.SpawnShockwave(shockwaveCenter, new Color(0.75f, 0.25f, 1f, 0.85f), shockwaveRadius, 0.26f);
        MagicFX.Burst(shockwaveCenter, new Color(0.85f, 0.35f, 1f, 0.95f), 25, 0.45f);

        // Dañar y repeler enemigos cercanos en 360° (evita que serpientes/slimes a corta distancia dañen a Kai)
        Collider2D[] closeEnemies = Physics2D.OverlapCircleAll(shockwaveCenter, shockwaveRadius);
        System.Collections.Generic.HashSet<IEnemigo> hitNearby = new System.Collections.Generic.HashSet<IEnemigo>();
        foreach (Collider2D col in closeEnemies)
        {
            IEnemigo enemigo = col.GetComponentInParent<IEnemigo>();
            if (enemigo == null) enemigo = col.GetComponent<IEnemigo>();

            if (enemigo != null && hitNearby.Add(enemigo))
            {
                enemigo.TakeDamage(attackDamage);

                // Empuje sutil (knockback) para despejar a enemigos cercanos
                Rigidbody2D erb = col.attachedRigidbody;
                if (erb != null && erb.bodyType == RigidbodyType2D.Dynamic)
                {
                    float pushX = Mathf.Sign(col.transform.position.x - transform.position.x);
                    if (Mathf.Abs(col.transform.position.x - transform.position.x) < 0.1f)
                        pushX = facingDirection;
                    erb.linearVelocity = new Vector2(pushX * shockwaveKnockback, 2.5f);
                }
            }
        }

        // 2. Disparo frontal del proyectil MagicBolt (nace desde la mano de Kai sin saltarse enemigos)
        Vector3 spawnPos = transform.position + Vector3.right * (0.35f * facingDirection) + Vector3.up * 0.35f;
        MagicBolt.Spawn(spawnPos, facingDirection, attackDamage);
        PlaySfx(Lib != null ? Lib.playerAttack : null);

        // Mueve el arma y brazo de Kai con animación de ataque tanto en tierra como en el aire
        spumPrefabs.PlayAnimation(PlayerState.ATTACK, 0);

        if (isGrounded)
        {
            isAttacking = true;
            yield return new WaitForSeconds(0.25f);
            isAttacking = false;
            currentState = PlayerState.IDLE;
            spumPrefabs.PlayAnimation(currentState, 0);
        }
        else
        {
            // En el aire ejecuta la animación del ataque sin congelar el movimiento horizontal ni cancelar el salto
            yield return new WaitForSeconds(0.25f);
            if (!isGrounded)
            {
                currentState = PlayerState.MOVE;
                spumPrefabs.PlayAnimation(currentState, 0);
            }
        }
    }

    private Vector3 GetAttackPosition()
    {
        Vector3 attackPosition = attackPoint.position;
        attackPosition.x = transform.position.x + attackPointDistance * facingDirection;
        return attackPosition;
    }

    public void TakeDamage(int amount, Transform attackerTransform = null)
    {
        if (isDead || isInvincible) return;
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

        // La capa reduce el daño recibido un 20%
        if (SaveSystem.HasCape)
        {
            amount = Mathf.RoundToInt(amount * 0.8f);
            if (amount < 1) amount = 1;
        }

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            PlaySfx(Lib != null ? Lib.playerHurt : null);
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
        PlaySfx(Lib != null ? Lib.playerDeath : null);

        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerDied();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.CompareTag("Fruta"))
        {
            // Los "frutas" ahora son corazones: suman vida y puntaje y solo reproducen un sonido.
            currentHealth = Mathf.Min(currentHealth + vidaPorCorazon, maxHealth);
            UpdateHealthUI();
            if (GameManager.Instance != null) GameManager.Instance.AddScore(10);
            PlaySfx(Lib != null ? Lib.heartPickup : null);
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Citrico"))
        {
            if (GameManager.Instance != null) GameManager.Instance.AddScore(-5);
            currentHealth = Mathf.Max(currentHealth - 10, 0);
            UpdateHealthUI();
            Destroy(other.gameObject);
            if (currentHealth <= 0) Die();
        }
        else if (other.CompareTag("Gema") && other.GetComponent<GemFragment>() == null)
        {
            // Respaldo: gema sin el componente GemFragment (comportamiento anterior).
            cristales++;
            AddDamageBonus(danoPorGema);
            Destroy(other.gameObject);
        }
    }

    // ---------- API para otros scripts ----------

    static AudioLibrary Lib => AudioManager.Lib;

    static void PlaySfx(AudioClip clip)
    {
        AudioManager.PlaySfx(clip);
    }

    void UpdateHealthUI()
    {
        if (barraVida != null) barraVida.Actualizar(currentHealth, maxHealth);
        if (GameManager.Instance != null) GameManager.Instance.SetPlayerHealth(currentHealth, maxHealth);
    }

    public void UpdateWeaponUI()
    {
        string weaponName = (currentWeapon == ActiveWeapon.Sword) ? "Espada" : "Bastón";
        if (GameManager.Instance != null)
            GameManager.Instance.SetPlayerWeapon(weaponName);

        UpdateWeaponVisual();
    }

    public void UpdateWeaponVisual()
    {
        if (weaponRenderer == null)
            InitWeaponRenderer();

        if (weaponRenderer != null)
        {
            if (currentWeapon == ActiveWeapon.Staff && staffSprite != null)
            {
                weaponRenderer.sprite = staffSprite;
            }
            else if (currentWeapon == ActiveWeapon.Sword && swordSprite != null)
            {
                weaponRenderer.sprite = swordSprite;
            }
        }
    }

    /// <summary>Equipa visualmente la capa protectora morada en Kai.</summary>
    public void EquipCape()
    {
        if (visualCape != null) return;

        // Buscar si SPUM ya tiene un objeto Back / Cape
        Transform backTransform = null;
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Back" || child.name == "P_Back")
            {
                backTransform = child;
                break;
            }
        }

        if (backTransform != null)
        {
            SpriteRenderer backRenderer = backTransform.GetComponent<SpriteRenderer>();
            if (backRenderer != null)
            {
                // Cargar sprite Back_2
                Sprite capeSprite = Resources.Load<Sprite>("Addons/Legacy/0_Unit/0_Sprite/7_Back/Back_2");
                if (capeSprite != null)
                    backRenderer.sprite = capeSprite;
                backRenderer.color = new Color(0.7f, 0.4f, 1f, 1f); // Tinte morado arcano
                backRenderer.enabled = true;
                visualCape = backTransform.gameObject;
                return;
            }
        }

        // Si no, crear aura/capa sutil como hijo
        GameObject capeGo = new GameObject("ArcaneCape");
        capeGo.transform.SetParent(transform, false);
        capeGo.transform.localPosition = new Vector3(0f, 0.4f, 0.1f);
        SpriteRenderer srCape = capeGo.AddComponent<SpriteRenderer>();
        srCape.sprite = MagicFX.GlowSprite;
        srCape.color = new Color(0.6f, 0.2f, 0.9f, 0.45f);
        srCape.sortingLayerName = "Player";
        srCape.sortingOrder = 1;
        capeGo.transform.localScale = new Vector3(0.8f, 1.2f, 1f);
        visualCape = capeGo;
    }

    /// <summary>Activa o desactiva el control del jugador (escenas guiadas como la del bastón).</summary>
    public void SetControlEnabled(bool enabled)
    {
        controlEnabled = enabled;
        if (!enabled)
        {
            isAttacking = false;
            StopAllCoroutines();
            transform.localScale = originalScale;
        }
    }

    /// <summary>Mira a la derecha (dir &gt; 0) o a la izquierda (dir &lt; 0).</summary>
    public void FaceDirection(float dir)
    {
        if (Mathf.Abs(dir) > 0.01f)
        {
            facingDirection = Mathf.Sign(dir);
            SetVisualFacing();
        }
    }

    private void SetVisualFacing()
    {
        float originalSign = Mathf.Sign(originalScale.x);
        if (Mathf.Abs(originalScale.x) < 0.001f)
            originalSign = 1f;

        float directionSign = facingDirection >= 0f ? 1f : -1f;
        transform.localScale = new Vector3(
            Mathf.Abs(originalScale.x) * originalSign * directionSign,
            originalScale.y,
            originalScale.z);
    }

    public void SetAnimation(PlayerState state)
    {
        if (state == currentState) return;
        currentState = state;
        spumPrefabs.PlayAnimation(currentState, 0);
    }

    /// <summary>Suma daño permanente (fragmento de la gema).</summary>
    public void AddDamageBonus(int amount)
    {
        attackDamage += amount;
        bonusDano += amount;
        ActualizarTextoDano();
    }

    /// <summary>Kai absorbe la energía del fragmento: brilla y crece un poco.</summary>
    public void PlayAbsorbFeedback(Color color)
    {
        StartCoroutine(AbsorbRoutine(color));
    }

    IEnumerator AbsorbRoutine(Color color)
    {
        SpriteRenderer glow = MagicFX.AddGlow(transform, color, 2.4f, 0f);
        glow.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        Vector3 glowScale = glow.transform.localScale;

        const float duration = 1f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float wave = Mathf.Sin(k * Mathf.PI);

            transform.localScale = originalScale * (1f + 0.25f * wave);

            glow.transform.localScale = glowScale * (1f + 0.4f * wave);
            Color c = color;
            c.a = color.a * wave;
            glow.color = c;
            yield return null;
        }

        transform.localScale = originalScale;
        Destroy(glow.gameObject);
    }

    void ActualizarTextoDano()
    {
        if (textoDano == null) return;
        textoDano.text = "+" + bonusDano + " Damage";
        StopCoroutine("PulsoTexto");
        StartCoroutine("PulsoTexto");
    }

    IEnumerator PulsoTexto()
    {
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float escala = Mathf.Lerp(1.5f, 1f, t / 0.3f);
            textoDano.transform.localScale = new Vector3(escala, escala, 1f);
            yield return null;
        }
        textoDano.transform.localScale = Vector3.one;
    }

    public void MuerteInstantanea()
    {
        if (isDead) return;

        currentHealth = 0;
        UpdateHealthUI();
        Die();
    }
}
