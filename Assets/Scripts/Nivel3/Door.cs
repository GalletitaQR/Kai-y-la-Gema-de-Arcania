using UnityEngine;

/// <summary>
/// Puerta interactiva con soporte para mensajes dinámicos (abierta/cerrada),
/// teletransporte y gestión de audio.
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
    [SerializeField] private AudioClip lockedClip;
    [SerializeField] private AudioClip newStageBGM; // Música opcional al cruzar

    [Header("Opciones de Teletransporte")]
    [SerializeField] private Transform targetDoor; 
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Mensajes de Texto")]
    [SerializeField] private string playerTag = "Player";
    [TextArea]
    [SerializeField] private string lockedMessage = "La puerta está cerrada. Activa la palanca.";
    [TextArea]
    [SerializeField] private string openMessage = "Presiona [E] para entrar";

    public bool IsOpen { get; private set; }

    private bool playerInside;
    private Transform playerTransform;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        SetState(false, false);
    }

    private void Update()
    {
        if (playerInside && IsOpen && Input.GetKeyDown(interactKey))
        {
            TeleportPlayer();
        }
    }

    private void TeleportPlayer()
    {
        if (targetDoor != null && playerTransform != null)
        {
            // Si quieres cambiar la música aquí, asegúrate de llamar al método correcto de tu AudioManager.
            // Por ejemplo, si tu AudioManager usa otra función, cámbiala aquí. 
            // Si no usas música por código aquí, puedes dejar esta parte comentada o adaptada a tu AudioManager.
            
            playerTransform.position = targetDoor.position;
        }
        else
        {
            Debug.LogWarning("Falta asignar el Target Door (destino) o no se encontró al jugador.", this);
        }
    }

    public void Open()
    {
        if (IsOpen) return;
        SetState(true, true);

        if (playerInside && HintUI.Instance != null)
        {
            HintUI.Show(this, openMessage);
        }
    }

    public void Close()
    {
        if (!IsOpen) return;
        SetState(false, true);

        if (playerInside && HintUI.Instance != null)
        {
            HintUI.Show(this, lockedMessage);
        }
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
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = true;
        playerTransform = other.transform;

        if (HintUI.Instance != null)
        {
            if (IsOpen)
            {
                HintUI.Show(this, openMessage);
            }
            else
            {
                HintUI.Show(this, lockedMessage);
                if (audioSource != null && lockedClip != null) audioSource.PlayOneShot(lockedClip);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = false;
        playerTransform = null;
        HintUI.Hide(this);
    }
}