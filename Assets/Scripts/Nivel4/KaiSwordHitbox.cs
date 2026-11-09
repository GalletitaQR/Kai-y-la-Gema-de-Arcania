using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hitbox temporal (BoxCollider2D con isTrigger = true) para los ataques de espada de Kai:
/// - Golpe vertical hacia abajo (Tecla X)
/// - Tajo horizontal / barrido lateral más ancho y bajo (Tecla Z / J)
/// El collider permanece desactivado y solo se activa durante la ventana de golpe del ataque.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class KaiSwordHitbox : MonoBehaviour
{
    [Header("Configuración de Hitbox Temporal")]
    public int damage = 50;
    public float knockbackX = 4.5f;
    public float knockbackY = 2.0f;

    private BoxCollider2D hitboxCollider;
    private Transform ownerRoot;
    private float facingDirection = 1f;
    private readonly HashSet<IEnemigo> damagedThisSwing = new HashSet<IEnemigo>();

    void Awake()
    {
        hitboxCollider = GetComponent<BoxCollider2D>();
        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
            hitboxCollider.enabled = false;
        }
        ownerRoot = transform.root;
    }

    /// <summary>
    /// Activa el trigger temporal con el tamaño, posición local y daño indicados.
    /// </summary>
    public void ActivateHitbox(Vector2 localPos, Vector2 size, int attackDamage, float facingDir, float pushX = 4.5f, float pushY = 2.0f)
    {
        if (hitboxCollider == null) hitboxCollider = GetComponent<BoxCollider2D>();
        ownerRoot = transform.root;

        damagedThisSwing.Clear();
        damage = attackDamage;
        facingDirection = facingDir >= 0f ? 1f : -1f;
        knockbackX = pushX;
        knockbackY = pushY;

        transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
        hitboxCollider.offset = Vector2.zero;
        hitboxCollider.size = size;
        hitboxCollider.enabled = true;

        // Verificar también enemigos que ya estén dentro del área en el frame de activación
        Vector2 worldCenter = transform.TransformPoint(Vector3.zero);
        Vector3 lossy = transform.lossyScale;
        Vector2 worldSize = new Vector2(Mathf.Abs(size.x * lossy.x), Mathf.Abs(size.y * lossy.y));
        Collider2D[] hits = Physics2D.OverlapBoxAll(worldCenter, worldSize, 0f);
        for (int i = 0; i < hits.Length; i++)
        {
            TryDamageCollider(hits[i]);
        }
    }

    /// <summary>
    /// Desactiva la hitbox temporal al terminar el golpe.
    /// </summary>
    public void DeactivateHitbox()
    {
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
        damagedThisSwing.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hitboxCollider == null || !hitboxCollider.enabled) return;
        TryDamageCollider(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (hitboxCollider == null || !hitboxCollider.enabled) return;
        TryDamageCollider(other);
    }

    private void TryDamageCollider(Collider2D col)
    {
        if (col == null) return;
        if (ownerRoot != null && (col.transform == ownerRoot || col.transform.IsChildOf(ownerRoot))) return;

        IEnemigo enemigo = col.GetComponentInParent<IEnemigo>();
        if (enemigo == null) enemigo = col.GetComponent<IEnemigo>();

        if (enemigo != null && damagedThisSwing.Add(enemigo))
        {
            enemigo.TakeDamage(damage);

            Rigidbody2D erb = col.attachedRigidbody;
            if (erb != null && erb.bodyType == RigidbodyType2D.Dynamic)
            {
                erb.linearVelocity = new Vector2(facingDirection * knockbackX, knockbackY);
            }
        }
    }
}
