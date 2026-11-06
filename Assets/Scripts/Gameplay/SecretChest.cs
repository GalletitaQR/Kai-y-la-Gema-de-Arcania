using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Cofre Secreto interactivo en el Nivel 2.
/// Al acercarse Kai y presionar [E] (o acercarse a él):
/// 1. Congela y repele temporalmente a los enemigos cercanos para evitar daño durante la recolección.
/// 2. Reproduce la animación completa de apertura (28 fotogramas con haz de luz dorada).
/// 3. Hace emerger la Capa Arcana flotando en un orbe mágico que Kai absorbe.
/// 4. Otorga permanentemente la Capa Protectora (-20% de daño recibido) con escudo visible.
/// 5. Muestra el mensaje en pantalla explicando la protección.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class SecretChest : MonoBehaviour
{
    [Header("Animación de Apertura")]
    public Sprite[] openSprites;
    public float openFps = 18f;

    [Header("Recompensa")]
    public int scoreReward = 150;
    [TextArea]
    public string rewardMessage = "¡HAS ENCONTRADO LA CAPA ARCANA!\nTe protegerá en tu aventura reduciendo un 20% todo el daño recibido.";
    public float messageDuration = 5f;

    [Header("Seguridad y Repulsión")]
    public float safetyRadius = 10f;
    public float invincibilitySeconds = 4f;

    private SpriteRenderer sr;
    private Collider2D triggerCol;
    private bool isPlayerInRange = false;
    private bool isOpened = false;
    private PlayerController playerInRange;
    private GameObject promptObject;
    private TextMesh promptTextMesh;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        triggerCol = GetComponent<Collider2D>();
        triggerCol.isTrigger = true;

        EnsureSpritesLoaded();
        CreatePromptUI();
    }

    private void Start()
    {
        if (openSprites != null && openSprites.Length > 0 && sr.sprite == null)
        {
            sr.sprite = openSprites[0];
        }
    }

    public void EnsureSpritesLoaded()
    {
#if UNITY_EDITOR
        if (openSprites == null || openSprites.Length < 28)
        {
            List<Sprite> list = new List<Sprite>();
            for (int i = 0; i < 28; i++)
            {
                string path = $"Assets/Cave Platformer Tileset/Individual PNG files/Tileset/objects_bigchest_animated/objects_bigchest_animated_{i}.png";
                Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp != null) list.Add(sp);
            }
            if (list.Count > 0)
            {
                openSprites = list.ToArray();
                EditorUtility.SetDirty(this);
            }
        }
