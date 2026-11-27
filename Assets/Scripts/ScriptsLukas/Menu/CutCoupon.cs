using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PatronPointilles))]
public class CutCoupon : MonoBehaviour
{
    [SerializeField] private MeshFilter upperPiece;
    [SerializeField] private MeshFilter lowerPiece;

    private PatronPointilles pattern;
    private Vector3 acrossCut;
    private Mesh upperMesh;
    private Mesh lowerMesh;

    private void Awake()
    {
        pattern = GetComponent<PatronPointilles>();
        var corners = pattern.TraceOuvert;
        if (corners == null || corners.Count != 2 || corners[0].x >= corners[1].x || corners[0].y >= corners[1].y)
        {
            Debug.LogError("Coupon : le tracé doit relier le coin inférieur gauche au coin supérieur droit.", this);
            return;
        }

        Vector2 start = corners[0];
        Vector2 end = corners[1];
        acrossCut = Vector2.Perpendicular((end - start).normalized);
        upperMesh = BuildPiece(upperPiece, start, end, new Vector2(start.x, end.y));
        lowerMesh = BuildPiece(lowerPiece, start, new Vector2(end.x, start.y), end);
    }

    private static Mesh BuildPiece(MeshFilter piece, Vector2 a, Vector2 b, Vector2 c)
    {
        const float thickness = 0.009f;
        Vector2 center = (a + b + c) / 3f;
        piece.transform.localPosition = center;
        Vector3 frontA = a - center;
        Vector3 frontB = b - center;
        Vector3 frontC = c - center;
        Vector3 depth = Vector3.forward * thickness;

        var mesh = new Mesh { name = piece.name };
        mesh.vertices = new[] { frontA, frontB, frontC, frontA + depth, frontB + depth, frontC + depth };
        mesh.triangles = new[] { 0, 2, 1, 3, 4, 5, 0, 1, 4, 0, 4, 3, 1, 2, 5, 1, 5, 4, 2, 0, 3, 2, 3, 5 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        piece.sharedMesh = mesh;
        return mesh;
    }

    public IEnumerator Separate()
    {
        if (upperMesh == null || lowerMesh == null)
            yield break;

        pattern.MasquerTrace();
        Vector3 upperStart = upperPiece.transform.localPosition;
        Vector3 lowerStart = lowerPiece.transform.localPosition;
        Vector3 upperOffset = PaperSeparationMotion.Offset(acrossCut, false);
        Vector3 lowerOffset = PaperSeparationMotion.Offset(-acrossCut, true);
        Quaternion upperTilt = PaperSeparationMotion.Tilt(false);
        Quaternion lowerTilt = PaperSeparationMotion.Tilt(true);

        for (float elapsed = 0f; elapsed < PaperSeparationMotion.Duration; elapsed += Time.unscaledDeltaTime)
        {
            float travel = PaperSeparationMotion.Progress(elapsed);
            upperPiece.transform.SetLocalPositionAndRotation(upperStart + upperOffset * travel,
                Quaternion.Slerp(Quaternion.identity, upperTilt, travel));
            lowerPiece.transform.SetLocalPositionAndRotation(lowerStart + lowerOffset * travel,
                Quaternion.Slerp(Quaternion.identity, lowerTilt, travel));
            yield return null;
        }

        upperPiece.transform.SetLocalPositionAndRotation(upperStart + upperOffset, upperTilt);
        lowerPiece.transform.SetLocalPositionAndRotation(lowerStart + lowerOffset, lowerTilt);
        yield return new WaitForSecondsRealtime(0.35f);
    }

    private void OnDestroy()
    {
        Destroy(upperMesh);
        Destroy(lowerMesh);
    }
}
