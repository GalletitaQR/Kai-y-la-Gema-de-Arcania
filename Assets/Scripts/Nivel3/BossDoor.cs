using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class BossDoor : MonoBehaviour
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
    [SerializeField] private AudioClip zoneMusic;

    [Header("Escena de destino")]
    [Tooltip("Nombre exacto de la escena (debe estar en Build Settings)")]
    [SerializeField] private string targetScene = "Nivel3";
    [Tooltip("Segundos de espera antes de cargar la escena")]
    [SerializeField] private float sceneLoadDelay = 0.5f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [Tooltip("Evento extra al cruzar (ej. secuencia de victoria)")]
    public UnityEvent onPlayerCross;

    [Header("Mensajes de Texto")]
    [SerializeField] private string playerTag = "Player";
    [TextArea]
    [SerializeField] private string lockedMessage = "La puerta está cerrada. ¡Derrota al jefe para avanzar!";
    [TextArea]
    [SerializeField] private string openMessage = "Presiona [E] para avanzar";

    public bool IsOpen { get; private set; }

    private bool playerInside;
    private bool crossing;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        SetState(false, false);
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null) return false;
        if (other.CompareTag(playerTag)) return true;
        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag)) return true;
        if (other.GetComponentInParent<PlayerController>() != null) return true;
        return false;
    }

    private void Update()
    {
        if (playerInside && IsOpen && !crossing && (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.H)))
        {
            Cross();
        }
    }

    private void Cross()
    {
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("Falta asignar Target Scene en la puerta.", this);
            return;
        }

        crossing = true;
        HintUI.Hide(this);
        onPlayerCross?.Invoke();

        if (zoneMusic != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(zoneMusic);

        Invoke(nameof(LoadScene), sceneLoadDelay);
    }

    private void LoadScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(targetScene);
    }

    /// <summary>Lo llama el jefe al morir (DarkKnightBoss.Die → bossDoor.Open()).</summary>
    public void Open()
    {
        if (IsOpen) return;
        SetState(true, true);

        if (playerInside && HintUI.Instance != null)
            HintUI.Show(this, openMessage);
    }

    public void Close()
    {
        if (!IsOpen) return;
        SetState(false, true);

        if (playerInside && HintUI.Instance != null)
            HintUI.Show(this, lockedMessage);
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
        if (!IsPlayerCollider(other)) return;
        playerInside = true;

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
        if (!IsPlayerCollider(other)) return;
        playerInside = false;
        HintUI.Hide(this);
    }
}