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
    public int danoPorGema = 3;
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

    [Header("Ataques de Espada con Hitbox Temporal (Nivel 4)")]
    [Tooltip("Tecla para el nuevo tajo horizontal (barrido lateral más ancho y bajo). También funciona con J.")]
    public KeyCode horizontalAttackKey = KeyCode.Z;
    public KaiSwordHitbox verticalSwordHitbox;
    public KaiSwordHitbox horizontalSwordHitbox;
    public Vector2 verticalHitboxLocalPos = new Vector2(0.85f, 0.45f);
    public Vector2 verticalHitboxSize = new Vector2(1.25f, 1.35f);
    public Vector2 horizontalHitboxLocalPos = new Vector2(1.15f, 0.22f);
    public Vector2 horizontalHitboxSize = new Vector2(2.15f, 0.72f);

    [Header("Checkpoint Activo")]
    public bool hasCheckpoint = false;
    public Vector3 lastCheckpointPosition;

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

    [Header("Joystick Virtual en Pantalla")]
    public bool usarJoystickEnPantalla = true;
    [Tooltip("Si está activo, el joystick virtual solo se crea en Nivel1 y Nivel2.")]
    public bool soloEnSampleScene = false;

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
        if (GameManager.Instance == null && GameObject.Find("GameUI") != null)
        {
            GameManager.EnsureExists();
        }

        rb = GetComponent<Rigidbody2D>();
        spumPrefabs = GetComponent<SPUM_Prefabs>();
        spumPrefabs.OverrideControllerInit();
        currentHealth = maxHealth;
        originalScale = transform.localScale;
        UpdateHealthUI();
        if (textoDano != null) textoDano.text = "";
        sr = GetComponent<SpriteRenderer>();

        if (attackPoint == null)
        {
            Transform ap = transform.Find("AttackPoint");
            if (ap != null)
            {
                attackPoint = ap;
            }
            else
            {
                GameObject apGo = new GameObject("AttackPoint");
                apGo.transform.SetParent(transform, false);
                apGo.transform.localPosition = new Vector3(0.85f, 0.35f, 0f);
                attackPoint = apGo.transform;
            }
        }
        attackPointDistance = Mathf.Abs(attackPoint.localPosition.x);
        if (attackPointDistance < 0.2f) attackPointDistance = 0.85f;

        if (groundCheck == null)
        {
            Transform gc = transform.Find("GroundCheck");
            if (gc != null) groundCheck = gc;
            else groundCheck = transform;
        }

        // Buscar el renderer del arma en la mano derecha de Kai
        InitWeaponRenderer();
        EnsureSwordHitboxes();

        // En Nivel 3 y Nivel 4 Kai ya cuenta con el bastón y la capa desbloqueados
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene == "Nivel3" || currentScene == "Nivel4")
        {
            SaveSystem.HasStaff = true;
            SaveSystem.HasCape = true;
        }

        // En Nivel 1 y Nivel 2 Kai inicia sin la capa (en Nivel 2 la debe descubrir en el Cofre Secreto)
        if (currentScene == "Nivel1" || currentScene == "Nivel2")
        {
            SaveSystem.HasCape = false;
        }

        // El objeto original 'Back' se apaga si aún no se tiene la capa, o se equipa si ya se desbloqueó
        Transform back = FindBackTransform();
        if (back != null)
        {
            SpriteRenderer srBack = back.GetComponent<SpriteRenderer>();
            if (srBack != null)
            {
                srBack.enabled = SaveSystem.HasCape;
            }
        }

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

        if (usarJoystickEnPantalla)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (!soloEnSampleScene || sceneName == "Nivel1" || sceneName == "Nivel2" || sceneName == "Nivel3" || sceneName == "Nivel4")
            {
                VirtualControlsUI.EnsureExists();
            }
        }
    }

    private void EnsureSwordHitboxes()
    {
        if (verticalSwordHitbox == null)
        {
            Transform vh = transform.Find("SwordHitbox_Vertical");
            if (vh != null)
            {
                verticalSwordHitbox = vh.GetComponent<KaiSwordHitbox>();
            }
            if (verticalSwordHitbox == null)
            {
                GameObject go = new GameObject("SwordHitbox_Vertical");
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(verticalHitboxLocalPos.x, verticalHitboxLocalPos.y, 0f);
                BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
                bc.isTrigger = true;
                bc.size = verticalHitboxSize;
                bc.enabled = false;
                verticalSwordHitbox = go.AddComponent<KaiSwordHitbox>();
            }
        }

        if (horizontalSwordHitbox == null)
        {
            Transform hh = transform.Find("SwordHitbox_Horizontal");
            if (hh != null)
            {
                horizontalSwordHitbox = hh.GetComponent<KaiSwordHitbox>();
            }
            if (horizontalSwordHitbox == null)
            {
                GameObject go = new GameObject("SwordHitbox_Horizontal");
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(horizontalHitboxLocalPos.x, horizontalHitboxLocalPos.y, 0f);
                BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
                bc.isTrigger = true;
                bc.size = horizontalHitboxSize;
                bc.enabled = false;
                horizontalSwordHitbox = go.AddComponent<KaiSwordHitbox>();
            }
        }
    }

    private float GetMoveHorizontal()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(moveX) < 0.01f && VirtualControlsUI.Instance != null)
        {
            moveX = VirtualControlsUI.Instance.HorizontalAxis;
        }
        return moveX;
    }

    private bool GetJumpDown()
    {
        if (Input.GetButtonDown("Jump")) return true;
        return VirtualControlsUI.Instance != null && VirtualControlsUI.Instance.JumpPressedThisFrame;
    }

    private bool GetAttackDown()
    {
        if (Input.GetKeyDown(KeyCode.X)) return true;
        return VirtualControlsUI.Instance != null && VirtualControlsUI.Instance.AttackPressedThisFrame;
    }

    private bool GetHorizontalAttackDown()
    {
        return Input.GetKeyDown(horizontalAttackKey) || Input.GetKeyDown(KeyCode.J);
    }

    private bool GetSwitchWeaponDown()
    {
        if (Input.GetKeyDown(KeyCode.C)) return true;
        return VirtualControlsUI.Instance != null && VirtualControlsUI.Instance.SwitchWeaponPressedThisFrame;
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

        float moveX = GetMoveHorizontal();
        Vector2 checkPos = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
        float checkRad = Mathf.Max(groundCheckRadius, 0.25f);
        isGrounded = Physics2D.OverlapCircle(checkPos, checkRad, groundLayer);
        if (!isGrounded)
        {
            isGrounded = Physics2D.OverlapBox((Vector2)transform.position + Vector2.down * 0.05f, new Vector2(0.35f, 0.15f), 0f, groundLayer);
        }

        // Salto
        if (GetJumpDown() && isGrounded && !isAttacking && !isKnockback)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            velocidadPlataforma = Vector2.zero;
            PlaySfx(Lib != null ? Lib.playerJump : null);
        }

        // Cambio de arma con C (solo si el bastón está desbloqueado)
        if (GetSwitchWeaponDown() && !isAttacking && !isKnockback)
        {
            if (SaveSystem.HasStaff)
            {
                currentWeapon = (currentWeapon == ActiveWeapon.Sword) ? ActiveWeapon.Staff : ActiveWeapon.Sword;
                UpdateWeaponUI();
                PlaySfx(Lib != null ? Lib.buttonClick : null);
            }
        }

        // Uso de arma con X (golpe de espada vertical hacia abajo o bastón)
        if (GetAttackDown() && !isAttacking && !isKnockback)
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
                // Ataque con espada vertical (actual)
                StartCoroutine(Attack());
            }
        }

        // Nuevo ataque de espada: Tajo horizontal / barrido lateral en tecla Z (o J)
        if (GetHorizontalAttackDown() && !isAttacking && !isKnockback)
        {
            StartCoroutine(HorizontalSlashAttack());
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
        float moveX = (isAttacking && isGrounded) ? 0f : GetMoveHorizontal();
        rb.linearVelocity = new Vector2(moveX * moveSpeed + velocidadPlataforma.x, rb.linearVelocity.y);
    }

    IEnumerator Attack()
    {
        isAttacking = true;
        currentState = PlayerState.ATTACK;
        spumPrefabs.PlayAnimation(currentState, 0);
        PlaySfx(Lib != null ? Lib.playerAttack : null);

        // Destello rápido en forma de arco cortante de espada
        Vector3 attackPosition = GetAttackPosition();
        MagicFX.Burst(attackPosition, new Color(1f, 1f, 1f, 0.9f), 10, 0.25f);

        yield return new WaitForSeconds(0.10f);

        // Activar hitbox trigger temporal del ataque vertical
        if (verticalSwordHitbox != null)
        {
            verticalSwordHitbox.ActivateHitbox(verticalHitboxLocalPos, verticalHitboxSize, attackDamage, facingDirection, 4f, 2f);
        }

        attackPosition = GetAttackPosition();
        float hitRadius = Mathf.Max(attackRange, 1.25f);
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPosition, hitRadius);
        System.Collections.Generic.HashSet<IEnemigo> damaged = new System.Collections.Generic.HashSet<IEnemigo>();

        foreach (Collider2D col in hitEnemies)
        {
            if (col.transform.IsChildOf(transform) || col.gameObject == gameObject) continue;

            IEnemigo enemigo = col.GetComponentInParent<IEnemigo>();
            if (enemigo == null)
                enemigo = col.GetComponent<IEnemigo>();

            if (enemigo != null && damaged.Add(enemigo))
            {
                if (verticalSwordHitbox == null || !verticalSwordHitbox.WasDamagedThisSwing(enemigo))
                {
                    enemigo.TakeDamage(attackDamage);
                    if (verticalSwordHitbox != null) verticalSwordHitbox.MarkAlreadyDamaged(enemigo);
                }
            }
        }

        // Verificación de respaldo alrededor de Kai por si el enemigo está muy pegado a su cuerpo
        Collider2D[] closeEnemies = Physics2D.OverlapCircleAll(transform.position + Vector3.up * 0.35f, 0.85f);
        foreach (Collider2D col in closeEnemies)
        {
            if (col.transform.IsChildOf(transform) || col.gameObject == gameObject) continue;

            IEnemigo enemigo = col.GetComponentInParent<IEnemigo>();
            if (enemigo == null) enemigo = col.GetComponent<IEnemigo>();

            if (enemigo != null && damaged.Add(enemigo))
            {
                if (verticalSwordHitbox == null || !verticalSwordHitbox.WasDamagedThisSwing(enemigo))
                {
                    enemigo.TakeDamage(attackDamage);
                    if (verticalSwordHitbox != null) verticalSwordHitbox.MarkAlreadyDamaged(enemigo);
                }
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0.08f, attackDuration - 0.10f));

        if (verticalSwordHitbox != null)
        {
            verticalSwordHitbox.DeactivateHitbox();
        }

        isAttacking = false;
        currentState = isGrounded ? PlayerState.IDLE : PlayerState.MOVE;
        spumPrefabs.PlayAnimation(currentState, 0);
    }

    /// <summary>
    /// Variante del ataque de espada en otra tecla (Z / J):
    /// Tajo horizontal (barrido lateral) con hitbox temporal más ancha y baja.
    /// </summary>
    IEnumerator HorizontalSlashAttack()
    {
        isAttacking = true;
        currentState = PlayerState.ATTACK;
        spumPrefabs.PlayAnimation(currentState, 0);
        PlaySfx(Lib != null ? Lib.playerAttack : null);

        // Efecto visual de barrido horizontal ancho y bajo frente a Kai
        Vector3 sweepCenter = transform.position + new Vector3(facingDirection * horizontalHitboxLocalPos.x, horizontalHitboxLocalPos.y, 0f);
        MagicFX.Burst(sweepCenter, new Color(0.75f, 0.92f, 1f, 0.95f), 14, 0.32f);
        MagicFX.Burst(sweepCenter + new Vector3(facingDirection * 0.55f, 0f, 0f), new Color(0.55f, 0.85f, 1f, 0.9f), 10, 0.25f);

        yield return new WaitForSeconds(0.08f);

        // Activar hitbox trigger temporal ancha y baja (solo activa durante el barrido)
        if (horizontalSwordHitbox != null)
        {
            horizontalSwordHitbox.ActivateHitbox(horizontalHitboxLocalPos, horizontalHitboxSize, attackDamage, facingDirection, 5.5f, 1.2f);
        }

        // Respaldo con OverlapBox ancho y bajo para alcanzar arañas y enemigos en fila
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(sweepCenter, horizontalHitboxSize, 0f);
        System.Collections.Generic.HashSet<IEnemigo> damaged = new System.Collections.Generic.HashSet<IEnemigo>();

        foreach (Collider2D col in hitEnemies)
        {
            if (col.transform.IsChildOf(transform) || col.gameObject == gameObject) continue;

            IEnemigo enemigo = col.GetComponentInParent<IEnemigo>();
            if (enemigo == null) enemigo = col.GetComponent<IEnemigo>();

            if (enemigo != null && damaged.Add(enemigo))
            {
                if (horizontalSwordHitbox == null || !horizontalSwordHitbox.WasDamagedThisSwing(enemigo))
                {
                    enemigo.TakeDamage(attackDamage);
                    if (horizontalSwordHitbox != null) horizontalSwordHitbox.MarkAlreadyDamaged(enemigo);
                }
                Rigidbody2D erb = col.attachedRigidbody;
                if (erb != null && erb.bodyType == RigidbodyType2D.Dynamic)
                {
                    erb.linearVelocity = new Vector2(facingDirection * 5.5f, 1.2f);
                }
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0.12f, attackDuration - 0.08f));

        if (horizontalSwordHitbox != null)
        {
            horizontalSwordHitbox.DeactivateHitbox();
        }

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
                enemigo.TakeDamage(Mathf.Max(2, attackDamage / 2));
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
        float dist = attackPointDistance > 0.1f ? attackPointDistance : 0.85f;
        float yOffset = attackPoint != null ? attackPoint.localPosition.y : 0.35f;
        return transform.position + Vector3.right * (dist * facingDirection) + Vector3.up * yOffset;
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
            MagicFX.Burst(transform.position + Vector3.up * 0.45f, new Color(0.70f, 0.35f, 1f, 0.65f), 8, 0.25f);
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
        if (verticalSwordHitbox != null) verticalSwordHitbox.DeactivateHitbox();
        if (horizontalSwordHitbox != null) horizontalSwordHitbox.DeactivateHitbox();

        // Siempre se muestra la muerte y la pantalla de Game Over.
        // Si hay checkpoint, el botón "Reintentar" del Game Over reaparece a Kai en el último checkpoint.
        isDead = true;
        isAttacking = false;
        isKnockback = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;
        currentState = PlayerState.DEATH;
        spumPrefabs.PlayAnimation(currentState, 0);
        PlaySfx(Lib != null ? Lib.playerDeath : null);

        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerDied();
    }

    public void SetCheckpoint(Vector3 checkpointPosition)
    {
        hasCheckpoint = true;
        lastCheckpointPosition = checkpointPosition;
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthUI();
    }

    /// <summary>Reaparece a Kai en el último checkpoint (lo llama el botón Reintentar del Game Over).</summary>
    public void RespawnAtCheckpoint()
    {
        StopAllCoroutines();
        isInvincible = false;
        StartCoroutine(RespawnAtCheckpointRoutine());
    }

    private IEnumerator RespawnAtCheckpointRoutine()
    {
        isAttacking = false;
        isKnockback = false;
        yield return null;

        transform.position = lastCheckpointPosition;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        currentHealth = maxHealth;
        UpdateHealthUI();

        isDead = false;
        currentState = PlayerState.IDLE;
        spumPrefabs.PlayAnimation(currentState, 0);
        StartInvincibility(1.5f);
        MagicFX.Burst(transform.position + Vector3.up * 0.5f, new Color(0.35f, 0.85f, 1f, 0.9f), 18, 0.4f);

        DragonGuardianBoss.ResetAllForCheckpointRespawn();
        SkeletonMageCompanion.ResetAllForCheckpointRespawn();
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
        else if (other.CompareTag("Gema") &&
                 other.GetComponent<GemFragment>() == null &&
                 other.GetComponent<ArcaniaFinalEnding>() == null &&
                 other.GetComponent<ArcanePowerCrystal>() == null)
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

    public Transform FindBackTransform()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Back")
                return child;
        }
        return null;
    }

    /// <summary>Equipa visualmente la capa original ('Back') haciéndola un poco más grande y con brillo morado tenue.</summary>
    public void EquipCape()
    {
        Transform back = FindBackTransform();
        if (back != null)
        {
            // Coordenadas basadas en el inspector del usuario y escala ligeramente superior (original: 1.1628x, 1.2900y -> 1.30x, 1.45y)
            back.localPosition = new Vector3(0.0565f, -0.0103f, 0f);
            back.localScale = new Vector3(1.30f, 1.45f, 1f);

            SpriteRenderer srBack = back.GetComponent<SpriteRenderer>();
            if (srBack != null)
            {
                srBack.enabled = true;
                // Suave tinte morado arcano sobre el sprite original
                srBack.color = new Color(0.92f, 0.75f, 1f, 1f);
            }

            // Brillito morado moderado (no muy brillante ni muy opaco)
            Transform existingGlow = back.Find("CapeMagicGlow");
            if (existingGlow == null)
            {
                SpriteRenderer glowSr = MagicFX.AddGlow(back, new Color(0.68f, 0.32f, 0.95f, 0.40f), 1.22f, 0f);
                glowSr.name = "CapeMagicGlow";
                glowSr.sortingLayerName = "Player";
                if (srBack != null)
                    glowSr.sortingOrder = srBack.sortingOrder - 1; // Colocado detrás de la capa
            }
            else
            {
                existingGlow.gameObject.SetActive(true);
            }

            visualCape = back.gameObject;
        }

        ActualizarTextoDano();
    }

    public Transform FindUnitRoot()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "UnitRoot")
                return child;
        }
        return transform;
    }

    /// <summary>Cambia la vestimenta de Kai al atuendo de Mago de Arcanía (túnica, cuello, capa SPUM y sombrero pixel-art) y aumenta su tamaño.</summary>
    public void TransformToMageOutfit(
        Sprite clothBody, Sprite clothLeft, Sprite clothRight,
        Sprite armorBody, Sprite armorLeft, Sprite armorRight,
        Sprite pantLeft, Sprite pantRight,
        Sprite mageCapeSprite = null, Sprite mageHatSprite = null,
        float scaleMultiplier = 1.48f)
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            switch (sr.gameObject.name)
            {
                case "ClothBody":
                    if (clothBody != null) { sr.sprite = clothBody; sr.color = Color.white; sr.enabled = true; }
                    break;
                case "21_LCArm":
                    if (clothLeft != null) { sr.sprite = clothLeft; sr.color = Color.white; sr.enabled = true; }
                    break;
                case "-19_RCArm":
                    if (clothRight != null) { sr.sprite = clothRight; sr.color = Color.white; sr.enabled = true; }
                    break;
                case "BodyArmor":
                    if (armorBody != null) { sr.sprite = armorBody; sr.color = Color.white; sr.enabled = true; }
                    break;
                case "25_L_Shoulder":
                    if (armorLeft != null) { sr.sprite = armorLeft; sr.color = Color.white; sr.enabled = true; }
                    break;
                case "-15_R_Shoulder":
                    if (armorRight != null) { sr.sprite = armorRight; sr.color = Color.white; sr.enabled = true; }
                    break;
                case "_2L_Cloth":
                    if (pantLeft != null) { sr.sprite = pantLeft; sr.color = Color.white; sr.enabled = true; }
                    break;
                case "_11R_Cloth":
                    if (pantRight != null) { sr.sprite = pantRight; sr.color = Color.white; sr.enabled = true; }
                    break;
                case "11_Helmet1":
                    if (mageHatSprite != null)
                    {
                        sr.sprite = mageHatSprite;
                        sr.color = Color.white;
                        sr.enabled = true;
                        sr.sortingOrder = 11;
                        sr.transform.localPosition = new Vector3(0.02f, 0.16f, 0f);
                        sr.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
                    }
                    break;
            }
        }

        EquipCape();

        if (scaleMultiplier > 1.01f)
        {
            originalScale *= scaleMultiplier;
            SetVisualFacing();
        }
    }

    public void StartInvincibility(float duration)
    {
        StartCoroutine(InvincibilityRoutine(duration));
    }

    private IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;
        yield return new WaitForSeconds(duration);
        isInvincible = false;
    }

    /// <summary>Activa o desactiva el control del jugador (escenas guiadas como la del bastón).</summary>
    public void SetControlEnabled(bool enabled)
    {
        controlEnabled = enabled;
        if (!enabled)
        {
            isAttacking = false;
            StopAllCoroutines();
            if (verticalSwordHitbox != null) verticalSwordHitbox.DeactivateHitbox();
            if (horizontalSwordHitbox != null) horizontalSwordHitbox.DeactivateHitbox();
            StopHorizontalMotion();
            currentState = PlayerState.IDLE;
            if (spumPrefabs != null)
            {
                spumPrefabs.PlayAnimation(PlayerState.IDLE, 0);
            }
            SetVisualFacing();
        }
    }

    /// <summary>Detiene de inmediato el desplazamiento horizontal de Kai.</summary>
    public void StopHorizontalMotion()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
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
        if (spumPrefabs != null)
        {
            spumPrefabs.PlayAnimation(currentState, 0);
        }
    }

    /// <summary>Fuerza la reproducción de una animación aunque coincida con currentState (útil en cinemáticas).</summary>
    public void ForceAnimation(PlayerState state)
    {
        currentState = state;
        if (spumPrefabs != null)
        {
            spumPrefabs.PlayAnimation(currentState, 0);
        }
    }

    /// <summary>Muestra visualmente el bastón o la espada durante una cinemática sin alterar el progreso guardado.</summary>
    public void SetCutsceneWeapon(bool useStaff)
    {
        if (weaponRenderer == null) return;
        if (useStaff && staffSprite != null)
        {
            weaponRenderer.sprite = staffSprite;
        }
        else if (!useStaff && swordSprite != null)
        {
            weaponRenderer.sprite = swordSprite;
        }
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
        string msg = "";
        if (bonusDano > 0)
            msg = "+" + bonusDano + " Daño";

        if (SaveSystem.HasCape)
        {
            if (msg.Length > 0) msg += "  |  ";
            msg += "🛡️ Capa: -20%";
        }

        if (string.IsNullOrEmpty(msg))
            msg = "+0 Daño";

        textoDano.text = msg;
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
