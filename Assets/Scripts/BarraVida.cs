using UnityEngine;
using UnityEngine.UI;

public class BarraVida : MonoBehaviour
{
    public Slider slider;
    public bool mantenerSinRotar = false; // actívalo en los enemigos

    public void Actualizar(int actual, int maximo)
    {
        slider.maxValue = maximo;
        slider.value = actual;
    }

    void LateUpdate()
    {
        // Evita que la barra se voltee cuando el enemigo gira (flip por rotación)
        if (mantenerSinRotar)
            transform.rotation = Quaternion.identity;
    }
}