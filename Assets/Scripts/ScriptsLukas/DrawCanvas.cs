using UnityEngine;

[RequireComponent(typeof(MeshCollider), typeof(Draw))]
public class DrawCanvas : PenSurface
{
    public Draw Drawing { get; private set; }

    private void Awake()
    {
        Drawing = GetComponent<Draw>();
    }

    protected override Vector2Int PixelAt(Vector2 uv) => Drawing.UVToPixel(uv);

    public override bool DrawPenLine(Vector2Int start, Vector2Int end, Color color)
    {
        Drawing.PaintLine(start, end, color);
        return true;
    }

    public bool TryGetPixel(Vector3 tipPosition, Vector3 direction,
                            float tolerance, out Vector2Int pixel)
    {
        return TryGetContact(tipPosition, tolerance, out pixel, out _);
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
