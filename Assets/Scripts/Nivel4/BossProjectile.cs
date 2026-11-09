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
    public Color trailColor = new Color(1f, 0.42f, 0.1f, 0.9f);
    public bool rotateTowardsDirection = true;

    private Vector2 moveDirection = Vector2.left;
    private float fxTimer = 0f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Initialize(Vector2 direction, float projSpeed, int projDamage, Color color)
    {
        moveDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.left;
        speed = projSpeed;
        damage = projDamage;
        trailColor = color;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = color;
        }

        if (rotateTowardsDirection)
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    void Update()
    {
        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);

        fxTimer += Time.deltaTime;
        if (fxTimer >= 0.09f)
        {
            fxTimer = 0f;
            MagicFX.Burst(transform.position, trailColor, 3, 0.14f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc == null) pc = other.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(damage, transform);
            }
            MagicFX.Burst(transform.position, trailColor, 10, 0.25f);
            Destroy(gameObject);
            return;
        }

        // Choca contra el suelo o paredes (Layer 6 = Ground)
        if (other.gameObject.layer == 6 && !other.isTrigger)
        {
            MagicFX.Burst(transform.position, trailColor, 8, 0.2f);
            Destroy(gameObject);
        }
    }
}
