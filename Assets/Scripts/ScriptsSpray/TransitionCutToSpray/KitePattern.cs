using UnityEngine;

internal sealed class KitePattern
{
    internal enum Style { Geometric, Symmetric, Creative }

    private const int Resolution = 512;
    private const float Dash = 0.016f;
    private const float Period = 0.027f;
    private const float Radius = 1.45f;
    private readonly Color32[] pixels = new Color32[Resolution * Resolution];
    private readonly Vector3[] vertices;
    private readonly Bounds bounds;

    private KitePattern(Mesh mesh)
    {
        vertices = mesh.vertices;
        bounds = mesh.bounds;
    }

    internal static Texture2D Create(Mesh mesh, Style style)
    {
        var pattern = new KitePattern(mesh);
        switch (style)
        {
            case Style.Geometric:
                pattern.Stroke(false, new(0f, 0.71f), new(0.82f, 0.22f), new(0f, -0.46f),
                    new(-0.82f, 0.22f), new(0f, 0.71f));
                pattern.Stroke(false, new(-0.82f, 0.22f), new(0.82f, 0.22f));
                break;
            case Style.Symmetric:
                pattern.Stroke(false, new(0f, -0.39f), new(0f, 0.72f));
                pattern.Wing(-1f);
                pattern.Wing(1f);
                break;
            case Style.Creative:
                pattern.Stroke(true, new(-0.84f, 0.17f), new(-0.5f, 0.54f), new(-0.04f, 0.30f),
                    new(0.31f, -0.22f), new(0.63f, -0.01f), new(0.8f, 0.43f));
                pattern.Stroke(true, new(-0.35f, -0.37f), new(-0.51f, -0.13f), new(-0.36f, 0.08f),
                    new(-0.2f, -0.13f), new(-0.35f, -0.37f));
                break;
        }

        var texture = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, true, true)
        {
            name = style + " guide",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear
        };
        texture.SetPixels32(pattern.pixels);
        texture.Apply(true, true);
        return texture;
    }

    private void Wing(float side)
    {
        Stroke(true, new(0.13f * side, -0.2f), new(0.46f * side, 0.02f),
            new(0.82f * side, 0.43f), new(0.52f * side, 0.56f), new(0.13f * side, 0.30f));
    }

    // The workshop mesh is a planar fan. Vertex zero is its center and the
    // remaining vertices are the ordered contour. Fit each row to that contour.
    private Vector2 Fit(Vector2 point)
    {
        float y = point.y * (point.y < 0f ? -bounds.min.y : bounds.max.y);
        float axis = bounds.center.x;
        float halfWidth = bounds.extents.x;
        for (int i = 1; i < vertices.Length; i++)
        {
            Vector3 a = vertices[i];
            Vector3 b = vertices[i + 1 < vertices.Length ? i + 1 : 1];
            if ((a.y > y) == (b.y > y))
                continue;

            float x = Mathf.Lerp(a.x, b.x, (y - a.y) / (b.y - a.y));
            halfWidth = Mathf.Min(halfWidth, Mathf.Abs(x - axis));
        }

        return new Vector2(axis + point.x * halfWidth, y);
    }

    private void Stroke(bool curved, params Vector2[] points)
    {
        Vector2 previous = Fit(points[0]);
        float phase = 0f;
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector2 a = points[Mathf.Max(0, i - 1)];
            Vector2 b = points[i];
            Vector2 c = points[i + 1];
            Vector2 d = points[Mathf.Min(points.Length - 1, i + 2)];
            for (int sample = 1; sample <= 16; sample++)
            {
                float t = sample / 16f;
                Vector2 point = curved ? Curve(a, b, c, d, t) : Vector2.Lerp(b, c, t);
                Vector2 next = Fit(point);
                float length = Vector2.Distance(previous, next);
                for (float start = -phase; start < length; start += Period)
                {
                    float from = Mathf.Max(0f, start);
                    float to = Mathf.Min(length, start + Dash);
                    if (to > from)
                        Rasterize(Vector2.Lerp(previous, next, from / length),
                            Vector2.Lerp(previous, next, to / length));
                }
                phase = (phase + length) % Period;
                previous = next;
            }
        }
    }

    private static Vector2 Curve(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
    {
        return b + 0.5f * t * (c - a + t * (2f * a - 5f * b + 4f * c - d
            + t * (3f * b - a - 3f * c + d)));
    }

    private void Rasterize(Vector2 from, Vector2 to)
    {
        // PieceDecoupee maps local XY to its painting UV with a half-unit offset.
        Vector2 a = (from + Vector2.one * 0.5f) * Resolution;
        Vector2 b = (to + Vector2.one * 0.5f) * Resolution;
        Vector2 delta = b - a;
        float lengthSquared = delta.sqrMagnitude;
        const float padding = Radius + 0.5f;
        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - padding));
        int maxX = Mathf.Min(Resolution - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + padding));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - padding));
        int maxY = Mathf.Min(Resolution - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + padding));
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 pixel = new Vector2(x + 0.5f, y + 0.5f);
                float t = lengthSquared > 0f ? Mathf.Clamp01(Vector2.Dot(pixel - a, delta) / lengthSquared) : 0f;
                float distance = Vector2.Distance(pixel, a + delta * t);
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(padding - distance) * 255f);
                int index = y * Resolution + x;
                if (alpha > pixels[index].a)
                    pixels[index].a = alpha;
            }
        }
    }
}
