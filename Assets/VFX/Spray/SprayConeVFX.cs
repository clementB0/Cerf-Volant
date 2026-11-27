using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SprayInput))]
[RequireComponent(typeof(SprayColor))]
public class SprayConeVFX : MonoBehaviour
{
    [Header("Référence")]
    [SerializeField]
    private ParticleSystem sprayParticles;
    [SerializeField]
    private ParticleSystem sprayEffectPrefab;

    [Header("Forme du jet")]
    [SerializeField, Range(1f, 30f)]
    private float coneHalfAngle = 10f;

    [SerializeField, Min(0.1f)]
    private float sprayRange = 1.5f;

    [SerializeField, Min(0.1f)]
    private float particleSpeed = 8f;

    [Header("Aspect du jet")]
    [SerializeField, Min(1f)]
    private float particlesPerSecond = 400f;

    [SerializeField, Min(0.001f)]
    private float particleSize = 0.018f;

    private SprayInput sprayInput;
    private SprayColor sprayColor;
    private ParticleSystemRenderer tintedRenderer;
    private MaterialPropertyBlock tintProperties;
    private static readonly int EffectTint = Shader.PropertyToID("_COULEUR");

    // Le script de peinture lit ces mêmes réglages.
    public float ConeHalfAngle => coneHalfAngle;
    public float SprayRange => sprayRange;

    public Transform SprayOrigin =>
        sprayParticles != null
            ? sprayParticles.transform
            : null;

    private void Awake()
    {
        sprayInput = GetComponent<SprayInput>();
        sprayColor = GetComponent<SprayColor>();

        if (sprayParticles == null)
        {
            Debug.LogError(
                "SprayConeVFX : assigne SprayParticles.",
                this
            );

            enabled = false;
            return;
        }

        if (sprayEffectPrefab != null)
        {
            ParticleSystem oldParticles = sprayParticles;
            Transform nozzle = oldParticles.transform;
            sprayParticles = Instantiate(sprayEffectPrefab, nozzle.parent, false);
            sprayParticles.transform.SetLocalPositionAndRotation(nozzle.localPosition, nozzle.localRotation);
            oldParticles.gameObject.SetActive(false);
        }

        StopSpray();

        var main = sprayParticles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace =
            ParticleSystemSimulationSpace.World;

        main.gravityModifier = 0f;
        main.maxParticles = 1000;

        var emission = sprayParticles.emission;
        emission.enabled = true;
        emission.SetBursts(new ParticleSystem.Burst[0]);

        var shape = sprayParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 0.003f;

        // Seuls les jets en billboard utilisent l'atlas de pigment.
        var sheet = sprayParticles.textureSheetAnimation;
        var particleRenderer = sprayParticles.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer.sharedMaterial != null && particleRenderer.sharedMaterial.HasProperty(EffectTint))
        {
            tintedRenderer = particleRenderer;
            tintProperties = new MaterialPropertyBlock();
            tintedRenderer.GetPropertyBlock(tintProperties);
        }
        sheet.enabled = particleRenderer.renderMode != ParticleSystemRenderMode.Mesh;
        sheet.mode = ParticleSystemAnimationMode.Grid;
        sheet.numTilesX = 2;
        sheet.numTilesY = 2;
        sheet.animation = ParticleSystemAnimationType.WholeSheet;
        sheet.frameOverTime = 0f;
        sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 0.999f);

        var expansion = sprayParticles.sizeOverLifetime;
        expansion.enabled = true;
        expansion.separateAxes = false;
        expansion.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.18f),
            new Keyframe(0.35f, 0.65f),
            new Keyframe(1f, 1.45f)
        ));
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);

        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0.92f, 0f),
                new GradientAlphaKey(0.68f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        var colorOverLifetime = sprayParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = fade;
    }

    private void Update()
    {
        ApplySettings();

        if (sprayInput.IsSpraying)
        {
            if (!sprayParticles.isEmitting)
                sprayParticles.Play();
        }
        else if (sprayParticles.isEmitting)
        {
            sprayParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void ApplySettings()
    {
        var main = sprayParticles.main;
        Color paint = sprayColor.PaintColor;
        main.startColor = paint;
        if (tintedRenderer != null)
        {
            tintProperties.SetColor(EffectTint, paint);
            tintedRenderer.SetPropertyBlock(tintProperties);
        }
        main.startSpeed = new ParticleSystem.MinMaxCurve(particleSpeed * 0.78f, particleSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.65f, particleSize);

        main.startLifetime =
            sprayRange / Mathf.Max(0.1f, particleSpeed);

        var emission = sprayParticles.emission;
        emission.rateOverTime = particlesPerSecond;

        var shape = sprayParticles.shape;
        shape.angle = coneHalfAngle;
    }

    private void StopSpray()
    {
        if (sprayParticles == null)
            return;

        sprayParticles.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );
    }

    private void OnDisable()
    {
        StopSpray();
    }
}
