using UnityEngine;

/// <summary>Gentle vertical float and/or scale pulse for pickups, glows and NPCs.</summary>
public class Bobbing : MonoBehaviour
{
    public float amplitude = 0.1f;
    public float speed = 2f;
    [Tooltip("Scale pulse, 0.1 = grows and shrinks 10 %")]
    public float pulse = 0f;

    private Vector3 startPosition;
    private Vector3 startScale;
    private float phase;

    private void OnEnable()
    {
        startPosition = transform.localPosition;
        startScale = transform.localScale;
        phase = Random.value * Mathf.PI * 2f;
    }

    private void OnDisable()
    {
        transform.localPosition = startPosition;
        transform.localScale = startScale;
    }

    private void Update()
    {
        float wave = Mathf.Sin(Time.time * speed + phase);
        transform.localPosition = startPosition + Vector3.up * (wave * amplitude);

        if (pulse > 0f)
            transform.localScale = startScale * (1f + wave * pulse);
    }
}
