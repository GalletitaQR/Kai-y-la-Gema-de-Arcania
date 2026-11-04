using System.Collections;
using UnityEngine;

/// <summary>
/// "Baston del Aprendiz" at the end of the level.
/// The root has a trigger zone (the door area). When Kai enters it: he walks under the staff, jumps and
/// catches it, the staff becomes a child of his hand, both spin together as a celebration, the message appears
/// and the level ends (fade to the next level).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StaffPickup : MonoBehaviour
{
    private const string HandName = "R_Weapon";

    [Header("Bastón")]
    [Tooltip("Child object with the staff sprite, floating above the door.")]
    public Transform staff;
    public Color glowColor = new Color(0.62f, 0.25f, 1f, 0.85f);

    [Header("Secuencia")]
    public float walkSpeed = 2.5f;
    public int spins = 2;
    public float spinDuration = 1.4f;
    [TextArea]
    public string message = "¡Has obtenido el Bastón del Aprendiz! Te ayudará a derrotar a tus enemigos";
    public float messageSeconds = 4f;
    public int scoreValue = 100;

    private bool triggered;

    private void Awake()
    {
        if (staff == null)
            return;

        MagicFX.AddGlow(staff, glowColor, 1.6f);
        MagicFX.AddSparkles(staff, glowColor, 0.35f, 16f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player.IsDead)
            return;

        triggered = true;
        StartCoroutine(Sequence(player));
    }

    private IEnumerator Sequence(PlayerController player)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Cutscene);

        player.SetControlEnabled(false);
        Rigidbody2D body = player.Body;

        // 1. Walk until Kai is right under the staff.
        float timeout = 3f;
        while (Mathf.Abs(player.transform.position.x - staff.position.x) > 0.12f && timeout > 0f)
        {
            float direction = Mathf.Sign(staff.position.x - player.transform.position.x);
            player.FaceDirection(direction);
            player.SetAnimation(PlayerState.MOVE);
            body.linearVelocity = new Vector2(direction * walkSpeed, body.linearVelocity.y);
            timeout -= Time.deltaTime;
            yield return null;
        }

        body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        player.SetAnimation(PlayerState.IDLE);
        yield return new WaitForSeconds(0.25f);

        // 2. Jump high enough to reach it.
        float height = Mathf.Max(0.8f, staff.position.y - player.transform.position.y - 0.4f);
        float jumpSpeed = Mathf.Clamp(Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * body.gravityScale * height), 4f, 10f);
        body.linearVelocity = new Vector2(0f, jumpSpeed);
        player.SetAnimation(PlayerState.MOVE);

        float airTime = 0f;
        while (body.linearVelocity.y > 0.3f && airTime < 1.5f)
        {
            airTime += Time.deltaTime;
            yield return null;
        }

        // 3. Catch it: the staff goes to Kai's hand and becomes his child.
        yield return CatchStaff(player);

        SaveSystem.HasStaff = true;
        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.staffPickup : null);

        // 4. Land.
        yield return new WaitForSeconds(0.3f);
        float landTimeout = 2f;
        while (Mathf.Abs(body.linearVelocity.y) > 0.05f && landTimeout > 0f)
        {
            landTimeout -= Time.deltaTime;
            yield return null;
        }

        player.SetAnimation(PlayerState.IDLE);
        yield return new WaitForSeconds(0.2f);

        // 5. Celebration: Kai and the staff spin together.
        yield return Celebrate(player);

        // 6. Message, then the level is complete.
        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreValue);

        if (MessageBanner.Instance != null)
            yield return MessageBanner.Instance.ShowRoutine(message, messageSeconds);

        if (GameManager.Instance != null)
            GameManager.Instance.CompleteLevel();
    }

    private IEnumerator CatchStaff(PlayerController player)
    {
        Bobbing bobbing = staff.GetComponent<Bobbing>();
        if (bobbing != null)
            bobbing.enabled = false;

        Transform hand = FindHand(player.transform);
        Vector3 startPosition = staff.position;
        Quaternion startRotation = staff.rotation;
        Quaternion holdRotation = Quaternion.Euler(0f, 0f, 10f);

        staff.SetParent(hand, true);

        SpriteRenderer renderer = staff.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = 60;
        }

        MagicFX.Burst(hand.position, glowColor, 35, 0.35f);

        const float duration = 0.25f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            staff.position = Vector3.Lerp(startPosition, hand.position, k);
            staff.rotation = Quaternion.Slerp(startRotation, holdRotation, k);
            yield return null;
        }

        staff.position = hand.position;
        staff.rotation = holdRotation;
    }

    private IEnumerator Celebrate(PlayerController player)
    {
        Rigidbody2D body = player.Body;
        float startY = player.transform.eulerAngles.y;

        MagicFX.Burst(player.transform.position + Vector3.up * 0.4f, glowColor, 50, 0.5f);
        body.linearVelocity = new Vector2(0f, 5.5f);

        float t = 0f;
        while (t < spinDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / spinDuration);
            player.transform.rotation = Quaternion.Euler(0f, startY + 360f * spins * k, 0f);
            yield return null;
        }

        player.transform.rotation = Quaternion.Euler(0f, startY, 0f);
        MagicFX.Burst(player.transform.position + Vector3.up * 0.4f, glowColor, 40, 0.5f);
    }

    private static Transform FindHand(Transform root)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == HandName)
                return t;
        }

        return root;
    }
}
