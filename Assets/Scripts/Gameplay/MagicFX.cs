using UnityEngine;

/// <summary>
/// Procedural magic effects (soft glow + sparkles) so no extra art or prefabs are needed.
/// </summary>
public static class MagicFX
{
    private const string FxSortingLayer = "Player";

    private static Sprite glowSprite;
    private static Material particleMaterial;

    /// <summary>Soft white circle, 1 world unit wide. Tint it with the SpriteRenderer color.</summary>
    public static Sprite GlowSprite
    {
        get
        {
            if (glowSprite == null)
                glowSprite = CreateGlowSprite();
            return glowSprite;
        }
    }

    private static Sprite CreateGlowSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(size / 2f, size / 2f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / (size / 2f);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha *= alpha;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Material ParticleMaterial
    {
        get
        {
            if (particleMaterial == null)
            {
                // Sprites/Default is always included in builds; the others are fallbacks.
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("UI/Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

                if (shader != null)
                {
                    particleMaterial = new Material(shader);
                    particleMaterial.mainTexture = GlowSprite.texture;
                }
            }

            return particleMaterial;
        }
    }

    /// <summary>Adds a pulsing glow child behind the given object.</summary>
    public static SpriteRenderer AddGlow(Transform parent, Color color, float diameter, float pulse = 0.12f)
    {
        GameObject go = new GameObject("MagicGlow");
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one * diameter;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = GlowSprite;
        renderer.color = color;
        ApplySorting(renderer, parent.GetComponentInParent<SpriteRenderer>(), -1);

        if (pulse > 0f)
        {
            Bobbing bobbing = go.AddComponent<Bobbing>();
            bobbing.amplitude = 0f;
            bobbing.pulse = pulse;
            bobbing.speed = 3f;
        }

        return renderer;
    }

    /// <summary>Continuous sparkles around an object.</summary>
    public static ParticleSystem AddSparkles(Transform parent, Color color, float radius, float rate)
    {
        GameObject go = new GameObject("MagicSparkles");
        go.transform.SetParent(parent, false);

        ParticleSystem system = BuildParticles(go, color, radius, true);
        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = rate;
        system.Play();

        SetParticleSorting(go, parent.GetComponentInParent<SpriteRenderer>());
        return system;
    }

    /// <summary>One-shot burst of sparkles at a world position. Cleans itself up.</summary>
    public static void Burst(Vector3 position, Color color, int count, float radius)
    {
        GameObject go = new GameObject("MagicBurst");
        go.transform.position = position;

        ParticleSystem system = BuildParticles(go, color, radius, false);
        system.Play();
        system.Emit(count);

        SetParticleSorting(go, null);
        Object.Destroy(go, 3f);
    }

    private static ParticleSystem BuildParticles(GameObject go, Color color, float radius, bool loop)
    {
        ParticleSystem system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.loop = loop;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.05f;
        main.maxParticles = 150;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;

        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        if (ParticleMaterial != null)
            go.GetComponent<ParticleSystemRenderer>().material = ParticleMaterial;

        return system;
    }

    private static void SetParticleSorting(GameObject go, SpriteRenderer reference)
    {
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = FxSortingLayer;
        renderer.sortingOrder = reference != null ? reference.sortingOrder + 5 : 40;
    }

    private static void ApplySorting(SpriteRenderer renderer, SpriteRenderer reference, int orderOffset)
    {
        if (reference != null)
        {
            renderer.sortingLayerID = reference.sortingLayerID;
            renderer.sortingOrder = reference.sortingOrder + orderOffset;
        }
        else
        {
            renderer.sortingLayerName = FxSortingLayer;
            renderer.sortingOrder = 35 + orderOffset;
        }
    }
}
