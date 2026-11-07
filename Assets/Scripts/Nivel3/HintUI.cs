using TMPro;
using UnityEngine;

/// <summary>
/// Muestra mensajes cortos en pantalla ("Pulsa H para...").
/// Ponlo en el Canvas (NO en el mismo objeto que el texto) y arrastra el TMP_Text al campo "label".
/// </summary>
public class HintUI : MonoBehaviour
{
    public static HintUI Instance { get; private set; }

    [SerializeField] private TMP_Text label;

    private object currentOwner;

    private void Awake()
    {
        Instance = this;
        if (label == null)
        {
            Debug.LogWarning("HintUI: falta asignar el TMP_Text en 'label'.", this);
            return;
        }
        label.gameObject.SetActive(false);
    }

    public static void Show(object owner, string text)
    {
        if (Instance == null || Instance.label == null) return;
        Instance.currentOwner = owner;
        Instance.label.text = text;
        Instance.label.gameObject.SetActive(true);
    }

    // Solo oculta si el mensaje actual pertenece a quien lo pide
    public static void Hide(object owner)
    {
        if (Instance == null || Instance.label == null) return;
        if (Instance.currentOwner != owner) return;
        Instance.currentOwner = null;
        Instance.label.gameObject.SetActive(false);
    }
}
