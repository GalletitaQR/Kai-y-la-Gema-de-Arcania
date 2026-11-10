using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Pinchos : MonoBehaviour
{
    [Header("Daño")]
    public int dano = 20;
    [Tooltip("Si está activo, mata al jugador al instante (ignora el daño).")]
    public bool muerteInstantanea = false;

    [Header("Rebote (opcional)")]
    [Tooltip("Impulso hacia arriba al pincharse. 0 = sin rebote.")]
    public float fuerzaRebote = 0f;

    [Header("Sonido (opcional)")]
    public AudioClip sonidoPinchos;

    void Reset()
    {
        // Por defecto como trigger para que el jugador "entre" en los pinchos
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    // ---- Si el collider es Trigger ----
    void OnTriggerEnter2D(Collider2D other) { Intentar(other); }
    void OnTriggerStay2D(Collider2D other)  { Intentar(other); }

    // ---- Si el collider es sólido (no Trigger) ----
    void OnCollisionEnter2D(Collision2D col) { Intentar(col.collider); }
    void OnCollisionStay2D(Collision2D col)  { Intentar(col.collider); }

    void Intentar(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player.IsDead) return;

        if (muerteInstantanea)
        {
            player.MuerteInstantanea();
            return;
        }

        int vidaAntes = player.currentHealth;
        player.TakeDamage(dano, transform);

        // Solo reacciona si realmente recibió daño (no estaba invencible)
        if (player.currentHealth < vidaAntes)
        {
            if (sonidoPinchos != null)
                AudioSource.PlayClipAtPoint(sonidoPinchos, transform.position);

            if (fuerzaRebote > 0f && !player.IsDead && player.Body != null)
            {
                player.Body.linearVelocity = new Vector2(player.Body.linearVelocity.x, fuerzaRebote);
            }
        }
    }
}