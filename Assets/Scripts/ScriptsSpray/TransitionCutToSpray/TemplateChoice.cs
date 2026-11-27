using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
public class TemplateChoice : MonoBehaviour
{
    [SerializeField] private XRInteractionManager interactionManager;
    [SerializeField] private Shader guideShader;
    [SerializeField] private TMP_FontAsset labelFont;
    [SerializeField] private AudioClip selectionSound;
    [SerializeField, Min(0.01f)] private float spacing = 0.24f;
    [SerializeField, Min(0.01f)] private float previewSize = 0.19f;
    [SerializeField] private bool showCreative = true;
    [SerializeField, Min(1f)] private float previewDashScale = 1f;

    private static readonly string[] Labels = { "Géométrique", "Symétrique", "Créatif", "Vierge" };
    private static readonly KitePattern.Style[] FullLayout =
        { KitePattern.Style.Geometric, KitePattern.Style.Symmetric, KitePattern.Style.Creative, KitePattern.Style.Blank };
    private static readonly KitePattern.Style[] CenteredBlankLayout =
        { KitePattern.Style.Geometric, KitePattern.Style.Blank, KitePattern.Style.Symmetric };
    private static readonly int GuideMap = Shader.PropertyToID("_GuideMap");

    private GameObject previews;
    private PieceDecoupee piece;
    private MeshRenderer surface;
    private MeshCollider paintCollider;
    private Material guideMaterial;
    private GameObject activeGuide;
    private Texture2D activePattern;
    private readonly List<Texture2D> previewPatterns = new();

    public bool HasChosen { get; private set; }

    public void Show(PieceDecoupee selectedPiece)
    {
        if (piece != null || HasChosen)
            return;

        piece = selectedPiece;
        Mesh mesh = piece.GetComponent<MeshFilter>().sharedMesh;
        surface = piece.GetComponent<MeshRenderer>();
        paintCollider = piece.GetComponent<MeshCollider>();
        guideMaterial = new Material(guideShader) { name = "Kite guide" };

        previews = new GameObject("Template previews");
        previews.SetActive(false);
        previews.transform.SetParent(transform, false);
        KitePattern.Style[] layout = showCreative ? FullLayout : CenteredBlankLayout;
        for (int i = 0; i < layout.Length; i++)
            CreatePreview(layout[i], (i - (layout.Length - 1) * 0.5f) * spacing, mesh);

        AddLabel(previews.transform, "Choisis un motif", new Vector3(0f, 0.14f, 0f), 0.38f, 0.5f);
        surface.enabled = false;
        paintCollider.enabled = false;
        previews.SetActive(true);
    }

    private void CreatePreview(KitePattern.Style style, float positionX, Mesh mesh)
    {
        string label = Labels[(int)style];
        GameObject option = new GameObject(label);
        option.transform.SetParent(previews.transform, false);
        option.transform.localPosition = Vector3.right * positionX;

        GameObject kite = new GameObject("Preview");
        kite.transform.SetParent(option.transform, false);
        float scale = previewSize / Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y);
        kite.transform.localScale = Vector3.one * scale;
        kite.transform.localPosition = -mesh.bounds.center * scale;

        kite.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = kite.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = surface.sharedMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;

        BoxCollider hitbox = option.AddComponent<BoxCollider>();
        hitbox.size = new Vector3(mesh.bounds.size.x * scale + 0.026f, 0.23f, 0.03f);
        hitbox.center = new Vector3(0f, -0.02f, 0f);

        if (style != KitePattern.Style.Blank)
        {
            Texture2D pattern = KitePattern.Create(mesh, style, previewDashScale);
            previewPatterns.Add(pattern);
            AddGuide(kite.transform, mesh, pattern);
        }
        AddLabel(option.transform, label, new Vector3(0f, -0.111f, -0.003f), 0.30f, spacing);

        XRSimpleInteractable interactable = option.AddComponent<XRSimpleInteractable>();
        interactable.interactionManager = interactionManager;
        interactable.colliders.Add(hitbox);

        var highlight = new MaterialPropertyBlock();
        highlight.SetColor("_BaseColor", new Color(0.81f, 0.93f, 1f));
        interactable.firstHoverEntered.AddListener(_ => renderer.SetPropertyBlock(highlight));
        interactable.lastHoverExited.AddListener(_ => renderer.SetPropertyBlock(null));
        interactable.selectEntered.AddListener(args => Choose(style, args, option.transform.position));
    }

    private void AddLabel(Transform parent, string text, Vector3 position, float size, float width)
    {
        var label = new GameObject(text).AddComponent<TextMeshPro>();
        label.transform.SetParent(parent, false);
        label.transform.localPosition = position;
        label.font = labelFont;
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(width, 0.036f);
        label.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private void Choose(KitePattern.Style style, SelectEnterEventArgs args, Vector3 selectionPosition)
    {
        if (HasChosen)
            return;

        HasChosen = true;
        if (args.interactorObject is XRBaseInputInteractor controller)
            controller.SendHapticImpulse(0.3f, 0.1f);
        if (selectionSound != null)
            AudioSource.PlayClipAtPoint(selectionSound, selectionPosition, 0.6f);

        if (style != KitePattern.Style.Blank)
        {
            Mesh mesh = piece.GetComponent<MeshFilter>().sharedMesh;
            activePattern = KitePattern.Create(mesh, style);
            activeGuide = AddGuide(piece.transform, mesh, activePattern);
        }

        previews.SetActive(false);
        Destroy(previews);
        ReleasePreviewPatterns();
        surface.enabled = true;
        paintCollider.enabled = true;
    }

    private GameObject AddGuide(Transform parent, Mesh mesh, Texture2D pattern)
    {
        var guide = new GameObject("Paint guide");
        guide.transform.SetParent(parent, false);
        guide.transform.localPosition = Vector3.back * 0.002f;
        guide.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = guide.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = guideMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        var properties = new MaterialPropertyBlock();
        properties.SetTexture(GuideMap, pattern);
        renderer.SetPropertyBlock(properties);
        return guide;
    }

    private void LateUpdate()
    {
        if (activeGuide != null && piece.PriseFinaleAutorisee)
        {
            activeGuide.SetActive(false);
            Destroy(activeGuide);
        }
    }

    private void OnDestroy()
    {
        if (activeGuide != null)
            Destroy(activeGuide);
        ReleasePreviewPatterns();
        Destroy(activePattern);
        Destroy(guideMaterial);
    }

    private void ReleasePreviewPatterns()
    {
        foreach (Texture2D pattern in previewPatterns)
            Destroy(pattern);
        previewPatterns.Clear();
    }
}
