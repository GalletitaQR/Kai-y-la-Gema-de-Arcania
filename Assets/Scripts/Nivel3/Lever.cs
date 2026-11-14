using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Palanca interactiva. Ponlo en el objeto de la palanca (con Collider2D Is Trigger).
/// Cuando el jugador está cerca muestra el mensaje; con la tecla H abre la puerta.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Lever : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Door door;

    [Header("Visual de la palanca (usa SOLO una opción)")]
    [Header("Opción A: un objeto que cambia de sprite")]
    [SerializeField] private SpriteRenderer leverSprite;
    [SerializeField] private Sprite offSprite; // palanca desactivada
    [SerializeField] private Sprite onSprite;  // palanca activada
    [Header("Opción B: dos objetos superpuestos")]
    [SerializeField] private GameObject offObject; // palanca desactivada
    [SerializeField] private GameObject onObject;  // palanca activada

    [Header("Sonido")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip leverClip;

    [Header("Opciones")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Si está activo, pulsar H otra vez cierra la puerta.")]
    [SerializeField] private bool canToggle = false;
    [TextArea]
    [SerializeField] private string promptText = "Pulsa la tecla H para abrir la puerta";
    [TextArea]
    [SerializeField] private string promptCloseText = "Pulsa la tecla H para cerrar la puerta";

    private bool playerInside;
    private bool activated;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
        leverSprite = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        ApplyVisual(false);
    }

    private void ApplyVisual(bool on)
    {
        // Opción B: dos objetos superpuestos
        if (offObject != null)
        {
            if (offObject == gameObject)
            {
                // Si el objeto apagado es el mismo que tiene este script y collider,
                // solo ocultamos/mostramos sus renderers para no desactivar el script ni el collider.
                var rends = offObject.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends) r.enabled = !on;
            }
            else
            {
                offObject.SetActive(!on);
            }
        }

        if (onObject != null) onObject.SetActive(on);

        // Opción A: cambio de sprite (si no hay sprites asignados, solo voltea)
        if (leverSprite != null)
        {
            if (offSprite != null && onSprite != null)
                leverSprite.sprite = on ? onSprite : offSprite;
            else
                leverSprite.flipX = on;
        }
    }

    private void Update()
    {
        if (!playerInside || !InteractPressed()) return;

        if (!activated) SetActivated(true);
        else if (canToggle) SetActivated(false);
    }

    private void SetActivated(bool value)
    {
        activated = value;

        ApplyVisual(value);
        if (audioSource != null && leverClip != null) audioSource.PlayOneShot(leverClip);

        if (door != null)
        {
            if (value) door.Open();
            else door.Close();
        }

        RefreshHint();
    }

    private void RefreshHint()
    {
        if (!playerInside) return;

        if (!activated) HintUI.Show(this, promptText);
        else if (canToggle) HintUI.Show(this, promptCloseText);
        else HintUI.Hide(this);
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null) return false;
        if (other.CompareTag(playerTag)) return true;
        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag)) return true;
        if (other.GetComponentInParent<PlayerController>() != null) return true;
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerCollider(other)) return;
        playerInside = true;
        RefreshHint();
        VirtualControlsUI.RequestInteractButton(true, "PALANCA");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other)) return;
        playerInside = false;
        HintUI.Hide(this);
        VirtualControlsUI.RequestInteractButton(false);
    }

    private static bool InteractPressed()
    {
        if (VirtualControlsUI.Instance != null && VirtualControlsUI.Instance.InteractPressedThisFrame)
            return true;
#if ENABLE_INPUT_SYSTEM
        bool hPressed = Keyboard.current != null && (Keyboard.current.hKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame);
        if (hPressed) return true;
#endif
        return Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.E);
    }
}
