using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public abstract class PenSurface : MonoBehaviour
{
    private static readonly Vector3[] Axes =
    {
        Vector3.right, Vector3.left, Vector3.up,
        Vector3.down, Vector3.forward, Vector3.back
    };
    private MeshCollider contactCollider;

    protected virtual bool ReadyForPen => true;
    protected abstract Vector2Int PixelAt(Vector2 uv);
    public abstract bool DrawPenLine(Vector2Int start, Vector2Int end, Color color);

    public bool TryGetContact(Vector3 tip, float tolerance, out Vector2Int pixel,
                              out float squaredDistance)
    {
        return TryGetContact(tip, tolerance, out pixel, out squaredDistance, out _);
    }

    public bool TryGetContact(Vector3 tip, float tolerance, out Vector2Int pixel,
                              out float squaredDistance, out RaycastHit contact)
    {
        pixel = default;
        squaredDistance = float.PositiveInfinity;
        contact = default;
        if (!isActiveAndEnabled || !ReadyForPen)
            return false;

        if (contactCollider == null)
            contactCollider = GetComponent<MeshCollider>();
        if (!contactCollider.enabled)
            return false;

        foreach (Vector3 localAxis in Axes)
        {
            Vector3 axis = transform.TransformDirection(localAxis);
            Ray ray = new Ray(tip + axis * tolerance, -axis);
            if (!contactCollider.Raycast(ray, out var hit, tolerance * 2f))
                continue;

            float distance = (hit.point - tip).sqrMagnitude;
            if (distance < squaredDistance)
            {
                squaredDistance = distance;
                pixel = PixelAt(hit.textureCoord);
                contact = hit;
            }
        }
        return !float.IsPositiveInfinity(squaredDistance);
    }
}
