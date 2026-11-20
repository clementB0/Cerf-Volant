using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class Draw : MonoBehaviour
{
    [SerializeField, Min(2)] private int totalXPixels = 1024;
    [SerializeField, Min(2)] private int totalYPixels = 512;
    [SerializeField, Min(1)] private int brushSize = 4;
    [SerializeField] private Color brushColor = Color.black;

    private Texture2D texture;

    private void Awake()
    {
        texture = new Texture2D(totalXPixels, totalYPixels, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32[] background = new Color32[totalXPixels * totalYPixels];
        for (int i = 0; i < background.Length; i++)
            background[i] = Color.white;

        texture.SetPixels32(background);
        texture.Apply(false);

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(properties);
        properties.SetTexture("_BaseMap", texture);
        meshRenderer.SetPropertyBlock(properties);
    }

    public Vector2Int UVToPixel(Vector2 uv)
    {
        int x = Mathf.RoundToInt(Mathf.Clamp01(uv.x) * (texture.width - 1));
        int y = Mathf.RoundToInt(Mathf.Clamp01(uv.y) * (texture.height - 1));
        return new Vector2Int(x, y);
    }

    public void PaintLine(Vector2Int start, Vector2Int end)
    {
        int steps = Mathf.Max(Mathf.Abs(end.x - start.x), Mathf.Abs(end.y - start.y));

        Stamp(start.x, start.y);

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(start.y, end.y, t));
            Stamp(x, y);
        }

        texture.Apply(false);
    }

    private void Stamp(int centerX, int centerY)
    {
        int radius = brushSize;
        int minX = Mathf.Max(0, centerX - radius);
        int maxX = Mathf.Min(texture.width - 1, centerX + radius);
        int minY = Mathf.Max(0, centerY - radius);
        int maxY = Mathf.Min(texture.height - 1, centerY + radius);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;

                if (dx * dx + dy * dy <= radius * radius)
                    texture.SetPixel(x, y, brushColor);
            }
        }
    }

    private void OnDestroy()
    {
        if (texture != null)
            Destroy(texture);
    }
}