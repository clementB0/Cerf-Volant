using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SprayInput))]
[RequireComponent(typeof(SprayColor))]
[RequireComponent(typeof(SprayConeVFX))]
public class SprayPainter : MonoBehaviour
{
    [Header("Surfaces autorisées")]
    [Tooltip("Glisse ici les objets de la scène possédant PaintableSurface.")]
    [SerializeField]
    private List<PaintableSurface> paintableSurfaces =
        new List<PaintableSurface>();

    [Header("Obstacles")]
    [Tooltip("Inclure tous les obstacles, même ceux non peignables.")]
    [SerializeField]
    private LayerMask obstacleMask = ~0;

    [Header("Pulvérisation")]
    [SerializeField, Range(1, 100)]
    private int raysPerTick = 40;

    [SerializeField, Range(1f, 60f)]
    private float ticksPerSecond = 30f;

    [SerializeField, Range(0.01f, 1f)]
    private float opacityPerImpact = 0.3f;

    private SprayInput sprayInput;
    private SprayColor sprayColor;
    private SprayConeVFX cone;

    private float nextPaintTime;

    private void Awake()
    {
        sprayInput = GetComponent<SprayInput>();
        sprayColor = GetComponent<SprayColor>();
        cone = GetComponent<SprayConeVFX>();
    }

    private void Update()
    {
        if (!sprayInput.IsSpraying)
        {
            nextPaintTime = 0f;
            return;
        }

        if (!cone.isActiveAndEnabled)
            return;

        Transform origin = cone.SprayOrigin;

        if (origin == null ||
            Time.time < nextPaintTime)
        {
            return;
        }

        nextPaintTime =
            Time.time + 1f / Mathf.Max(1f, ticksPerSecond);

        PaintCone(origin);
    }

    private void PaintCone(Transform origin)
    {
        float spread = Mathf.Tan(
            cone.ConeHalfAngle * Mathf.Deg2Rad
        );

        for (int i = 0; i < raysPerTick; i++)
        {
            Vector2 offset =
                i == 0
                    ? Vector2.zero
                    : Random.insideUnitCircle;

            Vector3 localDirection = new Vector3(
                offset.x * spread,
                offset.y * spread,
                1f
            ).normalized;

            Vector3 direction =
                origin.rotation * localDirection;

            RaycastHit[] hits = Physics.RaycastAll(
                origin.position,
                direction,
                cone.SprayRange,
                obstacleMask,
                QueryTriggerInteraction.Ignore
            );

            bool found = false;
            RaycastHit closestHit = default;
            float closestDistance = float.PositiveInfinity;

            foreach (RaycastHit hit in hits)
            {
                // La bombe ne bloque pas son propre spray.
                if (hit.collider.transform.IsChildOf(transform))
                    continue;

                if (hit.distance >= closestDistance)
                    continue;

                closestDistance = hit.distance;
                closestHit = hit;
                found = true;
            }

            if (!found)
                continue;

            PaintableSurface surface =
                closestHit.collider.GetComponent<PaintableSurface>();

            // Le premier obstacle bloque le rayon,
            // même s'il n'est pas autorisé à recevoir de peinture.
            if (surface == null ||
                !paintableSurfaces.Contains(surface))
            {
                continue;
            }

            surface.Paint(
                closestHit,
                sprayColor.PaintColor,
                opacityPerImpact
            );
        }
    }
}