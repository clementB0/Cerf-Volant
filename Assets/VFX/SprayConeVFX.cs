using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SprayInput))]
[RequireComponent(typeof(SprayColor))]
public class SprayConeVFX : MonoBehaviour
{
    [Header("Référence")]
    [SerializeField]
    private ParticleSystem sprayParticles;

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

    private ParticleSystem.Particle[] particleBuffer;
    private Color previousColor;
    private bool hasColor;

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

        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0.55f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        var colorOverLifetime = sprayParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = fade;

        particleBuffer =
            new ParticleSystem.Particle[main.maxParticles];
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
        Color color = sprayColor.PaintColor;

        var main = sprayParticles.main;
        main.startColor = color;
        main.startSpeed = particleSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.4f, particleSize);

        main.startLifetime =
            sprayRange / Mathf.Max(0.1f, particleSpeed);

        var emission = sprayParticles.emission;
        emission.rateOverTime = particlesPerSecond;

        var shape = sprayParticles.shape;
        shape.angle = coneHalfAngle;

        if (!hasColor || previousColor != color)
        {
            int count =
                sprayParticles.GetParticles(particleBuffer);

            for (int i = 0; i < count; i++)
                particleBuffer[i].startColor = color;

            sprayParticles.SetParticles(
                particleBuffer,
                count
            );

            previousColor = color;
            hasColor = true;
        }
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