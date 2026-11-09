using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// =====================================================================
//  DarkKnightBoss  -  Mini jefe 2D (Unity)
//  Requiere: Rigidbody2D + BoxCollider2D + Animator (ya los tienes).
//  Todos los ataques son melee (hitbox instantánea), cada uno con su
//  propia forma (Box / Circle / Cone), rango, daño y sonido.
// =====================================================================

public enum BossHitShape { Box, Circle, Cone }

[System.Serializable]
public class BossAttack
{
    public string name = "Ataque";
    public bool active = true;

    [Header("Animator")]
    [Tooltip("Nombre EXACTO del parámetro Trigger en el Animator")]
    public string animatorTrigger = "Attack1";

    [Header("Cuándo puede usarse")]
    [Tooltip("Distancia mínima entre colliders (borde a borde)")]
    public float minRange = 0f;
    [Tooltip("Distancia máxima entre colliders (borde a borde)")]
    public float maxRange = 2f;
    [Tooltip("Diferencia vertical mínima (pies del jugador - pies del jefe). Positivo = jugador más arriba")]
    public float minVertical = -1.5f;
    public float maxVertical = 1.5f;
    [Range(0f, 1f)] public float minHealthPercent = 0f;
    [Range(0f, 1f)] public float maxHealthPercent = 1f;
    [Tooltip("Probabilidad relativa de ser elegido")]
    public float weight = 1f;
    public float cooldown = 2f;

    [Header("Hitbox (se espeja según hacia dónde mire)")]
    public BossHitShape shape = BossHitShape.Box;
    [Tooltip("Offset desde el centro del collider, en unidades de mundo")]
    public Vector2 offset = new Vector2(1.3f, 0.3f);
    [Tooltip("Box: ancho/alto. Circle y Cone: size.x = radio")]
    public Vector2 size = new Vector2(2f, 1.5f);
    [Tooltip("Solo Cone: apertura total en grados")]
    public float coneAngle = 100f;
    [Tooltip("Solo Cone: inclinación hacia arriba (+) o abajo (-)")]
    public float coneTilt = 0f;

    [Header("Tiempos (ajústalos a la duración de tu animación)")]
    [Tooltip("Segundos desde que inicia la animación hasta el primer golpe")]
    public float windup = 0.3f;
    public int hits = 1;
    public float timeBetweenHits = 0.2f;
    [Tooltip("Duración total del ataque hasta empezar la recuperación")]
    public float totalDuration = 0.7f;
    [Tooltip("Tiempo de recuperación después del ataque")]
    public float recovery = 0.3f;
    [Tooltip("Si es true, durante la recuperación el jefe recibe daño extra (ventana para el jugador)")]
    public bool punishable = false;
    [Tooltip("Si es true, no se interrumpe cuando lo golpean")]
    public bool superArmor = false;

    [Header("Daño")]
    public int damage = 10;
    public Vector2 knockback = new Vector2(5f, 2f);

    [Header("Avance durante el ataque")]
    public float lungeSpeed = 0f;
    public float lungeStart = 0.1f;
    public float lungeDuration = 0.2f;

    [Header("Audio")]
    public AudioClip swingSound;
    public AudioClip hitSound;
    [Range(0f, 1f)] public float volume = 1f;

