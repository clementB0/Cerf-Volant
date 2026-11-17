using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class PaintableSurface : MonoBehaviour
{
    [Header("Texture de peinture")]
    [SerializeField, Range(128, 2048)]
    private int resolution = 512;

    [SerializeField]
    private Color backgroundColor = Color.white;

    [Header("Pinceau")]
    [SerializeField, Range(1, 30)]
    private int brushRadiusPixels = 5;

    private MeshRenderer surfaceRenderer;
    private MeshCollider surfaceCollider;

    private Material originalMaterial;
    private Material runtimeMaterial;
    private Texture2D paintTexture;

    private Color[] pixels;
    private bool textureDirty;

    private void Awake()
    {
        surfaceRenderer = GetComponent<MeshRenderer>();
        surfaceCollider = GetComponent<MeshCollider>();

        originalMaterial = surfaceRenderer.sharedMaterial;

        if (originalMaterial == null ||
            surfaceCollider.sharedMesh == null ||
            surfaceRenderer.sharedMaterials.Length != 1)
        {
            Debug.LogError(
                "PaintableSurface : un mesh dans le MeshCollider " +
                "et un seul matériau sont nécessaires.",
                this
            );

            enabled = false;
            return;
        }

        string textureProperty =
            originalMaterial.HasProperty("_BaseMap")
                ? "_BaseMap"
                : "_MainTex";

        if (!originalMaterial.HasProperty(textureProperty))
        {
            Debug.LogError(
                "PaintableSurface : utilise un matériau URP/Lit " +
                "ou Standard avec une texture de base.",
                this
            );

            enabled = false;
            return;
        }

        runtimeMaterial = new Material(originalMaterial);

        paintTexture = new Texture2D(
            resolution,
            resolution,
            TextureFormat.RGBA32,
            false
        );

        paintTexture.name = name + "_Paint";
        paintTexture.wrapMode = TextureWrapMode.Clamp;
        paintTexture.filterMode = FilterMode.Bilinear;

        pixels = new Color[resolution * resolution];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = backgroundColor;

        runtimeMaterial.SetTexture(
            textureProperty,
            paintTexture
        );

        runtimeMaterial.SetTextureScale(
            textureProperty,
            Vector2.one
        );

        runtimeMaterial.SetTextureOffset(
            textureProperty,
            Vector2.zero
        );

        if (runtimeMaterial.HasProperty("_BaseColor"))
            runtimeMaterial.SetColor("_BaseColor", Color.white);
        else if (runtimeMaterial.HasProperty("_Color"))
            runtimeMaterial.SetColor("_Color", Color.white);

        surfaceRenderer.sharedMaterial = runtimeMaterial;

        textureDirty = true;
        UploadTexture();
    }

    public void Paint(
        RaycastHit hit,
        Color color,
        float opacity)
    {
        if (!isActiveAndEnabled ||
            paintTexture == null ||
            hit.collider != surfaceCollider)
        {
            return;
        }

        Vector2 uv = hit.textureCoord;

        int centerX = Mathf.RoundToInt(
            Mathf.Clamp01(uv.x) * (resolution - 1)
        );

        int centerY = Mathf.RoundToInt(
            Mathf.Clamp01(uv.y) * (resolution - 1)
        );

        int radius = Mathf.Max(1, brushRadiusPixels);

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                int pixelX = centerX + x;
                int pixelY = centerY + y;

                if (pixelX < 0 || pixelX >= resolution ||
                    pixelY < 0 || pixelY >= resolution)
                {
                    continue;
                }

                float distance =
                    Mathf.Sqrt(x * x + y * y) / radius;

                if (distance >= 1f)
                    continue;

                float strength = Mathf.Clamp01(
                    opacity * color.a * (1f - distance)
                );

                int index = pixelY * resolution + pixelX;

                Color targetColor = new Color(
                    color.r,
                    color.g,
                    color.b,
                    pixels[index].a
                );

                pixels[index] = Color.Lerp(
                    pixels[index],
                    targetColor,
                    strength
                );
            }
        }

        textureDirty = true;
    }

    private void LateUpdate()
    {
        UploadTexture();
    }

    private void UploadTexture()
    {
        if (!textureDirty || paintTexture == null)
            return;

        paintTexture.SetPixels(pixels);
        paintTexture.Apply(false, false);

        textureDirty = false;
    }

    private void OnDestroy()
    {
        if (surfaceRenderer != null &&
            runtimeMaterial != null &&
            surfaceRenderer.sharedMaterial == runtimeMaterial)
        {
            surfaceRenderer.sharedMaterial = originalMaterial;
        }

        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);

        if (paintTexture != null)
            Destroy(paintTexture);
    }
}