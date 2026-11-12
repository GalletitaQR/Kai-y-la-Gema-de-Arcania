using UnityEngine;

/// <summary>
/// Violet magic projectile fired by the Apprentice Staff (unlocked at the end of level 1, used from level 2).
/// </summary>
public class MagicBolt : MonoBehaviour
{
    private const int GroundLayer = 6;

    private int damage;
    private bool hasHit;

    public static void Spawn(Vector3 position, float direction, int damage)
    {
        GameObject go = new GameObject("MagicBolt");
        go.transform.position = position;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = MagicFX.GlowSprite;
        renderer.color = new Color(0.75f, 0.35f, 1f, 1f);
        renderer.sortingLayerName = "Player";
        renderer.sortingOrder = 40;
        go.transform.localScale = Vector3.one * 0.6f;

        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.42f;

        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.linearVelocity = new Vector2(Mathf.Sign(direction) * 9.5f, 0f);

        MagicBolt bolt = go.AddComponent<MagicBolt>();
        bolt.damage = damage;

        MagicFX.AddSparkles(go.transform, new Color(0.7f, 0.35f, 1f, 0.9f), 0.15f, 25f);
        AudioManager.PlaySfx(AudioManager.Lib != null ? AudioManager.Lib.magicBolt : null);

        Destroy(go, 2f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if (other.GetComponentInParent<PlayerController>() != null)
            return;

        IEnemigo enemy = other.GetComponentInParent<IEnemigo>();
        if (enemy == null)
            enemy = other.GetComponent<IEnemigo>();

        if (enemy != null)
        {
            hasHit = true;
            enemy.TakeDamage(damage);
            MagicFX.Burst(transform.position, new Color(0.7f, 0.35f, 1f, 1f), 20, 0.2f);
            Destroy(gameObject);
        }
        else if (other.gameObject.layer == GroundLayer)
        {
            Destroy(gameObject);
        }
    }
}
