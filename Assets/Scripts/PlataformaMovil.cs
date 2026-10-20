using UnityEngine;

public class PlataformaMovil : MonoBehaviour
{
    public Transform puntoA;
    public Transform puntoB;
    public float velocidad = 2f;

    private Rigidbody2D rb;
    private Collider2D miCollider;
    private Vector2 posA, posB, destino;
    private Vector2 velocidadActual;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        miCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        posA = puntoA.position;
        posB = puntoB.position;
        rb.position = posA;
        destino = posB;
    }

    void FixedUpdate()
    {
        Vector2 nueva = Vector2.MoveTowards(rb.position, destino, velocidad * Time.fixedDeltaTime);
        velocidadActual = (nueva - rb.position) / Time.fixedDeltaTime;
        rb.MovePosition(nueva);

        if (Vector2.Distance(nueva, destino) < 0.05f)
            destino = (destino == posA) ? posB : posA;
    }

    void OnCollisionStay2D(Collision2D col)
    {
        PlayerController jugador = col.gameObject.GetComponent<PlayerController>();
        if (jugador == null) return;

        bool encima = col.collider.bounds.min.y >= miCollider.bounds.max.y - 0.15f;
        jugador.velocidadPlataforma = encima ? velocidadActual : Vector2.zero;
    }

    void OnCollisionExit2D(Collision2D col)
    {
        PlayerController jugador = col.gameObject.GetComponent<PlayerController>();
        if (jugador != null) jugador.velocidadPlataforma = Vector2.zero;
    }
}