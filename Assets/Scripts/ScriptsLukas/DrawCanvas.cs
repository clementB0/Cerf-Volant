using UnityEngine;

[RequireComponent(typeof(MeshCollider), typeof(Draw))]
public class DrawCanvas : MonoBehaviour
{
    private static readonly Vector3[] LocalDirections =
    {
        Vector3.right, Vector3.left,
        Vector3.up, Vector3.down,
        Vector3.forward, Vector3.back
    };

    private MeshCollider surfaceCollider;

    public Draw Drawing { get; private set; }

    private void Awake()
    {
        surfaceCollider = GetComponent<MeshCollider>();
        Drawing = GetComponent<Draw>();
    }

    // La signature reste compatible avec ton Pen.cs actuel.
    public bool TryGetPixel(Vector3 tipPosition, Vector3 direction,
                            float tolerance, out Vector2Int pixel)
    {
        pixel = default;

        if (!isActiveAndEnabled || !surfaceCollider.enabled)
            return false;

        float bestDistance = float.PositiveInfinity;
        Vector2 bestUV = default;

        foreach (Vector3 localAxis in LocalDirections)
        {
            Vector3 axis = transform.TransformDirection(localAxis);
            Vector3 origin = tipPosition + axis * tolerance;
            Ray ray = new Ray(origin, -axis);

            if (!surfaceCollider.Raycast(ray, out RaycastHit hit, tolerance * 2f))
                continue;

            float distance = (hit.point - tipPosition).sqrMagnitude;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestUV = hit.textureCoord;
            }
        }

        if (float.IsPositiveInfinity(bestDistance))
            return false;

        pixel = Drawing.UVToPixel(bestUV);
        return true;
    }

    [ContextMenu("Tester un trait au centre")]
    private void TesterTrait()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("Lance Play avant de tester le trait.", this);
            return;
        }

        Vector2Int debut = Drawing.UVToPixel(new Vector2(0.25f, 0.25f));
        Vector2Int fin = Drawing.UVToPixel(new Vector2(0.75f, 0.75f));
        Drawing.PaintLine(debut, fin);
        Debug.Log("Trait de test envoyé à la texture de " + gameObject.name, this);
    }
}