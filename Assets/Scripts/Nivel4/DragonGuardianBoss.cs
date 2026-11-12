using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Jefe Final del Nivel 4 ("Templo de Arcania"): Dragón Guardián (DragonRed).
/// Acompañado por el Mage Esqueleto (SkeletonMageCompanion).
/// El dragón SIEMPRE permanece en la arena atacando a Kai. 3 fases cíclicas:
/// 1. Ataque de fuego: ráfagas de bolas de fuego mientras se acerca; Kai esquiva y busca una abertura.
/// 2. Embestida por el suelo o salto aplastante: Kai debe SALTAR (el golpe solo daña a ras de suelo).
/// 3. Momento vulnerable: el dragón queda aturdido unos segundos y recibe daño extra
///    (solo si Kai participa en el combate y no lo ignora).
/// Durante la pelea aparecen Cristales de Poder Arcano que suben el daño de Kai.
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
    public float slamJumpHeight = 2.2f;

    [Header("Fase 1: Ataque de Fuego")]
    public GameObject fireballPrefab;
    public Sprite fallbackFireballSprite;
    public Transform fireBreathPoint;
    public int fireVolleys = 3;
    public float timeBetweenVolleys = 0.9f;
    public float fireballSpeed = 7.6f;
    public int fireballDamage = 15;
    public float walkSpeed = 1.6f;
    public float preferredDistance = 3.2f;

    [Header("Fase 2: Embestida / Salto Aplastante (saltar para esquivar)")]
    public float chargeSpeed = 9.5f;
    public int chargeDamage = 20;
    public float slamRadius = 3.0f;

    [Header("Fase 3: Momento Vulnerable")]
    public float vulnerableDuration = 4.5f;
    public float vulnerableDurationWhenMageDead = 6.0f;
    [Tooltip("Daño recibido fuera del momento vulnerable (0.5 = 50%).")]
    public float armoredDamageMultiplier = 0.2f;
    [Tooltip("Daño recibido durante el momento vulnerable (1.5 = 150%).")]
    public float vulnerableDamageMultiplier = 0.5f;

    [Header("Cristales de Poder Arcano (aumentan el daño de Kai)")]
    public Sprite powerCrystalSprite;
    public int crystalDamageBonus = 10;
    public int maxActiveCrystals = 2;
    public int maxTotalCrystals = 6;
    public Vector3[] crystalSpawnPoints = new Vector3[]
    {
        new Vector3(120.5f, -0.55f, 0f),
        new Vector3(125.5f, -0.55f, 0f),
        new Vector3(119.5f, 1.75f, 0f)
    };

    [Header("Vida y Daño")]
    public int maxHealth = 450;
    public int currentHealth = 300;
    public int contactDamage = 10;
    public int scoreValue = 500;

    private Transform player;
    private PlayerController playerController;
    private Collider2D playerCollider;
    private Animator animator;
    private Rigidbody2D rb;
    private Collider2D bossCollider;
    private SpriteRenderer[] childRenderers;

    private float baseScaleX = 0.38f;
    private bool combatStarted = false;
    private bool isVulnerable = false;
    private bool isDead = false;
    private bool lowHitOnly = false;   // durante la embestida solo daña a ras de suelo (Kai salta para esquivar)
    private float lastContactDamageTime = -10f;
    private float lastPlayerEngagedTime = -10f;
    private int crystalsSpawned = 0;
    private int crystalSpawnIndex = 0;
    private Vector3 initialPosition;
    private readonly List<ArcanePowerCrystal> activeCrystals = new List<ArcanePowerCrystal>();
    private static readonly List<DragonGuardianBoss> Instances = new List<DragonGuardianBoss>();

    void Awake()
    {
        if (!Instances.Contains(this)) Instances.Add(this);
        initialPosition = transform.position;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        bossCollider = GetComponent<Collider2D>();
        childRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        SortingGroup sg = GetComponent<SortingGroup>();
        if (sg == null) sg = gameObject.AddComponent<SortingGroup>();
        sg.sortingLayerName = "Enemies";
        sg.sortingOrder = 8;

        baseScaleX = Mathf.Abs(transform.localScale.x);
        if (baseScaleX < 0.01f) baseScaleX = 0.38f;

        currentHealth = maxHealth;
        if (barraVidaOpcional != null) barraVidaOpcional.Actualizar(currentHealth, maxHealth);

        if (gemObjectToActivate != null)
            gemObjectToActivate.SetActive(false);
    }

    void Update()
    {
        if (isDead) return;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerController = playerObj.GetComponentInParent<PlayerController>();
                if (playerController == null) playerController = playerObj.GetComponent<PlayerController>();
                playerCollider = playerObj.GetComponent<Collider2D>();
            }
            if (player == null) return;
        }

        // Registrar si Kai participa activamente dentro de la Sala del Trono
        if (player.position.x >= bossRoomTriggerX - 1f)
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
            StartBossFight();

        if (combatStarted && !isVulnerable && PlayerAvailable)
            CheckBossContactDamage();

        activeCrystals.RemoveAll(c => c == null);
    }

    private void CheckBossContactDamage()
    {
        if (Time.time - lastContactDamageTime < 0.9f || playerController == null) return;

        bool touching;
        if (bossCollider != null && bossCollider.enabled && playerCollider != null)
        {
            Bounds bossBounds = bossCollider.bounds;
            bossBounds.Expand(-0.15f);
            touching = bossBounds.Intersects(playerCollider.bounds);
        }
        else
        {
            touching = Mathf.Abs(player.position.x - transform.position.x) <= 1.25f &&
                       Mathf.Abs(player.position.y - transform.position.y) <= 1.6f;
        }

        if (!touching) return;

        // Durante la embestida solo golpea a ras de suelo: si Kai salta, la esquiva
        if (lowHitOnly && player.position.y > groundY + 0.65f) return;

        int dmg = lowHitOnly ? chargeDamage : contactDamage;
        playerController.TakeDamage(dmg, transform);
        lastContactDamageTime = Time.time;
    }

    private bool PlayerAvailable =>
        player != null && (playerController == null || !playerController.IsDead) &&
        (GameManager.Instance == null || GameManager.Instance.State == GameState.Playing);

    private void StartBossFight()
    {
        combatStarted = true;
        lastPlayerEngagedTime = Time.time;

        if (companionMage != null) companionMage.ActivateCombat();

        if (bossHealthUI == null)
        {
            bossHealthUI = FindFirstObjectByType<BossHealthBarUI>();
            if (bossHealthUI == null)
                bossHealthUI = new GameObject("BossHealthBarManager").AddComponent<BossHealthBarUI>();
        }

        bossHealthUI.SetVisible(true);
        UpdateBossUI("¡El Dragón Guardián despierta! Toma los Cristales de Poder para dañarlo más.");

        // Dos cristales de poder al iniciar la pelea
        SpawnPowerCrystal();
        SpawnPowerCrystal();

        StartCoroutine(BossCycleRoutine());
    }

    private IEnumerator BossCycleRoutine()
    {
        yield return new WaitForSeconds(0.8f);
        int cycle = 0;

        while (!isDead)
        {
            // Si Kai está muerto / en Game Over, el dragón espera en su sitio
            while (!isDead && !PlayerAvailable)
            {
                isVulnerable = false;
                lowHitOnly = false;
                SetBossTint(Color.white);
                yield return null;
            }
            if (isDead) yield break;

            // ==================== FASE 1: ATAQUE DE FUEGO ====================
            currentPhase = BossPhase.Phase1_FireBreath;
            isVulnerable = false;
            lowHitOnly = false;
            SetBossTint(Color.white);
            UpdateBossUI("Fase 1: Aliento de Fuego — ¡Esquiva y golpea entre ráfagas!");

            for (int v = 0; v < fireVolleys && !isDead; v++)
            {
                // Se acerca a Kai entre ráfagas (no se aleja de la pelea)
                float walkTime = 0.6f;
                while (walkTime > 0f && !isDead && player != null)
                {
                    walkTime -= Time.deltaTime;
                    float dx = player.position.x - transform.position.x;
                    FaceDirection(dx);
                    if (Mathf.Abs(dx) > preferredDistance)
                        MoveTo(transform.position.x + Mathf.Sign(dx) * walkSpeed * Time.deltaTime, groundY);
                    yield return null;
                }

                if (animator != null) animator.SetTrigger(v % 2 == 0 ? "SpecialATrigger" : "AttackTrigger");
                yield return new WaitForSeconds(0.45f);

                if (!isDead && PlayerAvailable) FireDragonBreathVolley(v);
                yield return new WaitForSeconds(timeBetweenVolleys);
            }
            if (isDead) yield break;

            // ============ FASE 2: EMBESTIDA o SALTO APLASTANTE (saltar) ============
            currentPhase = BossPhase.Phase2_AerialCharge;
            if (cycle % 2 == 0)
                yield return GroundChargeRoutine();
            else
                yield return SlamRoutine();
            lowHitOnly = false;
            if (isDead) yield break;

            // Reponer cristales de poder si Kai ya los tomó
            if (activeCrystals.Count < maxActiveCrystals) SpawnPowerCrystal();

            // ==================== FASE 3: MOMENTO VULNERABLE ====================
            bool playerInsideArena = player != null && player.position.x >= bossRoomTriggerX - 1f;
            bool combatNotIgnored = playerInsideArena && (Time.time - lastPlayerEngagedTime <= 8.0f);

            if (!combatNotIgnored)
            {
                UpdateBossUI("¡No ignores el combate! Enfrenta al Dragón y al Mage.");
                yield return new WaitForSeconds(0.8f);
                cycle++;
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
            UpdateBossUI(mageDefeated
                ? "¡MAGE DERROTADO! ¡El Dragón está totalmente expuesto, ATACA AHORA!"
                : "¡DRAGÓN VULNERABLE! ¡Atácalo ahora (cuidado con los hechizos del Mage)!");

            float vTimer = 0f;
            while (vTimer < vulnTime && !isDead)
            {
                vTimer += Time.deltaTime;
                float pulse = 0.55f + 0.45f * Mathf.Sin(vTimer * 10f);
                SetBossTint(Color.Lerp(Color.white, new Color(0.35f, 0.95f, 1f, 1f), pulse));
                yield return null;
            }

            isVulnerable = false;
            SetBossTint(Color.white);
            cycle++;
        }
    }

    /// <summary>Embestida rasante por el suelo hacia Kai. Solo daña a ras de suelo: hay que saltar.</summary>
    private IEnumerator GroundChargeRoutine()
    {
        UpdateBossUI("Fase 2: ¡EMBESTIDA! — ¡SALTA para esquivarla!");
        float targetX = player != null && player.position.x < transform.position.x ? arenaLeftX : arenaRightX;
        FaceDirection(targetX - transform.position.x);

        // Aviso: se pone rojo antes de embestir
        float warn = 0.7f;
        while (warn > 0f && !isDead)
        {
            warn -= Time.deltaTime;
            SetBossTint(Color.Lerp(Color.white, new Color(1f, 0.35f, 0.25f, 1f), Mathf.PingPong(warn * 6f, 1f)));
            yield return null;
        }
        SetBossTint(Color.white);

        if (animator != null) animator.SetTrigger("MoveTrigger");
        lowHitOnly = true;

        while (!isDead && Mathf.Abs(transform.position.x - targetX) > 0.2f)
        {
            MoveTo(Mathf.MoveTowards(transform.position.x, targetX, chargeSpeed * Time.deltaTime), groundY);
            if (Time.frameCount % 6 == 0)
                MagicFX.Burst(transform.position + Vector3.up * 0.3f, new Color(1f, 0.45f, 0.15f, 0.8f), 4, 0.2f);
            yield return null;
        }

        lowHitOnly = false;
        if (animator != null) animator.SetTrigger("AttackTrigger");
        if (player != null) FaceDirection(player.position.x - transform.position.x);
        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>Salto corto y aplastante sobre la posición de Kai; la onda daña a ras de suelo.</summary>
    private IEnumerator SlamRoutine()
    {
        UpdateBossUI("Fase 2: ¡ATAQUE DESDE EL AIRE! — ¡SALTA cuando aterrice!");
        if (animator != null) animator.SetTrigger("JumpTrigger");

        float startX = transform.position.x;
        float targetX = player != null ? Mathf.Clamp(player.position.x, arenaLeftX, arenaRightX) : startX;
        FaceDirection(targetX - startX);

        float duration = 0.9f;
        float t = 0f;
        while (t < duration && !isDead)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float y = groundY + Mathf.Sin(k * Mathf.PI) * slamJumpHeight;
            MoveTo(Mathf.Lerp(startX, targetX, k), y);
            yield return null;
        }
        MoveTo(targetX, groundY);
        if (isDead) yield break;

        // Onda de choque al aterrizar: daña solo si Kai está en el suelo cerca
        Vector3 impact = transform.position + Vector3.up * 0.2f;
        MagicFX.SpawnShockwave(impact, new Color(1f, 0.55f, 0.2f, 0.9f), slamRadius, 0.35f);
        MagicFX.Burst(impact, new Color(1f, 0.5f, 0.15f, 0.9f), 30, 0.5f);
        if (animator != null) animator.SetTrigger("AttackTrigger");

        if (PlayerAvailable && playerController != null &&
            Mathf.Abs(player.position.x - transform.position.x) <= slamRadius &&
            player.position.y < groundY + 0.5f)
        {
            playerController.TakeDamage(chargeDamage, transform);
            lastContactDamageTime = Time.time;
        }

        yield return new WaitForSeconds(0.6f);
    }

    private void MoveTo(float x, float y)
    {
        x = Mathf.Clamp(x, arenaLeftX, arenaRightX);
        Vector2 target = new Vector2(x, y);
        if (rb != null) rb.MovePosition(target);
        else transform.position = new Vector3(x, y, 0f);
    }

    private void SpawnPowerCrystal()
    {
        if (crystalsSpawned >= maxTotalCrystals || crystalSpawnPoints == null || crystalSpawnPoints.Length == 0) return;

        Vector3 pos = crystalSpawnPoints[crystalSpawnIndex % crystalSpawnPoints.Length];
        crystalSpawnIndex++;
        crystalsSpawned++;
        activeCrystals.Add(ArcanePowerCrystal.Spawn(pos, powerCrystalSprite, crystalDamageBonus));
    }

    private void FireDragonBreathVolley(int volleyIndex)
    {
        Vector3 spawnPos = fireBreathPoint != null
            ? fireBreathPoint.position
            : transform.position + new Vector3(transform.localScale.x > 0 ? -1.1f : 1.1f, 1.15f, 0f);

        Vector3 targetPos = player.position + new Vector3(0f, 0.45f, 0f);
        Vector2 baseDir = (targetPos - spawnPos).normalized;
        if (baseDir.sqrMagnitude < 0.001f) baseDir = Vector2.left;

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
        Vector3 s = transform.localScale;
        if (dirX < -0.02f) s.x = baseScaleX;
        else if (dirX > 0.02f) s.x = -baseScaleX;
        transform.localScale = s;
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

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isDead || !combatStarted || isVulnerable || other == null || !other.CompareTag("Player")) return;
        if (Time.time - lastContactDamageTime < 0.9f) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) return;

        // Durante la embestida solo golpea a ras de suelo: si Kai salta, la esquiva
        if (lowHitOnly && pc.transform.position.y > groundY + 0.6f) return;

        int dmg = lowHitOnly ? chargeDamage : contactDamage;
        pc.TakeDamage(dmg, transform);
        lastContactDamageTime = Time.time;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        if (!combatStarted) StartBossFight();

        lastPlayerEngagedTime = Time.time;

        float mult = isVulnerable ? vulnerableDamageMultiplier : armoredDamageMultiplier;
        int finalDamage = Mathf.Max(5, Mathf.RoundToInt(amount * mult));
        currentHealth = Mathf.Max(0, currentHealth - finalDamage);

        if (barraVidaOpcional != null) barraVidaOpcional.Actualizar(currentHealth, maxHealth);
        UpdateBossUI(null);

        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.enemyHit : null);
        MagicFX.Burst(transform.position + Vector3.up * 1.1f,
            isVulnerable ? new Color(0.3f, 0.95f, 1f, 0.95f) : new Color(1f, 0.5f, 0.2f, 0.8f), 12, 0.28f);

        if (currentHealth <= 0) Die();
    }

    private void UpdateBossUI(string phaseText)
    {
        if (bossHealthUI != null)
            bossHealthUI.UpdateBossHealth(currentHealth, maxHealth, isVulnerable, phaseText);
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
            companionMage.ForceDefeatByBossDeath();

        foreach (ArcanePowerCrystal c in activeCrystals)
            if (c != null) Destroy(c.gameObject);
        activeCrystals.Clear();

        if (bossHealthUI != null)
        {
            bossHealthUI.UpdateBossHealth(0, maxHealth, false, "¡DRAGÓN GUARDIÁN DERROTADO!");
            StartCoroutine(HideBossUIAfterDelay());
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

        // Aparece la Gema de Arcania en la Sala del Trono
        if (gemObjectToActivate != null) gemObjectToActivate.SetActive(true);

        StartCoroutine(DeactivateAfterDeath(2.0f));
    }

    private void OnDestroy()
    {
        Instances.Remove(this);
    }

    private IEnumerator DeactivateAfterDeath(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }

    /// <summary>Reactiva al Dragón Guardián y limpia proyectiles/cristales cuando Kai reaparece en un checkpoint.</summary>
    public static void ResetAllForCheckpointRespawn()
    {
        BossProjectile[] projectiles = FindObjectsByType<BossProjectile>();
        for (int i = 0; i < projectiles.Length; i++)
        {
            if (projectiles[i] != null) Destroy(projectiles[i].gameObject);
        }

        for (int i = 0; i < Instances.Count; i++)
        {
            if (Instances[i] != null) Instances[i].ResetEncounter();
        }
    }

    public void ResetEncounter()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();

        isDead = false;
        combatStarted = false;
        isVulnerable = false;
        lowHitOnly = false;
        currentPhase = BossPhase.WaitingForPlayer;
        lastContactDamageTime = -10f;
        lastPlayerEngagedTime = -10f;
        crystalsSpawned = 0;
        crystalSpawnIndex = 0;

        foreach (ArcanePowerCrystal c in activeCrystals)
            if (c != null) Destroy(c.gameObject);
        activeCrystals.Clear();

        transform.position = initialPosition;
        FaceDirection(-1f);
        SetBossTint(Color.white);

        currentHealth = maxHealth;
        if (bossCollider != null) bossCollider.enabled = true;
        if (barraVidaOpcional != null)
        {
            barraVidaOpcional.gameObject.SetActive(true);
            barraVidaOpcional.Actualizar(currentHealth, maxHealth);
        }
        if (bossHealthUI != null) bossHealthUI.SetVisible(false);

        if (gemObjectToActivate != null)
            gemObjectToActivate.SetActive(false);

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        if (companionMage != null)
            companionMage.ResetEncounter();
    }

    private IEnumerator HideBossUIAfterDelay()
    {
        yield return new WaitForSeconds(1.8f);
        if (bossHealthUI != null) bossHealthUI.SetVisible(false);
    }
}