    [HideInInspector] public float nextAvailableTime;
}

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class DarkKnightBoss : MonoBehaviour, IEnemigo
{
    enum State { Chase, Attacking, Jumping, Parrying, Staggered, Dead }

    // ------------------------------------------------------------ OBJETIVO
    [Header("Objetivo")]
    public string playerTag = "Player";
    [Tooltip("0 = persigue siempre. Si > 0 solo reacciona dentro de esta distancia horizontal")]
    public float aggroRange = 0f;
    [Tooltip("Evita que gire constantemente cuando el jugador está justo encima")]
    public float turnDeadZone = 0.3f;
    [Tooltip("Marca si el sprite original mira a la derecha")]
    public bool spriteFacesRight = true;

    // ------------------------------------------------------------------ SALA
    [Header("Sala / zona de detección")]
    [Tooltip("Si está activo, el jefe solo se activa cuando el jugador está DENTRO de la sala")]
    public bool useRoomArea = true;
    [Tooltip("Opcional (recomendado): un BoxCollider2D con Is Trigger en un objeto aparte (NO hijo del jefe). Se edita con handles en la escena y reemplaza Center/Size")]
    public BoxCollider2D roomCollider;
    [Tooltip("Centro de la sala en coordenadas de mundo (si no usas roomCollider)")]
    public Vector2 roomCenter;
    [Tooltip("Ancho y alto de la sala (si no usas roomCollider)")]
    public Vector2 roomSize = new Vector2(30f, 12f);
    [Tooltip("El jefe no camina, salta ni se lanza fuera de los límites de la sala")]
    public bool stayInsideRoom = true;
    [Tooltip("Segundos de espera desde que el jugador entra hasta que el jefe empieza a actuar")]
    public float activationDelay = 0.5f;
    [Tooltip("Si el jugador sale de la sala, el jefe regresa a su punto inicial")]
    public bool returnHomeWhenPlayerLeaves = true;
    [Tooltip("Si el jugador sale de la sala, el jefe recupera toda su vida")]
    public bool resetHealthWhenPlayerLeaves = false;
    [Tooltip("Oculta la barra de vida del mundo hasta que el jefe se activa")]
    public bool hideBarUntilActive = true;
    public UnityEvent onActivated;
    public UnityEvent onPlayerLeft;

    // --------------------------------------------------------- VIDA / PUNTAJE
    [Header("Vida y puntaje")]
    public int maxHealth = 300;
    public int scoreValue = 1000;
    [Tooltip("Al morir llama player.SendMessage(scoreMethodName, scoreValue)")]
    public bool sendScoreToPlayer = true;
    public string scoreMethodName = "AddScore";
    public float destroyDelay = 2.5f;

    // ------------------------------------------------------------ BARRA VIDA
    [Header("Barra de vida")]
    public bool showWorldHealthBar = true;
    public Vector2 barSize = new Vector2(3f, 0.3f);
    public float barOffsetY = 0.5f;
    public float barBorder = 0.05f;
    public int barSortingOrder = 100;
    public Color barFrameColor = Color.black;
    public Color barBackColor = new Color(0.25f, 0f, 0f, 1f);
    public Color barHighColor = new Color(0.2f, 0.9f, 0.2f, 1f);
    public Color barLowColor = new Color(0.95f, 0.15f, 0.15f, 1f);
    [Tooltip("Opcional: Image (Type=Filled) de un Canvas para una barra de jefe en pantalla")]
    public Image screenFillImage;

    // ------------------------------------------------------------- MOVIMIENTO
    [Header("Movimiento")]
    public float moveSpeed = 3.5f;
    public float acceleration = 30f;
    public float deceleration = 40f;
    [Tooltip("Distancia (borde a borde) a la que se detiene si no tiene ataques listos")]
    public float closeGap = 1f;

    [Header("Detección de entorno")]
    [Tooltip("Capas que cuentan como suelo/pared. Si está en Nothing usa todo menos su propia capa")]
    public LayerMask groundLayers;
    public float groundCheckDistance = 0.12f;
    public float wallCheckDistance = 0.35f;
    public float stepHeight = 0.5f;
    public float ledgeCheckForward = 0.3f;
    public float ledgeCheckDepth = 1.2f;
    [Tooltip("Si el jugador está al menos esta distancia más abajo, el jefe se deja caer del borde")]
    public float dropThreshold = 1.5f;

    // ------------------------------------------------------------------ SALTO
    [Header("Salto")]
    [Tooltip("Altura máxima que puede saltar (unidades). Ajusta a la separación de tus plataformas")]
    public float maxJumpHeight = 5f;
    [Tooltip("Margen extra sobre la plataforma del jugador")]
    public float jumpClearance = 0.8f;
    [Tooltip("El jugador debe estar al menos así de alto para que salte hacia él")]
    public float jumpMinHeightDiff = 1.2f;
    [Tooltip("Distancia horizontal máxima desde la que salta hacia una plataforma")]
    public float jumpMaxHorizontalDistance = 7f;
    [Tooltip("Segundos que el jugador debe estar arriba antes de saltar (evita saltar por cada salto del jugador)")]
    public float jumpReactionDelay = 0.35f;
    public float jumpCooldown = 1.2f;
    public float obstacleJumpHeight = 1.8f;
    public float gapJumpHeight = 2f;
    public float gapJumpSpeed = 6f;
    public float airSpeedMax = 9f;
    public float airAcceleration = 25f;
    [Tooltip("Qué tanto frena en el aire al acercarse en X al jugador")]
    public float airHoming = 2.5f;

    [Header("Salto de acercamiento (leap)")]
    public bool leapEnabled = true;
    public float leapMinGap = 4f;
    public float leapMaxGap = 10f;
    public float leapHeight = 1.5f;
    public float leapSpeed = 10f;
    public float leapChancePerSecond = 0.25f;
    public float leapCooldown = 5f;

    // ----------------------------------------------------------------- COMBATE
    [Header("Ataques (todos cuerpo a cuerpo)")]
    public List<BossAttack> attacks = new List<BossAttack>();
    public float minTimeBetweenAttacks = 0.5f;
    [Tooltip("Multiplica el peso del último ataque usado (0.3 = menos probable que repita)")]
    [Range(0f, 1f)] public float repeatPenalty = 0.3f;

    [Header("Comunicación con el jugador")]
    [Tooltip("Nombre del método de tu jugador que recibe el daño")]
    public string damageMethodName = "TakeDamage";
    [Tooltip("Actívalo si el método de tu jugador recibe float en vez de int")]
    public bool damageAsFloat = false;
    [Tooltip("Muestra en consola cuándo el jefe golpea y cuándo recibe daño")]
    public bool debugLogs = true;

    [Header("Daño por contacto")]
    public int contactDamage = 5;
    public Vector2 contactKnockback = new Vector2(4f, 2f);
    public float contactInterval = 1f;
    [Tooltip("Se invoca cada vez que el jefe golpea al jugador (úsalo para aplicar knockback/stun propio)")]
    public UnityEvent<GameObject> onHitPlayer;

    // ------------------------------------------------------ DEFENSA / BALANCE
    [Header("Balance: que sea posible matarlo")]
    public float hitInvulnerability = 0.08f;
    [Range(0f, 1f)] public float staggerChance = 0.3f;
    public float staggerDuration = 0.45f;
    public float staggerCooldown = 3f;
    public float staggerKnockbackSpeed = 3f;
    [Tooltip("Daño extra mientras está en recuperación de un ataque pesado o tras un parry")]
    public float punishMultiplier = 1.5f;

    [Header("Parry")]
    public bool canParry = true;
    [Range(0f, 1f)] public float parryChance = 0.2f;
    public float parryCooldown = 6f;
    public float parryDuration = 0.5f;
    [Tooltip("Si bloqueó un golpe, queda vulnerable este tiempo")]
    public float parryPunishTime = 1f;

    [Header("Fase 2")]
    [Range(0f, 1f)] public float phase2HealthPercent = 0.5f;
    public float phase2SpeedMultiplier = 1.25f;
    [Tooltip("Menor = ataca más seguido")]
    public float phase2CooldownMultiplier = 0.75f;
    public float phase2ParryMultiplier = 0.5f;

    // ----------------------------------------------------------------- ANIMATOR
    [Header("Nombres de parámetros del Animator (vacío o inexistente = se ignora)")]
    public string speedFloat = "Speed";
    public string groundedBool = "IsGrounded";
    public string verticalSpeedFloat = "VelocityY";
    public string jumpTrigger = "Jump";
    public string dieTrigger = "Die";
    public string parryTrigger = "Parry";
    public string hurtTrigger = "Hit";
    public bool warnMissingParameters = true;

    // -------------------------------------------------------------------- AUDIO
    [Header("Sonidos")]
    public AudioClip[] footstepSounds;
    public float footstepInterval = 0.4f;
    public AudioClip jumpSound;
    public AudioClip landSound;
    public AudioClip hurtSound;
    public AudioClip parrySound;
    public AudioClip deathSound;
    [Range(0f, 1f)] public float masterVolume = 1f;
    public float pitchVariation = 0.05f;

    // ------------------------------------------------------------------ FEEDBACK
    [Header("Feedback")]
    public Color hitFlashColor = new Color(1f, 0.4f, 0.4f, 1f);
    public float hitFlashTime = 0.08f;

    // ------------------------------------------------------------------ EVENTOS
    [Header("Eventos")]
    public UnityEvent<int, int> onDamaged;     // vida actual, vida máxima
    public UnityEvent onParry;
    public UnityEvent onPhase2;
    public UnityEvent onDeath;
    public UnityEvent<int> onScoreAwarded;
    public static event System.Action<int> OnAnyBossScore;

    // ============================================================ PRIVADO
    Rigidbody2D rb;
    Collider2D col;
    Animator anim;
    SpriteRenderer sr;
    AudioSource audioSrc;

    Transform player;
    Collider2D playerCol;
    Rigidbody2D playerRb;

    State state = State.Chase;
    int currentHealth;
    int facing = 1;
    Vector3 baseScale;
    Color originalColor = Color.white;
    bool grounded, phase2Triggered, parryActive, parrySuccess;
    bool active;
    float readyTime;
    float stuckTimer, lastCmdX;
    Vector3 homePos;
    float airTime, jumpStartTime, jumpHSpeed;
    float nextJumpTime, nextLeapTime, nextAttackAllowedTime, nextParryTime, nextStaggerTime, nextContactTime, nextFindTime;
    float invulnerableUntil, punishUntil, playerAboveTimer, stepTimer;
    BossAttack currentAttack, lastAttack;
    Coroutine actionRoutine, flashRoutine;
    LayerMask groundMask;
    ContactFilter2D filter;
    readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(8);
    readonly List<BossAttack> candidates = new List<BossAttack>();

    readonly Dictionary<int, AnimatorControllerParameterType> animParams = new Dictionary<int, AnimatorControllerParameterType>();
    readonly HashSet<string> warned = new HashSet<string>();

    Transform barRoot, barFill;
    SpriteRenderer barFillSr;
    Texture2D barTex;
    Sprite barSprite;

    // ---------- propiedades útiles
    public bool IsDead => state == State.Dead;
    public int CurrentHealth => currentHealth;
    public float HealthPercent => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    bool InPhase2 => phase2Triggered;
    float MoveSpeedNow => moveSpeed * (InPhase2 ? phase2SpeedMultiplier : 1f);
    float CooldownMult => InPhase2 ? phase2CooldownMultiplier : 1f;

    // ---------- velocidad compatible Unity 6 y anteriores
    static Vector2 GetVel(Rigidbody2D r)
    {
#if UNITY_6000_0_OR_NEWER
        return r.linearVelocity;
#else
        return r.velocity;
#endif
    }
    static void SetVel(Rigidbody2D r, Vector2 v)
    {
#if UNITY_6000_0_OR_NEWER
        r.linearVelocity = v;
#else
        r.velocity = v;
#endif
    }
    Vector2 Vel { get { return GetVel(rb); } set { SetVel(rb, value); } }
    void SetVelX(float x) { Vector2 v = Vel; v.x = x; Vel = v; }
    float GravityMagnitude() { return Mathf.Max(0.01f, Mathf.Abs(Physics2D.gravity.y * rb.gravityScale)); }

    // ================================================================ UNITY
    void Reset()
    {
        attacks = DefaultAttacks();
        roomCenter = transform.position;
    }

    [ContextMenu("Centrar sala en el jefe")]
    void CenterRoomOnBoss()
    {
        roomCenter = transform.position;
    }

    [ContextMenu("Cargar ataques por defecto")]
    void LoadDefaults()
    {
        attacks = DefaultAttacks();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr) originalColor = sr.color;

        audioSrc = GetComponent<AudioSource>();
        if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;

        rb.freezeRotation = true;

        baseScale = transform.localScale;
        facing = ((baseScale.x >= 0f) == spriteFacesRight) ? 1 : -1;

        if (attacks == null || attacks.Count == 0) attacks = DefaultAttacks();

        // Suma Ground Layers + todo con lo que la capa del jefe colisiona físicamente (menos su propia capa).
        // Así cualquier bloque que lo detenga cuenta como pared, aunque esté en otra capa.
        int physMask = Physics2D.GetLayerCollisionMask(gameObject.layer);
        int mask = (physMask | groundLayers.value) & ~(1 << gameObject.layer);
        groundMask = mask != 0 ? (LayerMask)mask : (LayerMask)(~(1 << gameObject.layer));
        filter = new ContactFilter2D();
        filter.SetLayerMask(groundMask);
        filter.useTriggers = false;

        if (anim)
            foreach (var p in anim.parameters) animParams[p.nameHash] = p.type;

        currentHealth = maxHealth;
        homePos = transform.position;
        CacheRoomBounds();
    }

    void Start()
    {
        FindPlayer();
        BuildHealthBar();
        UpdateHealthUI();
    }

    void OnDestroy()
    {
        if (barRoot) Destroy(barRoot.gameObject);
        if (barSprite) Destroy(barSprite);
        if (barTex) Destroy(barTex);
    }

    void Update()
    {
        if (state == State.Dead) return;
        HandleFootsteps();
    }

    void LateUpdate()
    {
        if (barRoot && state != State.Dead)
        {
            barRoot.gameObject.SetActive(active || !hideBarUntilActive);
            Bounds b = col.bounds;
            barRoot.position = new Vector3(b.center.x, b.max.y + barOffsetY, 0f);
        }
    }

    void FixedUpdate()
    {
        if (state == State.Dead) return;

        UpdateGrounded();

        if (player == null)
        {
            if (Time.time >= nextFindTime) { FindPlayer(); nextFindTime = Time.time + 1f; }
            if (player == null) { Decel(); UpdateAnimator(); return; }
        }

        UpdateActivation();

        if (!active || Time.time < readyTime)
        {
            if (state == State.Chase)
            {
                if (!active && returnHomeWhenPlayerLeaves && grounded) ReturnHome();
                else Decel();
            }
            else if (state == State.Jumping) CheckLanding();

            ClampToRoom();
            UpdateAnimator();
            return;
        }

        switch (state)
        {
            case State.Chase:
                if (!grounded) AirControl(); else ChaseTick();
                break;
            case State.Jumping:
                AirControl();
                CheckLanding();
                break;
            case State.Parrying:
            case State.Staggered:
                Decel();
                break;
        }

        ClampToRoom();
        UpdateAnimator();
    }

    // ============================================================== IA
    void ChaseTick()
    {
        float cx = col.bounds.center.x;
        float dx = PlayerCenterX() - cx;
        float dxAbs = Mathf.Abs(dx);
        float dy = PlayerFeetY() - col.bounds.min.y;
        float gap = ComputeGap();

        FacePlayer();

        // ¿El jugador lleva rato en una plataforma más alta?
        bool playerSettled = playerRb == null || Mathf.Abs(GetVel(playerRb).y) < 1f;
        if (dy >= jumpMinHeightDiff && playerSettled) playerAboveTimer += Time.fixedDeltaTime;
        else playerAboveTimer = 0f;

        // 1) Atacar
        if (Time.time >= nextAttackAllowedTime)
        {
            BossAttack a = SelectAttack(gap, dy);
            if (a != null) { StartAction(AttackRoutine(a)); return; }
        }

        // 2) Saltar hacia una plataforma más alta
        if (playerAboveTimer >= jumpReactionDelay && Time.time >= nextJumpTime &&
            dy <= maxJumpHeight - jumpClearance && dxAbs <= jumpMaxHorizontalDistance)
        {
            JumpToTarget(dy, dx);
            return;
        }

        // 3) Salto de acercamiento para cerrar distancia
        if (leapEnabled && Time.time >= nextLeapTime && Time.time >= nextJumpTime &&
            gap >= leapMinGap && gap <= leapMaxGap && Mathf.Abs(dy) < 1.2f &&
            Random.value < leapChancePerSecond * Time.fixedDeltaTime &&
            !WallAhead() && !LedgeAhead())
        {
            nextLeapTime = Time.time + leapCooldown;
            DoJump(leapHeight, leapSpeed, dx >= 0f ? 1 : -1);
            return;
        }

        // 4) Caminar
        if (gap > closeGap) MoveTowardPlayer(dxAbs, dy);
        else Decel();
    }

    void MoveTowardPlayer(float dxAbs, float dy)
    {
        if (dxAbs < turnDeadZone) { Decel(); return; }

        // Detector de atasco: si ordenó avanzar pero la velocidad real es ~0, hay algo bloqueándolo.
        if (Mathf.Abs(lastCmdX) > 1f && Mathf.Abs(Vel.x) < 0.3f) stuckTimer += Time.fixedDeltaTime;
        else stuckTimer = 0f;

        bool wall = WallAhead() || stuckTimer > 0.25f;
        bool ledge = !wall && LedgeAhead();

        if (wall)
        {
            if (Time.time >= nextJumpTime)
            {
                float needed = MeasureObstacleHeight() + jumpClearance;
                DoJump(Mathf.Max(obstacleJumpHeight, needed), MoveSpeedNow, facing);
                stuckTimer = 0f;
                lastCmdX = 0f;
                return;
            }
            Decel();
            return;
        }

        if (ledge && dy > -dropThreshold)
        {
            // Hay un hueco/borde y el jugador no está abajo: salta el hueco o se detiene
            if (Time.time >= nextJumpTime) { DoJump(gapJumpHeight, gapJumpSpeed, facing); return; }
            Decel();
            return;
        }

        SetVelX(Mathf.MoveTowards(Vel.x, facing * MoveSpeedNow, acceleration * Time.fixedDeltaTime));
        lastCmdX = Vel.x;
    }

    // Lanza un rayo hacia abajo desde arriba del obstáculo para encontrar su superficie superior.
    float MeasureObstacleHeight()
    {
        Bounds b = col.bounds;
        float x = b.center.x + facing * (b.extents.x + wallCheckDistance + 0.15f);
        Vector2 o = new Vector2(x, b.min.y + maxJumpHeight + 0.5f);
        hits.Clear();
        Physics2D.Raycast(o, Vector2.down, filter, hits, maxJumpHeight + 0.5f);
        RaycastHit2D h;
        if (FirstValid(out h)) return Mathf.Max(0f, h.point.y - b.min.y);
        // Si no se pudo medir (p. ej. el obstáculo es más alto que el rango del rayo), salta más alto por seguridad.
        return Mathf.Max(obstacleJumpHeight, maxJumpHeight * 0.6f);
    }

    void Decel()
    {
        SetVelX(Mathf.MoveTowards(Vel.x, 0f, deceleration * Time.fixedDeltaTime));
        lastCmdX = 0f;
    }

    void AirControl()
    {
        if (player == null) return;
        float dx = PlayerCenterX() - col.bounds.center.x;
        float dir = dx >= 0f ? 1f : -1f;
        float desired = dir * Mathf.Min(Mathf.Max(jumpHSpeed, MoveSpeedNow * 0.5f), Mathf.Abs(dx) * airHoming);
        SetVelX(Mathf.MoveTowards(Vel.x, desired, airAcceleration * Time.fixedDeltaTime));
    }

    void CheckLanding()
    {
        if (grounded && Time.time >= jumpStartTime + 0.12f) state = State.Chase;
        else if (Time.time > jumpStartTime + 4f) state = State.Chase; // seguro anti-atasco
    }

    // ============================================================== SALA
    bool hasCachedRoom;
    Bounds cachedRoom;

    // Lee los límites del collider una vez y lo desactiva: así no puede
    // interactuar con el jugador (ni con triggers ni con colisiones).
    void CacheRoomBounds()
    {
        if (roomCollider == null || hasCachedRoom) return;
        cachedRoom = roomCollider.bounds;
        hasCachedRoom = true;
        roomCollider.enabled = false;
    }

    Bounds RoomBounds
    {
        get
        {
            if (hasCachedRoom) return cachedRoom;
            if (roomCollider != null) return roomCollider.bounds;
            return new Bounds(new Vector3(roomCenter.x, roomCenter.y, 0f), new Vector3(roomSize.x, roomSize.y, 1000f));
        }
    }

    bool PlayerInRoom()
    {
        Bounds r = RoomBounds;
        Vector3 p = playerCol ? playerCol.bounds.center : player.position;
        p.z = r.center.z;
        return r.Contains(p);
    }

    void UpdateActivation()
    {
        bool inside = true;
        if (useRoomArea) inside = PlayerInRoom();
        if (inside && aggroRange > 0f)
            inside = Mathf.Abs(player.position.x - transform.position.x) <= aggroRange;

        if (inside && !active)
        {
            active = true;
            readyTime = Time.time + activationDelay;
            onActivated?.Invoke();
        }
        else if (!inside && active)
        {
            active = false;
            onPlayerLeft?.Invoke();
            if (resetHealthWhenPlayerLeaves)
            {
                currentHealth = maxHealth;
                phase2Triggered = false;
                UpdateHealthUI();
            }
        }
    }

    void ReturnHome()
    {
        float dx = homePos.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.2f) { Decel(); return; }

        int dir = dx > 0f ? 1 : -1;
        SetFacing(dir);
        if (WallAhead() || LedgeAhead()) { Decel(); return; }
        SetVelX(Mathf.MoveTowards(Vel.x, dir * moveSpeed * 0.7f, acceleration * Time.fixedDeltaTime));
    }

    void ClampToRoom()
    {
        if (!useRoomArea || !stayInsideRoom) return;
        Bounds r = RoomBounds;
        Bounds b = col.bounds;
        Vector2 v = Vel;
        if (b.max.x >= r.max.x - 0.05f && v.x > 0f) v.x = 0f;
        else if (b.min.x <= r.min.x + 0.05f && v.x < 0f) v.x = 0f;
        Vel = v;
    }

    // ============================================================ SALTOS
    void JumpToTarget(float dy, float dx)
    {
        float h = Mathf.Clamp(dy + jumpClearance, jumpClearance, maxJumpHeight);
        float g = GravityMagnitude();
        float vy = Mathf.Sqrt(2f * g * h);
        float disc = Mathf.Max(0f, vy * vy - 2f * g * dy);
        float t = (vy + Mathf.Sqrt(disc)) / g;
        float hs = Mathf.Clamp(Mathf.Abs(dx) / Mathf.Max(0.1f, t), 0f, airSpeedMax);
        DoJump(h, hs, dx >= 0f ? 1 : -1);
    }

    void DoJump(float height, float hSpeed, int dir)
    {
        height = Mathf.Min(height, maxJumpHeight);
        float vy = Mathf.Sqrt(2f * GravityMagnitude() * height);
        SetFacing(dir);
        jumpHSpeed = hSpeed;
        Vel = new Vector2(dir * hSpeed, vy);
        state = State.Jumping;
        jumpStartTime = Time.time;
        nextJumpTime = Time.time + jumpCooldown;
        Trigger(jumpTrigger);
        PlayClip(jumpSound);
    }

    // =========================================================== ATAQUES
    BossAttack SelectAttack(float gap, float dy)
    {
        candidates.Clear();
        float total = 0f;
        float hp = HealthPercent;

        foreach (var a in attacks)
        {
            if (a == null || !a.active) continue;
            if (Time.time < a.nextAvailableTime) continue;
            if (gap < a.minRange || gap > a.maxRange) continue;
            if (dy < a.minVertical || dy > a.maxVertical) continue;
            if (hp < a.minHealthPercent || hp > a.maxHealthPercent) continue;

            candidates.Add(a);
            total += EffectiveWeight(a);
        }
        if (candidates.Count == 0) return null;

        float roll = Random.value * total;
        foreach (var a in candidates)
        {
            roll -= EffectiveWeight(a);
            if (roll <= 0f) return a;
        }
        return candidates[candidates.Count - 1];
    }

    float EffectiveWeight(BossAttack a)
    {
        float w = Mathf.Max(0.01f, a.weight);
        return a == lastAttack ? w * repeatPenalty : w;
    }

    IEnumerator AttackRoutine(BossAttack a)
    {
        state = State.Attacking;
        currentAttack = a;
        lastAttack = a;
        a.nextAvailableTime = Time.time + a.cooldown * CooldownMult;

        FacePlayer(true);
        SetVelX(0f);
        Trigger(a.animatorTrigger);

        float total = Mathf.Max(a.totalDuration, a.windup + Mathf.Max(0, a.hits - 1) * a.timeBetweenHits + 0.05f);
        int hitsDone = 0;
        float t = 0f;

        while (t < total)
        {
            if (hitsDone < a.hits && t >= a.windup + hitsDone * a.timeBetweenHits)
            {
                PerformHit(a);
                hitsDone++;
            }

            bool lunge = a.lungeSpeed != 0f && t >= a.lungeStart && t < a.lungeStart + a.lungeDuration;
            if (lunge && !WallAhead() && !LedgeAhead()) SetVelX(facing * a.lungeSpeed);
            else SetVelX(0f);
            ClampToRoom();

            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        SetVelX(0f);
        if (a.punishable) punishUntil = Time.time + a.recovery;
        yield return new WaitForSeconds(a.recovery);

        nextAttackAllowedTime = Time.time + minTimeBetweenAttacks * CooldownMult;
        currentAttack = null;
        actionRoutine = null;
        state = State.Chase;
    }

    void PerformHit(BossAttack a)
    {
        Vector2 origin = HitOrigin(a, facing);
        Collider2D[] found;

        switch (a.shape)
        {
            case BossHitShape.Box: found = Physics2D.OverlapBoxAll(origin, a.size, 0f); break;
            default: found = Physics2D.OverlapCircleAll(origin, a.size.x); break;
        }

        bool anyHit = false;
        HashSet<GameObject> done = new HashSet<GameObject>();
        Vector2 fwd = ConeForward(a, facing);

        foreach (var c in found)
        {
            if (!IsPlayerCollider(c)) continue;

            if (a.shape == BossHitShape.Cone)
            {
                Vector2 closest = c.ClosestPoint(origin);
                Vector2 dir = closest - origin;
                if (dir.sqrMagnitude > 0.0001f && Vector2.Angle(fwd, dir) > a.coneAngle * 0.5f) continue;
            }

            GameObject target = PlayerRoot(c);
            if (!done.Add(target)) continue;

            DealDamage(target, a.damage, a.knockback);
            anyHit = true;
        }

        if (debugLogs && !anyHit)
            Debug.Log("[DarkKnightBoss] '" + a.name + "' no conectó (revisa que el jugador tenga el tag '" + playerTag + "' y que la hitbox lo alcance)", this);

        PlayClip(a.swingSound, a.volume);
        if (anyHit) PlayClip(a.hitSound, a.volume);
    }

    Vector2 HitOrigin(BossAttack a, int f)
    {
        return (Vector2)col.bounds.center + new Vector2(a.offset.x * f, a.offset.y);
    }

    static Vector2 ConeForward(BossAttack a, int f)
    {
        float ang = f > 0 ? a.coneTilt : 180f - a.coneTilt;
        float rad = ang * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    void DealDamage(GameObject target, int dmg, Vector2 knockback)
    {
        // Camino directo: PlayerController.TakeDamage(int, Transform). Usa el knockback e invencibilidad del jugador.
        PlayerController pc = target.GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            pc.TakeDamage(dmg, transform);
            if (debugLogs) Debug.Log("[DarkKnightBoss] Golpeó a '" + pc.name + "' con " + dmg + " de daño", this);
            onHitPlayer?.Invoke(target);
            return;
        }

        // Respaldo para otros scripts de jugador (SendMessage con un solo parámetro).
        if (damageAsFloat) target.SendMessageUpwards(damageMethodName, (float)dmg, SendMessageOptions.DontRequireReceiver);
        else target.SendMessageUpwards(damageMethodName, dmg, SendMessageOptions.DontRequireReceiver);
        if (debugLogs) Debug.Log("[DarkKnightBoss] Golpeó a '" + target.name + "' con " + dmg + " de daño (método: " + damageMethodName + ")", this);

        Rigidbody2D prb = target.GetComponentInParent<Rigidbody2D>();
        if (prb != null && knockback != Vector2.zero)
        {
            float dir = target.transform.position.x >= transform.position.x ? 1f : -1f;
            prb.AddForce(new Vector2(dir * knockback.x, knockback.y) * prb.mass, ForceMode2D.Impulse);
        }

        onHitPlayer?.Invoke(target);
    }

    void OnCollisionStay2D(Collision2D c)
    {
        if (state == State.Dead || contactDamage <= 0 || Time.time < nextContactTime) return;
        if (!IsPlayerCollider(c.collider)) return;
        nextContactTime = Time.time + contactInterval;
        DealDamage(PlayerRoot(c.collider), contactDamage, contactKnockback);
    }

    // ======================================================= RECIBIR DAÑO
    /// <summary>Llama esto desde el ataque del jugador: boss.TakeDamage(10);</summary>
    public void TakeDamage(int damage)
    {
        Vector2 src = player ? (Vector2)player.position : (Vector2)transform.position - Vector2.right * facing;
        TakeDamageFrom(damage, src);
    }

    // Nombre distinto a propósito: SendMessage no funciona bien con métodos sobrecargados.
    public void TakeDamageFrom(int damage, Vector2 sourcePosition)
    {
        if (debugLogs) Debug.Log("[DarkKnightBoss] TakeDamage recibido: " + damage + " (estado: " + state + ")", this);
        if (state == State.Dead || damage <= 0) return;
        if (Time.time < invulnerableUntil) return;

        // Parry activo: bloquea el golpe
        if (state == State.Parrying && parryActive)
        {
            parrySuccess = true;
            invulnerableUntil = Time.time + 0.1f;
            PlayClip(parrySound);
            onParry?.Invoke();
            return;
        }

        // Parry reactivo (con chance y cooldown para que no sea frustrante)
        if (TryReactiveParry())
        {
            parrySuccess = true;
            invulnerableUntil = Time.time + 0.1f;
            PlayClip(parrySound);
            onParry?.Invoke();
            return;
        }

        int dmg = damage;
        if (Time.time < punishUntil) dmg = Mathf.CeilToInt(damage * punishMultiplier);

        currentHealth = Mathf.Max(0, currentHealth - dmg);
        invulnerableUntil = Time.time + hitInvulnerability;
        UpdateHealthUI();
        onDamaged?.Invoke(currentHealth, maxHealth);
        PlayClip(hurtSound);
        Flash();

        if (currentHealth <= 0) { Die(); return; }

        if (!phase2Triggered && HealthPercent <= phase2HealthPercent)
        {
            phase2Triggered = true;
            onPhase2?.Invoke();
        }

        TryStagger(sourcePosition);
    }

    public void Heal(int amount)
    {
        if (state == State.Dead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthUI();
    }

    bool TryReactiveParry()
    {
        if (!canParry || state != State.Chase || !grounded) return false;
        if (Time.time < nextParryTime) return false;
        float chance = parryChance * (InPhase2 ? phase2ParryMultiplier : 1f);
        if (Random.value > chance) return false;

        StartAction(ParryRoutine());
        return true;
    }

    IEnumerator ParryRoutine()
    {
        state = State.Parrying;
        parryActive = true;
        parrySuccess = false;
        nextParryTime = Time.time + parryCooldown;
        FacePlayer(true);
        Trigger(parryTrigger);

        yield return new WaitForSeconds(parryDuration);
        parryActive = false;

        if (parrySuccess)
        {
            // Bloqueó un golpe: queda expuesto
            punishUntil = Time.time + parryPunishTime;
            yield return new WaitForSeconds(parryPunishTime);
        }

        actionRoutine = null;
        state = State.Chase;
    }

    void TryStagger(Vector2 source)
    {
        if (state == State.Parrying || state == State.Jumping || !grounded) return;
        if (currentAttack != null && currentAttack.superArmor) return;
        if (Time.time < nextStaggerTime) return;
        if (Random.value > staggerChance) return;

        nextStaggerTime = Time.time + staggerCooldown;
        currentAttack = null;
        float dir = transform.position.x >= source.x ? 1f : -1f;
        SetVelX(dir * staggerKnockbackSpeed);
        StartAction(StaggerRoutine());
    }

    IEnumerator StaggerRoutine()
    {
        state = State.Staggered;
        parryActive = false;
        Trigger(hurtTrigger);
        yield return new WaitForSeconds(staggerDuration);
        actionRoutine = null;
        state = State.Chase;
    }

    void StartAction(IEnumerator routine)
    {
        if (actionRoutine != null) StopCoroutine(actionRoutine);
        actionRoutine = StartCoroutine(routine);
    }

    // =============================================================== MUERTE
    [SerializeField] private BossDoor bossDoor;

    void Die()
    {
        state = State.Dead;
        StopAllCoroutines();
        actionRoutine = null;
        currentAttack = null;
        if (sr) sr.color = originalColor;
        if (barRoot) barRoot.gameObject.SetActive(false);
        if (screenFillImage) screenFillImage.fillAmount = 0f;

        Trigger(dieTrigger);
        PlayClip(deathSound);

        onDeath?.Invoke();
        onScoreAwarded?.Invoke(scoreValue);
        OnAnyBossScore?.Invoke(scoreValue);
        if (sendScoreToPlayer && player != null && !string.IsNullOrEmpty(scoreMethodName))
            player.SendMessage(scoreMethodName, scoreValue, SendMessageOptions.DontRequireReceiver);

        StartCoroutine(DeathRoutine());
        if (bossDoor != null)
    {
        bossDoor.Open();
    }
    }

    IEnumerator DeathRoutine()
    {
        float t = 0f;
        while (t < 2f)
        {
            UpdateGrounded();
            SetVelX(0f);
            if (grounded) break;
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        SetVel(rb, Vector2.zero);
        rb.simulated = false; // ya no bloquea al jugador
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }

    // ============================================================= ENTORNO
    void UpdateGrounded()
    {
        Bounds b = col.bounds;
        Vector2 size = new Vector2(b.size.x * 0.9f, 0.05f);
        Vector2 origin = new Vector2(b.center.x, b.min.y + 0.03f);

        hits.Clear();
        Physics2D.BoxCast(origin, size, 0f, Vector2.down, filter, hits, groundCheckDistance + 0.03f);
        RaycastHit2D h;
        bool hit = FirstValid(out h);

        bool was = grounded;
        grounded = hit && Vel.y <= 0.5f;

        if (!grounded) airTime += Time.fixedDeltaTime;
        else
        {
            if (!was && airTime > 0.2f) PlayClip(landSound);
            airTime = 0f;
        }
    }

    bool WallAhead()
    {
        Bounds b = col.bounds;
        Vector2 o = new Vector2(b.center.x + facing * (b.extents.x + 0.02f), b.min.y + stepHeight);
        hits.Clear();
        Physics2D.Raycast(o, Vector2.right * facing, filter, hits, wallCheckDistance);
        RaycastHit2D h;
        return FirstValid(out h);
    }

    bool LedgeAhead()
    {
        Bounds b = col.bounds;
        Vector2 o = new Vector2(b.center.x + facing * (b.extents.x + ledgeCheckForward), b.min.y + 0.15f);
        hits.Clear();
        Physics2D.Raycast(o, Vector2.down, filter, hits, ledgeCheckDepth);
        RaycastHit2D h;
        return !FirstValid(out h);
    }

    bool FirstValid(out RaycastHit2D result)
    {
        for (int i = 0; i < hits.Count; i++)
        {
            Collider2D c = hits[i].collider;
            if (c == null || c == col || c.isTrigger || IsPlayerCollider(c)) continue;
            result = hits[i];
            return true;
        }
        result = default(RaycastHit2D);
        return false;
    }

    // ============================================================= JUGADOR
    void FindPlayer()
    {
        GameObject go = null;
        try { go = GameObject.FindGameObjectWithTag(playerTag); } catch { }
        if (go == null) { player = null; return; }
        player = go.transform;
        playerCol = go.GetComponentInChildren<Collider2D>();
        playerRb = go.GetComponent<Rigidbody2D>();
    }

    float PlayerCenterX() { return playerCol ? playerCol.bounds.center.x : player.position.x; }
    float PlayerFeetY() { return playerCol ? playerCol.bounds.min.y : player.position.y; }

    float ComputeGap()
    {
        if (playerCol != null && playerCol.enabled)
            return Mathf.Max(0f, col.Distance(playerCol).distance);
        return Mathf.Max(0f, Mathf.Abs(PlayerCenterX() - col.bounds.center.x) - col.bounds.extents.x);
    }

    bool IsPlayerCollider(Collider2D c)
    {
        if (c == null) return false;
        if (c.CompareTag(playerTag)) return true;
        if (c.attachedRigidbody != null && c.attachedRigidbody.CompareTag(playerTag)) return true;
        return c.transform.root.CompareTag(playerTag);
    }

    GameObject PlayerRoot(Collider2D c)
    {
        return c.attachedRigidbody ? c.attachedRigidbody.gameObject : c.gameObject;
    }

    // ============================================================== FACING
    void FacePlayer(bool force = false)
    {
        if (player == null) return;
        float dx = PlayerCenterX() - col.bounds.center.x;
        if (force || Mathf.Abs(dx) > turnDeadZone) SetFacing(dx >= 0f ? 1 : -1);
    }

    void SetFacing(int f)
    {
        facing = f;
        transform.localScale = new Vector3(
            Mathf.Abs(baseScale.x) * f * (spriteFacesRight ? 1f : -1f),
            baseScale.y, baseScale.z);
    }

    // ============================================================= ANIMATOR
    void Trigger(string param)
    {
        if (anim == null || string.IsNullOrEmpty(param)) return;
        int hash = Animator.StringToHash(param);
        AnimatorControllerParameterType type;
        if (animParams.TryGetValue(hash, out type) && type == AnimatorControllerParameterType.Trigger)
            anim.SetTrigger(hash);
        else WarnMissing(param);
    }

    void UpdateAnimator()
    {
        if (anim == null) return;
        SetAnimFloat(speedFloat, Mathf.Abs(Vel.x));
        SetAnimFloat(verticalSpeedFloat, Vel.y);
        SetAnimBool(groundedBool, grounded);
    }

    void SetAnimFloat(string p, float v)
    {
        if (string.IsNullOrEmpty(p)) return;
        int h = Animator.StringToHash(p);
        AnimatorControllerParameterType t;
        if (animParams.TryGetValue(h, out t) && t == AnimatorControllerParameterType.Float) anim.SetFloat(h, v);
    }

    void SetAnimBool(string p, bool v)
    {
        if (string.IsNullOrEmpty(p)) return;
        int h = Animator.StringToHash(p);
        AnimatorControllerParameterType t;
        if (animParams.TryGetValue(h, out t) && t == AnimatorControllerParameterType.Bool) anim.SetBool(h, v);
    }

    void WarnMissing(string p)
    {
        if (!warnMissingParameters || warned.Contains(p)) return;
        warned.Add(p);
        Debug.LogWarning("[DarkKnightBoss] El Animator no tiene un Trigger llamado '" + p + "'.", this);
    }

    // ================================================================ AUDIO
    void PlayClip(AudioClip clip, float vol = 1f)
    {
        if (clip == null || audioSrc == null) return;
        audioSrc.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSrc.PlayOneShot(clip, vol * masterVolume);
    }

    void HandleFootsteps()
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;
        float speed = Mathf.Abs(Vel.x);
        if (!grounded || speed < 0.5f || state != State.Chase) { stepTimer = 0f; return; }

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            PlayClip(footstepSounds[Random.Range(0, footstepSounds.Length)], 0.6f);
            stepTimer = footstepInterval * (moveSpeed / Mathf.Max(0.5f, speed));
        }
    }

    // ============================================================ FEEDBACK
    void Flash()
    {
        if (sr == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        sr.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashTime);
        sr.color = originalColor;
        flashRoutine = null;
    }

    // ============================================================ BARRA VIDA
    void BuildHealthBar()
    {
        if (!showWorldHealthBar) return;

        barTex = new Texture2D(1, 1);
        barTex.SetPixel(0, 0, Color.white);
        barTex.Apply();
        barSprite = Sprite.Create(barTex, new Rect(0, 0, 1, 1), new Vector2(0f, 0.5f), 1f);

        // Se crea SIN padre para que no le afecte el flip ni la escala x4 del jefe
        barRoot = new GameObject(name + "_HealthBar").transform;

        CreateBarPiece("Frame", barFrameColor,
            new Vector2(barSize.x + barBorder * 2f, barSize.y + barBorder * 2f),
            -barSize.x * 0.5f - barBorder, barSortingOrder, false);
        CreateBarPiece("Back", barBackColor, barSize, -barSize.x * 0.5f, barSortingOrder + 1, false);
        barFill = CreateBarPiece("Fill", barHighColor, barSize, -barSize.x * 0.5f, barSortingOrder + 2, true);
    }

    Transform CreateBarPiece(string pieceName, Color color, Vector2 size, float leftX, int order, bool isFill)
    {
        GameObject go = new GameObject(pieceName);
        go.transform.SetParent(barRoot, false);
        SpriteRenderer r = go.AddComponent<SpriteRenderer>();
        r.sprite = barSprite;
        r.color = color;
        r.sortingOrder = order;
        go.transform.localPosition = new Vector3(leftX, 0f, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        if (isFill) barFillSr = r;
        return go.transform;
    }

    void UpdateHealthUI()
    {
        float pct = HealthPercent;
        if (barFill)
        {
            barFill.localScale = new Vector3(barSize.x * pct, barSize.y, 1f);
            if (barFillSr) barFillSr.color = Color.Lerp(barLowColor, barHighColor, pct);
        }
        if (screenFillImage) screenFillImage.fillAmount = pct;
    }

    // ===================================================== ATAQUES POR DEFECTO
    // Los triggers coinciden con tu Animator. Cada ataque tiene forma y rango distintos.
    static List<BossAttack> DefaultAttacks()
    {
        return new List<BossAttack>
        {
            new BossAttack {
                name = "Corte rápido", animatorTrigger = "Attack1",
                shape = BossHitShape.Box, offset = new Vector2(1.4f, 0.3f), size = new Vector2(2.2f, 1.8f),
                minRange = 0f, maxRange = 1.8f, weight = 3f, cooldown = 1.2f,
                windup = 0.3f, totalDuration = 0.7f, recovery = 0.25f,
                damage = 8, knockback = new Vector2(4f, 2f),
                lungeSpeed = 4f, lungeStart = 0.1f, lungeDuration = 0.15f
            },
            new BossAttack {
                name = "Estocada larga", animatorTrigger = "Attack2",
                shape = BossHitShape.Box, offset = new Vector2(2.2f, 0.2f), size = new Vector2(3.6f, 0.9f),
                minRange = 0.8f, maxRange = 3.2f, weight = 2f, cooldown = 3f,
                windup = 0.4f, totalDuration = 0.9f, recovery = 0.35f,
                damage = 10, knockback = new Vector2(6f, 1f),
                lungeSpeed = 9f, lungeStart = 0.25f, lungeDuration = 0.2f
            },
            new BossAttack {
                name = "Barrido 360", animatorTrigger = "Attack3",
                shape = BossHitShape.Circle, offset = new Vector2(0f, 0f), size = new Vector2(2.6f, 0f),
                minRange = 0f, maxRange = 2.2f, minVertical = -1.5f, maxVertical = 2.5f,
                weight = 1.5f, cooldown = 4f,
                windup = 0.4f, totalDuration = 0.9f, recovery = 0.4f,
                damage = 10, knockback = new Vector2(7f, 3f)
            },
            new BossAttack {
                name = "Tajo ascendente (anti-aire)", animatorTrigger = "Attack4",
                shape = BossHitShape.Cone, offset = new Vector2(0.6f, 0.8f), size = new Vector2(3.2f, 0f),
                coneAngle = 110f, coneTilt = 55f,
                minRange = 0f, maxRange = 2.5f, minVertical = 0.8f, maxVertical = 4f,
                weight = 4f, cooldown = 2.5f,
                windup = 0.35f, totalDuration = 0.8f, recovery = 0.3f,
                damage = 12, knockback = new Vector2(3f, 6f)
            },
            new BossAttack {
                name = "Golpe pesado", animatorTrigger = "HeavyAttack1",
                shape = BossHitShape.Box, offset = new Vector2(1.9f, 0.5f), size = new Vector2(3.4f, 2.8f),
                minRange = 0f, maxRange = 2.8f, weight = 1.5f, cooldown = 6f,
                windup = 0.7f, totalDuration = 1.2f, recovery = 0.9f, punishable = true, superArmor = true,
                damage = 22, knockback = new Vector2(10f, 5f),
                lungeSpeed = 6f, lungeStart = 0.4f, lungeDuration = 0.25f
            },
            new BossAttack {
                name = "Impacto en área (solo fase 2)", animatorTrigger = "HeavyAttack2",
                shape = BossHitShape.Circle, offset = new Vector2(1f, -0.3f), size = new Vector2(3.8f, 0f),
                minRange = 0f, maxRange = 3.2f, minVertical = -1.5f, maxVertical = 2f,
                maxHealthPercent = 0.6f, weight = 1.5f, cooldown = 9f,
                windup = 0.8f, totalDuration = 1.3f, recovery = 1f, punishable = true, superArmor = true,
                damage = 25, knockback = new Vector2(12f, 6f)
            }
        };
    }

    // ================================================================ GIZMOS
    // La sala se dibuja siempre en verde (aunque el jefe no esté seleccionado).
    void OnDrawGizmos()
    {
        if (!useRoomArea) return;
        Bounds r = RoomBounds;
        Vector3 c = new Vector3(r.center.x, r.center.y, 0f);
        Vector3 s = new Vector3(r.size.x, r.size.y, 0.01f);
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.10f);
        Gizmos.DrawCube(c, s);
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 1f);
        Gizmos.DrawWireCube(c, s);
    }

    // Selecciona el jefe en la escena para ver hitboxes, rangos y altura de salto.
    void OnDrawGizmosSelected()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null) return;
        Bounds b = c.bounds;
        int f = Application.isPlaying ? facing : (((transform.localScale.x >= 0f) == spriteFacesRight) ? 1 : -1);

        if (attacks != null)
        {
            foreach (var a in attacks)
            {
                if (a == null || !a.active) continue;
                Vector2 o = (Vector2)b.center + new Vector2(a.offset.x * f, a.offset.y);

                switch (a.shape)
                {
                    case BossHitShape.Box:
                        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
                        Gizmos.DrawWireCube(o, a.size);
                        break;
                    case BossHitShape.Circle:
                        Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.9f);
                        DrawWireCircle(o, a.size.x);
                        break;
                    case BossHitShape.Cone:
                        Gizmos.color = new Color(1f, 0.2f, 1f, 0.9f);
                        DrawWireCone(o, ConeForward(a, f), a.coneAngle, a.size.x);
                        break;
                }
            }
        }

        // Altura máxima de salto y alcance horizontal
        Gizmos.color = Color.cyan;
        Vector3 feet = new Vector3(b.center.x, b.min.y, 0f);
        Gizmos.DrawLine(feet, feet + Vector3.up * maxJumpHeight);
        Gizmos.DrawLine(feet + Vector3.up * maxJumpHeight + Vector3.left * 0.5f, feet + Vector3.up * maxJumpHeight + Vector3.right * 0.5f);
        Gizmos.DrawLine(feet + Vector3.left * jumpMaxHorizontalDistance, feet + Vector3.right * jumpMaxHorizontalDistance);
    }

    static void DrawWireCircle(Vector2 center, float radius)
    {
        const int seg = 24;
        Vector3 prev = center + new Vector2(radius, 0f);
        for (int i = 1; i <= seg; i++)
        {
            float a = i * Mathf.PI * 2f / seg;
            Vector3 next = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    static void DrawWireCone(Vector2 origin, Vector2 forward, float angle, float radius)
    {
        float baseAng = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
        float start = baseAng - angle * 0.5f;
        const int seg = 12;
        Vector3 prev = origin + Dir(start) * radius;
        Gizmos.DrawLine(origin, prev);
        for (int i = 1; i <= seg; i++)
        {
            Vector3 next = origin + Dir(start + angle * i / seg) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
        Gizmos.DrawLine(origin, prev);
    }

    static Vector2 Dir(float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
    }
}