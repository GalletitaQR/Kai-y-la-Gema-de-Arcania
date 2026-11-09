using System.Collections;
using UnityEngine;

/// <summary>
/// Jefe Final del Nivel 4 ("Templo de Arcania"): Dragón Guardián (DragonRed).
/// Acompañado por el Mage Esqueleto (SkeletonMageCompanion).
/// Tiene 3 fases cíclicas usando únicamente los controles actuales de Kai:
/// 1. Ataque de fuego: ráfagas de bolas de fuego con aberturas para esquivar y acercarse.
/// 2. Embestida / ataque desde el aire: vuelo rasante de extremo a extremo de la Sala del Trono; Kai debe saltar o subir a las plataformas.
/// 3. Momento vulnerable: el dragón queda expuesto unos segundos (solo si Kai participa en el combate y no lo ignora).
/// </summary>
public class DragonGuardianBoss : MonoBehaviour, IEnemigo
{
    public enum BossPhase
    {
        WaitingForPlayer,
        Phase1_FireBreath,
        Phase2_AerialCharge,
        Phase3_Vulnerable,
        Defeated
    }

    [Header("Estado y Referencias del Jefe")]
    public BossPhase currentPhase = BossPhase.WaitingForPlayer;
    public SkeletonMageCompanion companionMage;
    public BossHealthBarUI bossHealthUI;
    public BarraVida barraVidaOpcional;
    public GameObject gemObjectToActivate;

    [Header("Activación y Arena (Sala del Trono)")]
    public float bossRoomTriggerX = 114.2f;
    public float arenaLeftX = 118.2f;
    public float arenaRightX = 132.8f;
    public float groundY = -0.85f;
    public float airHoverY = 1.65f;

    [Header("Fase 1: Ataque de Fuego")]
    public GameObject fireballPrefab;
    public Sprite fallbackFireballSprite;
    public Transform fireBreathPoint;
    public int fireVolleys = 3;
    public float timeBetweenVolleys = 1.25f;
    public float fireballSpeed = 7.6f;
    public int fireballDamage = 18;

    [Header("Fase 2: Embestida / Ataque desde el Aire")]
    public float chargeSpeed = 9.5f;
    public int chargeDamage = 22;
    public int chargePasses = 2;

    [Header("Fase 3: Momento Vulnerable")]
    public float vulnerableDuration = 4.2f;
    public float vulnerableDurationWhenMageDead = 5.5f;

    [Header("Vida y Daño")]
    public int maxHealth = 350;
    public int currentHealth = 350;
    public int scoreValue = 500;

    private Transform player;
    private Animator animator;
    private Rigidbody2D rb;
    private Collider2D bossCollider;
    private SpriteRenderer[] childRenderers;

    private float baseScaleX = 0.36f;
    private bool combatStarted = false;
    private bool isVulnerable = false;
    private bool isDead = false;
    private float lastContactDamageTime = -10f;
    private float lastPlayerEngagedTime = -10f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        bossCollider = GetComponent<Collider2D>();
        childRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        baseScaleX = Mathf.Abs(transform.localScale.x);
        if (baseScaleX < 0.01f) baseScaleX = 0.36f;

        currentHealth = maxHealth;
        if (barraVidaOpcional != null) barraVidaOpcional.Actualizar(currentHealth, maxHealth);

