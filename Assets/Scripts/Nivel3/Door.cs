using UnityEngine;

/// <summary>
/// Puerta interactiva que transporta al jugador, muestra mensajes contextuales
/// y cambia la música de fondo al cruzar (ideal para zonas como minijefes).
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
    
    [Header("Música de la Nueva Zona")]
    [SerializeField] private AudioClip zoneMusic; // <--- Arrastra aquí la música del minijefe o nueva zona

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
            // 1. Cambiar la música de fondo si se asignó un AudioClip para esta zona
            if (zoneMusic != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(zoneMusic);
            }

            // 2. Teletransportar al jugador a la puerta de destino
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