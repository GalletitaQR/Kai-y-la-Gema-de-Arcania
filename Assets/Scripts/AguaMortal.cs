using UnityEngine;

public class AguaMortal : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D col)
    {
        PlayerController jugador = col.GetComponent<PlayerController>();
        if (jugador != null)
            jugador.MuerteInstantanea();
    }
}