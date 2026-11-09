using UnityEngine;

/// <summary>
/// Zona de trampa (pinchos o caída peligrosa) para el Nivel 4 (Templo de Arcania).
/// Si instantKill es true, mata a Kai al instante (vacíos/abismos).
/// Si instantKill es false, aplica daño y retroceso a Kai (pinchos).
/// </summary>
public class HazardZone : MonoBehaviour
{
    [Header("Configuración de Trampa")]
    public bool instantKill = false;
    public int damage = 15;
    public float damageCooldown = 0.8f;

    private float nextDamageTime = 0f;

    private void OnTriggerEnter2D(Collider2D col)
    {
        TryApplyHazard(col);
    }

    private void OnTriggerStay2D(Collider2D col)
    {
        TryApplyHazard(col);
    }

    private void TryApplyHazard(Collider2D col)
    {
        if (Time.time < nextDamageTime) return;

        PlayerController player = col.GetComponent<PlayerController>();
        if (player == null)
            player = col.GetComponentInParent<PlayerController>();

        if (player == null) return;

        nextDamageTime = Time.time + damageCooldown;

        if (instantKill)
        {
            player.MuerteInstantanea();
        }
        else
        {
            player.TakeDamage(damage, transform);
        }
    }
}
