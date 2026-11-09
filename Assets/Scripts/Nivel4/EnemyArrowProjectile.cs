using UnityEngine;

/// <summary>
/// Proyectil de flecha disparado por el SkeletonArcher en el Nivel 4.
/// Se mueve en línea recta hacia la dirección calculada al disparar y daña a Kai al impactar.
/// </summary>
public class EnemyArrowProjectile : MonoBehaviour
{
    [Header("Configuración de la Flecha")]
    public float speed = 7f;
    public int damage = 15;
    public float lifeTime = 4f;

    private Vector2 moveDirection = Vector2.left;
    private bool initialized = false;

    void Start()
    {
        if (!initialized)
        {
            ApplyRotation();
        }
        Destroy(gameObject, lifeTime);
    }

    public void Initialize(Vector2 direction, float arrowSpeed, int arrowDamage)
    {
        if (direction.sqrMagnitude > 0.0001f)
            moveDirection = direction.normalized;
        else
            moveDirection = Vector2.left;

        speed = arrowSpeed;
        damage = arrowDamage;
        initialized = true;
        ApplyRotation();
    }

    private void ApplyRotation()
    {
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        // Si golpea a Kai
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null) player = other.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                player.TakeDamage(damage, transform);
            }
            Destroy(gameObject);
            return;
        }

        // Si choca con el suelo o muros (Layer 6 = Ground)
        if (other.gameObject.layer == 6 && !other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}

