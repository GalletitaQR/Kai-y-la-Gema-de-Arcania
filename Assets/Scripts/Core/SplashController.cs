using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Splash screen with the team logo. Fades in, holds, fades out and loads the main menu. Any key skips it.</summary>
public class SplashController : MonoBehaviour
{
    public CanvasGroup logoGroup;
    public float fadeIn = 1f;
    public float hold = 2f;
    public float fadeOut = 1f;
    public string nextScene = "MainMenu";

    private bool skip;

    private IEnumerator Start()
    {
        if (logoGroup != null)
            logoGroup.alpha = 0f;

        yield return Fade(0f, 1f, fadeIn);

        float t = 0f;
        while (t < hold && !skip)
        {
            t += Time.deltaTime;
            yield return null;
        }

        yield return Fade(1f, 0f, fadeOut);
        SceneManager.LoadScene(nextScene);
    }

    private void Update()
    {
        if (Input.anyKeyDown)
            skip = true;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration && !skip)
        {
            t += Time.deltaTime;
            if (logoGroup != null)
                logoGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        if (logoGroup != null)
            logoGroup.alpha = to;
    }
}
