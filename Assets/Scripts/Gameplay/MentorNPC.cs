using UnityEngine;

/// <summary>
/// The Guardian of Arcania (mentor wizard). Plays an idle animation made from a horizontal sprite sheet,
/// stands on the ground and always looks at Kai. It stays at its post watching over the portal.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class MentorNPC : MonoBehaviour
{
    [Header("Sprite sheet (frames in one row)")]
    public Sprite sheet;
    public int frameCount = 5;
    public float framesPerSecond = 5f;
    public float pixelsPerUnit = 150f;
    [Tooltip("Tick if the wizard in the sheet looks to the right.")]
    public bool sheetFacesRight = true;

    [Header("Posición")]
    public bool snapToGround = true;
    public LayerMask groundLayer = 1 << 6;

    [Header("Aura")]
    public Color auraColor = new Color(0.55f, 0.75f, 1f, 0.5f);

    private Sprite[] frames;
    private SpriteRenderer spriteRenderer;
    private Transform player;

    /// <summary>First frame, used as portrait in the dialogue box.</summary>
    public Sprite Portrait => frames != null && frames.Length > 0 ? frames[0] : sheet;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        BuildFrames();

        if (frames != null && frames.Length > 0)
            spriteRenderer.sprite = frames[0];

        MagicFX.AddGlow(transform, auraColor, 2.2f, 0.08f).transform.localPosition = new Vector3(0f, 0.7f, 0f);
    }

    private void Start()
    {
        if (snapToGround)
        {
            RaycastHit2D hit = Physics2D.Raycast((Vector2)transform.position + Vector2.up * 6f, Vector2.down, 25f, groundLayer);
            if (hit.collider != null)
                transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
        }

        PlayerController found = FindFirstObjectByType<PlayerController>();
        if (found != null)
            player = found.transform;
    }

    private void Update()
    {
        if (frames != null && frames.Length > 1)
        {
            int index = Mathf.FloorToInt(Time.time * framesPerSecond) % frames.Length;
            spriteRenderer.sprite = frames[index];
        }

        if (player != null)
        {
            float dx = player.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.2f)
            {
                bool playerIsRight = dx > 0f;
                spriteRenderer.flipX = sheetFacesRight ? !playerIsRight : playerIsRight;
            }
        }
    }

    private void BuildFrames()
    {
        if (sheet == null)
            return;

        Texture2D texture = sheet.texture;
        int count = Mathf.Max(1, frameCount);
        float cellWidth = texture.width / (float)count;

        frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            Rect rect = new Rect(Mathf.Round(i * cellWidth), 0f, Mathf.Floor(cellWidth), texture.height);
            frames[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
        }
    }
}