        if (gemObjectToActivate != null)
        {
            gemObjectToActivate.SetActive(false);
        }
    }

    void Update()
    {
        if (isDead) return;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            if (player == null) return;
        }

        // Registrar si Kai está participando activamente dentro de la Sala del Trono
        if (player.position.x >= bossRoomTriggerX - 1f && Vector2.Distance(transform.position, player.position) <= 18f)
        {
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.05f ||
                Input.GetButtonDown("Jump") ||
                Input.GetKeyDown(KeyCode.X) ||
                Input.GetKeyDown(KeyCode.Z) ||
                Input.GetKeyDown(KeyCode.J))
            {
                lastPlayerEngagedTime = Time.time;
            }
        }

        if (!combatStarted && player.position.x >= bossRoomTriggerX)
        {
            StartBossFight();
        }
    }

    private void StartBossFight()
    {
        combatStarted = true;
        lastPlayerEngagedTime = Time.time;

        if (companionMage != null)
        {
            companionMage.ActivateCombat();
        }

        if (bossHealthUI == null)
        {
            bossHealthUI = FindFirstObjectByType<BossHealthBarUI>();
            if (bossHealthUI == null)
            {
                GameObject uiGo = new GameObject("BossHealthBarManager");
                bossHealthUI = uiGo.AddComponent<BossHealthBarUI>();
            }
        }

        bossHealthUI.SetVisible(true);
        UpdateBossUI("¡El Dragón Guardián despierta!");

        StartCoroutine(BossCycleRoutine());
    }

    private IEnumerator BossCycleRoutine()
    {
        yield return new WaitForSeconds(0.8f);

        while (!isDead)
        {
            // ==================================================================
            // FASE 1: ATAQUE DE FUEGO (Kai esquiva y busca una abertura)
            // ==================================================================
            currentPhase = BossPhase.Phase1_FireBreath;
            isVulnerable = false;
            SetBossTint(Color.white);
            UpdateBossUI("Fase 1: Aliento de Fuego — ¡Esquiva las llamas y busca una abertura!");

            for (int v = 0; v < fireVolleys && !isDead; v++)
            {
                if (player != null)
                {
                    FaceDirection(player.position.x - transform.position.x);
                }

                if (animator != null)
                {
                    animator.SetTrigger(v % 2 == 0 ? "SpecialATrigger" : "AttackTrigger");
                }

                yield return new WaitForSeconds(0.55f);

                if (!isDead && player != null)
                {
                    FireDragonBreathVolley(v);
                }

                // Pequeña abertura entre ráfagas para que Kai pueda esquivar o golpear
                yield return new WaitForSeconds(timeBetweenVolleys);
            }

            if (isDead) yield break;

            // ==================================================================
            // FASE 2: EMBESTIDA O ATAQUE DESDE EL AIRE (Saltar o cambiar posición)
            // ==================================================================
            currentPhase = BossPhase.Phase2_AerialCharge;
            isVulnerable = false;
            UpdateBossUI("Fase 2: Embestida Aérea — ¡Salta o sube a las plataformas!");

            if (animator != null) animator.SetTrigger("JumpTrigger");

            // Elevarse al aire antes de embestir
            float liftTime = 0.45f;
            float elapsed = 0f;
            Vector3 startLift = transform.position;
            Vector3 targetLift = new Vector3(transform.position.x, airHoverY, 0f);
            while (elapsed < liftTime && !isDead)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startLift, targetLift, elapsed / liftTime);
                yield return null;
            }

            for (int p = 0; p < chargePasses && !isDead; p++)
            {
                float targetX = Mathf.Abs(transform.position.x - arenaRightX) < Mathf.Abs(transform.position.x - arenaLeftX)
                    ? arenaLeftX
                    : arenaRightX;

                // Embestida rasante a la altura de Kai para obligar a saltar
                float swoopY = (p % 2 == 0) ? groundY + 0.15f : groundY + 0.85f;
                FaceDirection(targetX - transform.position.x);

                if (animator != null) animator.SetTrigger("MoveTrigger");
                MagicFX.Burst(transform.position + Vector3.up * 0.8f, new Color(1f, 0.4f, 0.1f, 0.9f), 14, 0.3f);

                yield return new WaitForSeconds(0.32f);

                while (!isDead && Mathf.Abs(transform.position.x - targetX) > 0.25f)
                {
                    float step = chargeSpeed * Time.deltaTime;
                    float newX = Mathf.MoveTowards(transform.position.x, targetX, step);
                    float newY = Mathf.MoveTowards(transform.position.y, swoopY, step * 0.6f);
                    transform.position = new Vector3(newX, newY, 0f);
                    yield return null;
                }

                if (animator != null) animator.SetTrigger("AttackTrigger");
                yield return new WaitForSeconds(0.45f);
            }

            // Aterrizar en la arena
            Vector3 landPos = new Vector3(transform.position.x, groundY, 0f);
            transform.position = landPos;

            if (isDead) yield break;

            // ==================================================================
            // FASE 3: MOMENTO VULNERABLE (Solo si el combate NO se ignora)
            // ==================================================================
            bool playerInsideArena = player != null && player.position.x >= bossRoomTriggerX - 1f;
            bool combatNotIgnored = playerInsideArena && (Time.time - lastPlayerEngagedTime <= 8.0f);

            if (!combatNotIgnored)
            {
                UpdateBossUI("¡No ignores el combate! Acércate y enfrenta al Dragón y al Mage.");
                yield return new WaitForSeconds(1.0f);
                continue;
            }

            currentPhase = BossPhase.Phase3_Vulnerable;
            isVulnerable = true;

            if (animator != null)
            {
                animator.ResetTrigger("MoveTrigger");
                animator.SetTrigger("StunedTrigger");
            }

            bool mageDefeated = companionMage == null || companionMage.IsDead;
            float vulnTime = mageDefeated ? vulnerableDurationWhenMageDead : vulnerableDuration;

            string mageHint = mageDefeated
                ? "¡MAGE DERROTADO! ¡El Dragón está totalmente expuesto, ATACA AHORA!"
                : "¡DRAGÓN VULNERABLE! ¡Atácalo ahora (cuidado con los hechizos del Mage)!";
            UpdateBossUI(mageHint);

            float vTimer = 0f;
            while (vTimer < vulnTime && !isDead)
            {
                vTimer += Time.deltaTime;
                float pulse = 0.55f + 0.45f * Mathf.Sin(vTimer * 10f);
                SetBossTint(Color.Lerp(Color.white, new Color(0.35f, 0.95f, 1f, 1f), pulse));

                if (animator != null && vTimer > 1.6f && Mathf.Repeat(vTimer, 1.5f) < Time.deltaTime)
                {
                    animator.SetTrigger("StunedTrigger");
                }
                yield return null;
            }

            isVulnerable = false;
            SetBossTint(Color.white);
        }
    }

    private void FireDragonBreathVolley(int volleyIndex)
    {
        Vector3 spawnPos = fireBreathPoint != null
            ? fireBreathPoint.position
            : transform.position + new Vector3(transform.localScale.x > 0 ? -1.1f : 1.1f, 1.15f, 0f);

        Vector3 targetPos = player.position + new Vector3(0f, 0.45f, 0f);
        Vector2 baseDir = (targetPos - spawnPos).normalized;
        if (baseDir.sqrMagnitude < 0.001f) baseDir = Vector2.left;

        // Dispara proyectil central y otro ligeramente angulado para que Kai deba saltar o moverse
        float[] angles = (volleyIndex % 2 == 0) ? new float[] { -8f, 8f } : new float[] { 0f, -14f, 14f };
        Color fireColor = new Color(1f, 0.38f, 0.08f, 0.95f);

        MagicFX.Burst(spawnPos, fireColor, 14, 0.3f);

        for (int i = 0; i < angles.Length; i++)
        {
            Vector2 dir = Quaternion.Euler(0f, 0f, angles[i]) * baseDir;
            GameObject fb = fireballPrefab != null
                ? Instantiate(fireballPrefab, spawnPos, Quaternion.identity)
                : CreateFallbackFireball(spawnPos);

            BossProjectile proj = fb.GetComponent<BossProjectile>();
            if (proj == null) proj = fb.AddComponent<BossProjectile>();
            proj.Initialize(dir, fireballSpeed, fireballDamage, fireColor);
        }
    }

    private GameObject CreateFallbackFireball(Vector3 spawnPos)
    {
        GameObject go = new GameObject("BolaFuegoDragon_Runtime");
        go.transform.position = spawnPos;
        go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = fallbackFireballSprite;
        sr.color = new Color(1f, 0.38f, 0.08f, 0.95f);
        sr.sortingOrder = 16;

        Rigidbody2D projRb = go.AddComponent<Rigidbody2D>();
        projRb.bodyType = RigidbodyType2D.Kinematic;
        projRb.gravityScale = 0f;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.32f;

        return go;
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

    private void SetBossTint(Color tint)
    {
        if (childRenderers == null) return;
        for (int i = 0; i < childRenderers.Length; i++)
        {
            SpriteRenderer sr = childRenderers[i];
            if (sr == null || sr.gameObject == gameObject || sr.sprite == null) continue;
            Color c = tint;
            c.a = sr.color.a;
            sr.color = c;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead || collision == null || collision.collider == null) return;
        if (!collision.collider.CompareTag("Player")) return;

        if (Time.time - lastContactDamageTime >= 0.85f)
        {
            PlayerController pc = collision.collider.GetComponent<PlayerController>();
            if (pc != null)
            {
                int dmg = currentPhase == BossPhase.Phase2_AerialCharge ? chargeDamage : 15;
                pc.TakeDamage(dmg, transform);
                lastContactDamageTime = Time.time;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isDead || other == null || !other.CompareTag("Player")) return;

        if (Time.time - lastContactDamageTime >= 0.85f)
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc == null) pc = other.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                int dmg = currentPhase == BossPhase.Phase2_AerialCharge ? chargeDamage : 15;
                pc.TakeDamage(dmg, transform);
                lastContactDamageTime = Time.time;
            }
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        if (!combatStarted)
        {
            StartBossFight();
        }

        lastPlayerEngagedTime = Time.time;

        // Durante el Momento Vulnerable recibe el 100% del daño; fuera de él sus escamas mitigan el golpe
        int finalDamage = isVulnerable ? amount : Mathf.Max(5, Mathf.RoundToInt(amount * 0.25f));
        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(0, currentHealth);

        if (barraVidaOpcional != null) barraVidaOpcional.Actualizar(currentHealth, maxHealth);
        UpdateBossUI(null);

        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.enemyHit : null);
        MagicFX.Burst(transform.position + Vector3.up * 1.1f,
            isVulnerable ? new Color(0.3f, 0.95f, 1f, 0.95f) : new Color(1f, 0.5f, 0.2f, 0.8f),
            12, 0.28f);

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (isVulnerable && animator != null)
        {
            animator.SetTrigger("StunedTrigger");
        }
    }

    private void UpdateBossUI(string phaseText)
    {
        if (bossHealthUI != null)
        {
            bossHealthUI.UpdateBossHealth(currentHealth, maxHealth, isVulnerable, phaseText);
        }
    }

    private void Die()
    {
        isDead = true;
        currentPhase = BossPhase.Defeated;
        StopAllCoroutines();
        SetBossTint(Color.white);

        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.enemyDeath : null);
        if (GameManager.Instance != null) GameManager.Instance.AddScore(scoreValue);

        if (bossCollider != null) bossCollider.enabled = false;
        if (barraVidaOpcional != null) barraVidaOpcional.gameObject.SetActive(false);

        if (companionMage != null && !companionMage.IsDead)
        {
            companionMage.ForceDefeatByBossDeath();
        }

        if (bossHealthUI != null)
        {
            bossHealthUI.UpdateBossHealth(0, maxHealth, false, "¡DRAGÓN GUARDIÁN DERROTADO!");
            StartCoroutine(HideBossUIAfterDelay());
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        if (animator != null)
        {
            animator.ResetTrigger("MoveTrigger");
            animator.ResetTrigger("AttackTrigger");
            animator.ResetTrigger("SpecialATrigger");
            animator.ResetTrigger("JumpTrigger");
            animator.ResetTrigger("StunedTrigger");
            animator.SetTrigger("DeathTrigger");
        }

        MagicFX.Burst(transform.position + Vector3.up * 1.2f, new Color(1f, 0.85f, 0.3f, 1f), 28, 0.55f);

        // Activar la Gema de Arcania en la Sala del Trono al derrotar al jefe
        if (gemObjectToActivate != null)
        {
            gemObjectToActivate.SetActive(true);
        }

        Destroy(gameObject, 2.2f);
    }

    private IEnumerator HideBossUIAfterDelay()
    {
        yield return new WaitForSeconds(1.8f);
        if (bossHealthUI != null)
        {
            bossHealthUI.SetVisible(false);
        }
    }
}

