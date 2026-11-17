using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer), typeof(MeshCollider))]
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

    [Header("Validation de la peinture")]
    [SerializeField]
    private bool suivreCouverture = false;

    [SerializeField, Range(0.1f, 1f)]
    private float couvertureRequise = 0.98f;

    [Tooltip("Quantité de peinture nécessaire pour compter un pixel.")]
    [SerializeField, Range(0.05f, 0.95f)]
    private float opaciteRequise = 0.5f;

    [SerializeField]
    private UnityEvent quandPeintureTerminee = new UnityEvent();

    [Header("Suivi pendant le jeu — ne pas modifier")]
    [SerializeField, Range(0f, 100f)]
    private float progressionPourcent;

    [SerializeField]
    private bool peintureTerminee;

    private MeshRenderer surfaceRenderer;
    private MeshCollider surfaceCollider;

    private Material originalMaterial;
    private Material runtimeMaterial;
    private Texture2D paintTexture;

    private Color[] pixels;

    // Pixels qui appartiennent réellement au maillage.
    private bool[] masqueSurface;

    // Quantité de peinture déposée sur chaque pixel.
    private float[] depotPeinture;

    // Empêche de compter deux fois le même pixel.
    private bool[] pixelsComptes;

    private int nombrePixelsSurface;
    private int nombrePixelsPeints;

    private bool textureDirty;

    private void Start()
    {
        // La pièce construit son maillage dans Awake.
        // On initialise donc la peinture dans Start.
        surfaceRenderer = GetComponent<MeshRenderer>();
        surfaceCollider = GetComponent<MeshCollider>();

        originalMaterial = surfaceRenderer.sharedMaterial;
        Mesh mesh = surfaceCollider.sharedMesh;

        if (originalMaterial == null ||
            mesh == null ||
            surfaceRenderer.sharedMaterials.Length != 1 ||
            surfaceCollider.convex)
        {
            Debug.LogError(
                "PaintableSurface : il faut un seul matériau et " +
                "un MeshCollider non Convex avec un maillage.",
                this
            );

            enabled = false;
            return;
        }

        if (!mesh.isReadable)
        {
            Debug.LogError(
                "PaintableSurface : le maillage doit être lisible.",
                this
            );

            enabled = false;
            return;
        }

        Vector2[] uv = mesh.uv;

        if (uv.Length != mesh.vertexCount)
        {
            Debug.LogError(
                "PaintableSurface : le maillage ne possède pas d'UV valides.",
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
                "PaintableSurface : le matériau doit posséder " +
                "une texture _BaseMap ou _MainTex.",
                this
            );

            enabled = false;
            return;
        }

        int nombrePixels = resolution * resolution;

        pixels = new Color[nombrePixels];
        masqueSurface = new bool[nombrePixels];
        depotPeinture = new float[nombrePixels];
        pixelsComptes = new bool[nombrePixels];

        for (int i = 0; i < nombrePixels; i++)
            pixels[i] = backgroundColor;

        ConstruireMasque(uv, mesh.triangles);

        if (nombrePixelsSurface == 0)
        {
            Debug.LogError(
                "PaintableSurface : aucune surface détectée dans les UV.",
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

        runtimeMaterial.SetTexture(textureProperty, paintTexture);
        runtimeMaterial.SetTextureScale(textureProperty, Vector2.one);
        runtimeMaterial.SetTextureOffset(textureProperty, Vector2.zero);

        if (runtimeMaterial.HasProperty("_BaseColor"))
            runtimeMaterial.SetColor("_BaseColor", Color.white);

        if (runtimeMaterial.HasProperty("_Color"))
            runtimeMaterial.SetColor("_Color", Color.white);

        surfaceRenderer.sharedMaterial = runtimeMaterial;

        progressionPourcent = 0f;
        peintureTerminee = false;

        textureDirty = true;
        ActualiserTexture();
    }

    private void ConstruireMasque(Vector2[] uv, int[] triangles)
    {
        // Parcourt les triangles dans la texture.
        // Seuls les pixels à l'intérieur sont comptabilisés.
        for (int t = 0; t < triangles.Length; t += 3)
        {
            Vector2 a = uv[triangles[t]];
            Vector2 b = uv[triangles[t + 1]];
            Vector2 c = uv[triangles[t + 2]];

            float aire = ProduitCroise(b - a, c - a);

            if (Mathf.Abs(aire) < 0.00000001f)
                continue;

            int minX = Mathf.Clamp(
                Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)) * resolution),
                0, resolution - 1
            );

            int maxX = Mathf.Clamp(
                Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)) * resolution),
                0, resolution - 1
            );

            int minY = Mathf.Clamp(
                Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)) * resolution),
                0, resolution - 1
            );

            int maxY = Mathf.Clamp(
                Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)) * resolution),
                0, resolution - 1
            );

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 point = new Vector2(
                        (x + 0.5f) / resolution,
                        (y + 0.5f) / resolution
                    );

                    float poidsA = ProduitCroise(b - point, c - point) / aire;
                    float poidsB = ProduitCroise(c - point, a - point) / aire;
                    float poidsC = 1f - poidsA - poidsB;

                    if (poidsA < -0.000001f ||
                        poidsB < -0.000001f ||
                        poidsC < -0.000001f)
                    {
                        continue;
                    }

                    int index = y * resolution + x;

                    if (!masqueSurface[index])
                    {
                        masqueSurface[index] = true;
                        nombrePixelsSurface++;
                    }
                }
            }
        }
    }

    private static float ProduitCroise(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    public void Paint(RaycastHit hit, Color color, float opacity)
    {
        if (!isActiveAndEnabled ||
            paintTexture == null ||
            peintureTerminee ||
            hit.collider != surfaceCollider)
        {
            return;
        }

        Vector2 uv = hit.textureCoord;

        int centreX = Mathf.Clamp(
            Mathf.FloorToInt(uv.x * resolution),
            0, resolution - 1
        );

        int centreY = Mathf.Clamp(
            Mathf.FloorToInt(uv.y * resolution),
            0, resolution - 1
        );

        int rayon = Mathf.Max(1, brushRadiusPixels);

        for (int y = -rayon; y <= rayon; y++)
        {
            for (int x = -rayon; x <= rayon; x++)
            {
                int pixelX = centreX + x;
                int pixelY = centreY + y;

                if (pixelX < 0 || pixelX >= resolution ||
                    pixelY < 0 || pixelY >= resolution)
                {
                    continue;
                }

                float distance = Mathf.Sqrt(x * x + y * y) / rayon;

                if (distance >= 1f)
                    continue;

                int index = pixelY * resolution + pixelX;

                if (!masqueSurface[index])
                    continue;

                float force = Mathf.Clamp01(
                    opacity * color.a * (1f - distance)
                );

                if (force <= 0f)
                    continue;

                Color couleurCible = new Color(
                    color.r,
                    color.g,
                    color.b,
                    pixels[index].a
                );

                pixels[index] = Color.Lerp(
                    pixels[index],
                    couleurCible,
                    force
                );

                if (suivreCouverture)
                {
                    // Même du blanc sur du blanc dépose de la peinture.
                    depotPeinture[index] =
                        1f - (1f - depotPeinture[index]) * (1f - force);

                    if (!pixelsComptes[index] &&
                        depotPeinture[index] >= opaciteRequise)
                    {
                        pixelsComptes[index] = true;
                        nombrePixelsPeints++;
                    }
                }

                textureDirty = true;
            }
        }
    }

    private void LateUpdate()
    {
        ActualiserTexture();

        if (!suivreCouverture ||
            peintureTerminee ||
            nombrePixelsSurface == 0)
        {
            return;
        }

        float couverture =
            (float)nombrePixelsPeints / nombrePixelsSurface;

        progressionPourcent = couverture * 100f;

        if (couverture >= couvertureRequise)
        {
            peintureTerminee = true;

            Debug.Log(
                "Peinture du cerf-volant terminée !",
                this
            );

            quandPeintureTerminee.Invoke();
        }
    }

    private void ActualiserTexture()
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