using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Full-screen black fade that survives scene changes (Mario style transition).
/// It builds its own canvas, no setup needed: ScreenFader.FadeAndLoad("Nivel2").
/// </summary>
public class ScreenFader : MonoBehaviour
{
    private static ScreenFader instance;

    private Image overlay;
    private bool loading;

    private static ScreenFader Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("ScreenFader");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<ScreenFader>();
                instance.Build();
            }

            return instance;
        }
    }

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        GameObject imageGo = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
        imageGo.transform.SetParent(transform, false);
        RectTransform rt = (RectTransform)imageGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        overlay = imageGo.GetComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0f);
        overlay.raycastTarget = false;
    }

    /// <summary>Gradually darkens the screen, loads the scene, then lights it up again.</summary>
    public static void FadeAndLoad(string sceneName, float duration = 1.5f)
    {
        Instance.StartCoroutine(Instance.FadeAndLoadRoutine(sceneName, duration));
    }

    /// <summary>Only darkens the screen.</summary>
    public static IEnumerator FadeOut(float duration)
    {
        return Instance.Fade(0f, 1f, duration);
    }

    private IEnumerator FadeAndLoadRoutine(string sceneName, float duration)
    {
        if (loading)
            yield break;

        loading = true;
        yield return Fade(0f, 1f, duration);

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
        yield return null;

        yield return Fade(1f, 0f, duration * 0.6f);
        loading = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        overlay.raycastTarget = true;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }

        SetAlpha(to);
        overlay.raycastTarget = to > 0.5f;
    }

    private void SetAlpha(float alpha)
    {
        overlay.color = new Color(0f, 0f, 0f, alpha);
    }
}
