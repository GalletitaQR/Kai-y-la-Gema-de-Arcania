using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// On-screen message ("Has obtenido el primer fragmento..."). One per level, built by the setup tool.
/// Use: yield return MessageBanner.Instance.ShowRoutine("text", 3.5f);  or  MessageBanner.Instance.Show("text");
/// </summary>
public class MessageBanner : MonoBehaviour
{
    public static MessageBanner Instance { get; private set; }

    public CanvasGroup group;
    public TextMeshProUGUI label;

    private Coroutine running;

    private void Awake()
    {
        Instance = this;
        if (group != null)
            group.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show(string text, float seconds = 3.5f)
    {
        if (running != null)
            StopCoroutine(running);

        running = StartCoroutine(ShowRoutine(text, seconds));
    }

    public IEnumerator ShowRoutine(string text, float seconds)
    {
        if (label != null)
            label.text = text;

        yield return FadeTo(1f, 0.4f);
        yield return new WaitForSecondsRealtime(seconds);
        yield return FadeTo(0f, 0.5f);
        running = null;
    }

    private IEnumerator FadeTo(float target, float duration)
    {
        if (group == null)
            yield break;

        float start = group.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }

        group.alpha = target;
    }
}
