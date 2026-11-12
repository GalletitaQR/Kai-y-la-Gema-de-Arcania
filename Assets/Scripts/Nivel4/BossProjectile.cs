using UnityEngine;

/// <summary>
/// Proyectil usado en la Sala del Jefe (Nivel 4):
/// - Bolas de fuego del Dragón Guardián (DragonRed)
/// - Hechizos arcanos del Mage Esqueleto (SkeletonMage)
/// </summary>
public class BossProjectile : MonoBehaviour
{
    [Header("Configuración del Proyectil")]
    public float speed = 7.5f;
    public int damage = 18;
    public float lifeTime = 4.5f;
    public float hitRadius = 0.58f;
    public Color trailColor = new Color(1f, 0.42f, 0.1f, 0.9f);
    public bool rotateTowardsDirection = true;

    private Vector2 moveDirection = Vector2.left;
    private float fxTimer = 0f;
    private bool hasHit = false;
    private PlayerController cachedPlayer;
    private Collider2D cachedPlayerCollider;

    void Start()
    {
        // Asegurar capa Default para colisionar también por trigger con Layer 7 (Player)
        if (gameObject.layer == 8) gameObject.layer = 0;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            cachedPlayer = playerObj.GetComponent<PlayerController>();
            if (cachedPlayer == null) cachedPlayer = playerObj.GetComponentInParent<PlayerController>();
            cachedPlayerCollider = playerObj.GetComponent<Collider2D>();
        }

        Destroy(gameObject, lifeTime);
    }

    public void Initialize(Vector2 direction, float projSpeed, int projDamage, Color color)
    {
        moveDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.left;
        speed = projSpeed;
        damage = projDamage;
        trailColor = color;
        if (gameObject.layer == 8) gameObject.layer = 0;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (sr.sprite == null) sr.sprite = MagicFX.GlowSprite;
            sr.color = color;
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 18;
        }

        if (rotateTowardsDirection)
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    void Update()
    {
        if (hasHit) return;

        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);

        fxTimer += Time.deltaTime;
        if (fxTimer >= 0.09f)
        {
            fxTimer = 0f;
            MagicFX.Burst(transform.position, trailColor, 3, 0.14f);
        }

        // Comprobación directa de proximidad además de OnTriggerEnter2D (por matriz de capas Player/Enemy)
        if (cachedPlayer != null && !cachedPlayer.IsDead)
        {
            Vector2 hitTarget = cachedPlayerCollider != null
                ? (Vector2)cachedPlayerCollider.bounds.center
                : (Vector2)cachedPlayer.transform.position + new Vector2(0f, 0.45f);

            if (Vector2.Distance(transform.position, hitTarget) <= hitRadius)
            {
                HitPlayer(cachedPlayer);
            }
        }
    }

    private void HitPlayer(PlayerController pc)
    {
        if (hasHit) return;
        hasHit = true;

        if (pc != null)
        {
            pc.TakeDamage(damage, transform);
        }
        MagicFX.Burst(transform.position, trailColor, 10, 0.25f);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || other == null) return;

        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc == null) pc = other.GetComponentInParent<PlayerController>();
            HitPlayer(pc);
            return;
        }

        // Choca contra el suelo o paredes (Layer 6 = Ground)
        if (other.gameObject.layer == 6 && !other.isTrigger)
        {
            hasHit = true;
            MagicFX.Burst(transform.position, trailColor, 8, 0.2f);
            Destroy(gameObject);
        }
    }
}

