using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Interactive tutorial for level 1. Shows one tip at a time at the top of the screen and moves on
/// when Kai does what the tip asks. It runs right after the prologue (first time only).
/// </summary>
public class TutorialController : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI tipText;

    private bool running;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    /// <summary>Called when the prologue was already seen: nothing to show.</summary>
    public void SkipWithoutShowing()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    public void Begin()
    {
        if (!running)
            StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        running = true;
        if (panel != null)
            panel.SetActive(true);

        yield return Step("Muévete con el Joystick, las flechas ← → o con A y D", () =>
            Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f ||
            (VirtualControlsUI.Instance != null && Mathf.Abs(VirtualControlsUI.Instance.HorizontalAxis) > 0.1f), 0.5f);
        yield return Step("Salta con el botón SALTAR o la barra ESPACIO", () =>
            Input.GetButtonDown("Jump") ||
            (VirtualControlsUI.Instance != null && VirtualControlsUI.Instance.JumpPressedThisFrame), 0.5f);
        yield return Step("Ataca con el botón ATACAR o la tecla X para golpear a los enemigos", () =>
            Input.GetKeyDown(KeyCode.X) ||
            (VirtualControlsUI.Instance != null && VirtualControlsUI.Instance.AttackPressedThisFrame), 0.5f);
        yield return Step("Recoge los corazones para recuperar vida. Los cangrejos y murciélagos te quitan vida.", () => true, 5f);
        yield return Step("Pulsa ESC para pausar. Busca el fragmento de la Gema de Arcanía al final del bosque.", () => true, 5f);

        if (panel != null)
            panel.SetActive(false);
        running = false;
    }

    private IEnumerator Step(string message, Func<bool> completed, float minimumSeconds)
    {
        if (tipText != null)
            tipText.text = message;

        float shownFor = 0f;
        while (true)
        {
            yield return null;

            // Paused or in a cutscene: wait.
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
                continue;

            shownFor += Time.deltaTime;
            if (shownFor >= minimumSeconds && completed())
                yield break;
        }
    }
}