#endif
    }

    private void CreatePromptUI()
    {
        promptObject = new GameObject("ChestPrompt");
        promptObject.transform.SetParent(transform, false);
        promptObject.transform.localPosition = new Vector3(0f, 1.25f, 0f);

        promptTextMesh = promptObject.AddComponent<TextMesh>();
        promptTextMesh.text = "[E] Abrir Cofre";
        promptTextMesh.characterSize = 0.12f;
        promptTextMesh.fontSize = 24;
        promptTextMesh.alignment = TextAlignment.Center;
        promptTextMesh.anchor = TextAnchor.MiddleCenter;
        promptTextMesh.color = new Color(1f, 0.95f, 0.4f, 1f);

        MeshRenderer mr = promptObject.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingLayerName = "Player";
            mr.sortingOrder = 30;
        }

        promptObject.SetActive(false);
    }

    private void Update()
    {
        if (isOpened) return;

        if (isPlayerInRange && playerInRange != null)
        {
            // Efecto suave de flotación del cartel indicador
            if (promptObject != null && promptObject.activeSelf)
            {
                float bobY = 1.25f + Mathf.Sin(Time.time * 4f) * 0.08f;
                promptObject.transform.localPosition = new Vector3(0f, bobY, 0f);
            }

            // Abrir al presionar E, Z, X, W, Flecha Arriba o simplemente al acercarse mucho (<= 0.85m)
            bool keyPressed = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Z) ||
                              Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.W) ||
                              Input.GetKeyDown(KeyCode.UpArrow) || Input.GetButtonDown("Fire1");
            bool veryClose = Vector2.Distance(transform.position, playerInRange.transform.position) <= 0.85f;

            if (keyPressed || veryClose)
            {
                StartCoroutine(OpenRoutine(playerInRange));
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isOpened) return;

        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) pc = other.GetComponent<PlayerController>();

        if (pc != null)
        {
            playerInRange = pc;
            isPlayerInRange = true;
            if (promptObject != null) promptObject.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) pc = other.GetComponent<PlayerController>();

        if (pc != null && pc == playerInRange)
        {
            isPlayerInRange = false;
            playerInRange = null;
            if (promptObject != null) promptObject.SetActive(false);
        }
    }

    private IEnumerator OpenRoutine(PlayerController player)
    {
        isOpened = true;
        if (promptObject != null) promptObject.SetActive(false);

        // 1. SEGURIDAD: Inmunidad temporal para Kai y neutralizar enemigos cercanos
        player.StartInvincibility(invincibilitySeconds);
        NeutralizeNearbyEnemies();

        // 2. Audio de tesoro y primera explosión de partículas
        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.gemFragmentPickup : null);
        MagicFX.Burst(transform.position + Vector3.up * 0.4f, new Color(1f, 0.88f, 0.25f, 1f), 30, 0.5f);

        // 3. Animación de apertura de los 28 fotogramas del cofre
        if (openSprites != null && openSprites.Length > 0)
        {
            float waitPerFrame = 1f / openFps;
            for (int i = 0; i < openSprites.Length; i++)
            {
                sr.sprite = openSprites[i];
                yield return new WaitForSeconds(waitPerFrame);
            }
        }

        // 4. Luz mística resplandeciente saliendo del cofre
        MagicFX.Burst(transform.position + Vector3.up * 0.6f, new Color(1f, 0.9f, 0.3f, 1f), 45, 0.7f);
        MagicFX.Burst(transform.position + Vector3.up * 0.6f, new Color(0.75f, 0.35f, 1f, 1f), 35, 0.6f);

        // 5. Orbe flotante con la Capa Arcana emergiendo hacia Kai
        yield return StartCoroutine(SpawnFloatingCapeItem(player));

        // 6. Equipar visualmente la Capa Arcana + Escudo de Protección en Kai
        SaveSystem.HasCape = true;
        player.EquipCape();
        player.PlayAbsorbFeedback(new Color(0.8f, 0.4f, 1f, 1f));

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreReward);

        // 7. Mensaje claro en pantalla
        if (MessageBanner.Instance != null)
            MessageBanner.Instance.Show(rewardMessage, messageDuration);

        // Deja un halo de luz permanente sobre el cofre abierto
        MagicFX.AddGlow(transform, new Color(1f, 0.85f, 0.3f, 0.7f), 1.6f);
    }

    private void NeutralizeNearbyEnemies()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, safetyRadius);
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Enemigo") || col.GetComponent<IEnemigo>() != null)
            {
                // Empujar al enemigo suavemente hacia afuera del cofre
                Rigidbody2D rb = col.attachedRigidbody;
                if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
                {
                    Vector2 pushDir = (col.transform.position - transform.position).normalized;
                    if (pushDir.sqrMagnitude < 0.01f) pushDir = Vector2.right;
                    rb.linearVelocity = pushDir * 3.5f + Vector2.up * 1.5f;
                }
            }
        }
    }

    private IEnumerator SpawnFloatingCapeItem(PlayerController player)
    {
        // Crear un objeto visual que sube flotando desde el cofre hacia Kai
        GameObject orb = new GameObject("FloatingArcaneCape");
        orb.transform.position = transform.position + Vector3.up * 0.4f;
        orb.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

        SpriteRenderer srOrb = orb.AddComponent<SpriteRenderer>();
        Sprite capeSp = Resources.Load<Sprite>("Addons/Legacy/0_Unit/0_Sprite/7_Back/Back_2");
        if (capeSp == null && player != null)
        {
            Transform backT = player.FindBackTransform();
            if (backT != null)
            {
                SpriteRenderer srB = backT.GetComponent<SpriteRenderer>();
                if (srB != null) capeSp = srB.sprite;
            }
        }
        srOrb.sprite = capeSp != null ? capeSp : MagicFX.GlowSprite;
        srOrb.color = new Color(0.92f, 0.75f, 1f, 1f);
        srOrb.sortingLayerName = "Player";
        srOrb.sortingOrder = 25;

        MagicFX.AddGlow(orb.transform, new Color(0.85f, 0.45f, 1f, 0.9f), 1.6f);

        Vector3 start = orb.transform.position;
        Vector3 peak = start + Vector3.up * 1.5f;

        // Subir hacia arriba en el haz de luz
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.6f;
            orb.transform.position = Vector3.Lerp(start, peak, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        // Breve pausa gloriosa en el aire para admirar la capa
        MagicFX.Burst(peak, new Color(1f, 0.9f, 0.4f, 1f), 20, 0.35f);
        yield return new WaitForSeconds(0.45f);

        // Volar hacia el pecho de Kai
        t = 0f;
        Vector3 curPos = orb.transform.position;
        while (t < 1f && player != null)
        {
            t += Time.deltaTime * 2.5f;
            Vector3 target = player.transform.position + Vector3.up * 0.45f;
            orb.transform.position = Vector3.Lerp(curPos, target, t);
            yield return null;
        }

        MagicFX.Burst(orb.transform.position, new Color(0.85f, 0.45f, 1f, 1f), 30, 0.4f);
        Destroy(orb);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, safetyRadius);
    }
}

