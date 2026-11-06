using UnityEngine;

/// <summary>
/// Puerta con dos objetos superpuestos: cerrada y abierta.
/// Ponlo en un objeto vacío con un Collider2D (Is Trigger) un poco más grande que la puerta.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Door : MonoBehaviour
{
    [Header("Visuales")]
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject openVisual;

    [Header("Sonido")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    [SerializeField] private AudioClip lockedClip; // opcional: suena al intentar con la puerta cerrada

    [Header("Opciones")]
    [SerializeField] private string playerTag = "Player";
    [TextArea]
    [SerializeField] private string lockedMessage = "La puerta está cerrada. Activa la palanca.";

    public bool IsOpen { get; private set; }

    private bool playerInside;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        SetState(false, false);
    }

    public void Open()
    {
        if (IsOpen) return;
        SetState(true, true);
    }

    public void Close()
    {
        if (!IsOpen) return;
        SetState(false, true);
    }

    private void SetState(bool open, bool playSound)
    {
        IsOpen = open;

        if (closedVisual != null) closedVisual.SetActive(!open);
        if (openVisual != null) openVisual.SetActive(open);

        if (playSound && audioSource != null)
        {
            AudioClip clip = open ? openClip : closeClip;
            if (clip != null) audioSource.PlayOneShot(clip);
        }

        if (open) HintUI.Hide(this);
        else if (playerInside) HintUI.Show(this, lockedMessage);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = true;

        if (!IsOpen)
        {
            HintUI.Show(this, lockedMessage);
            if (audioSource != null && lockedClip != null) audioSource.PlayOneShot(lockedClip);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = false;
        HintUI.Hide(this);
    }
}
