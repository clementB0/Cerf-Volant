using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRInteractionManager))]
public class GrabOutline : MonoBehaviour
{
    [SerializeField, Min(0f)] private float distance = 0.12f;
    [SerializeField] private Shader outlineShader;
    private XRInteractionManager manager;
    private Material mask, edge;
    private readonly List<IXRInteractor> hands = new();
    private readonly HashSet<XRGrabInteractable> drawn = new();
    private readonly List<MeshFilter> meshes = new();
    private MaterialPropertyBlock properties;
    private int offset;
    private static readonly Vector2[] Directions = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };

    private void Awake()
    {
        manager = GetComponent<XRInteractionManager>();
        properties = new MaterialPropertyBlock();
        offset = Shader.PropertyToID("_PixelOffset");
        mask = new Material(outlineShader) { renderQueue = 3100 };
        edge = new Material(outlineShader) { renderQueue = 3101 };
        mask.SetInt("_ColorMask", 0);
        mask.SetInt("_StencilPass", (int)StencilOp.Replace);
        edge.SetInt("_StencilTest", (int)CompareFunction.NotEqual);
    }

    private void LateUpdate()
    {
        drawn.Clear();
        manager.GetRegisteredInteractors(hands);
        foreach (var item in hands)
        {
            if (item is not XRBaseInputInteractor hand || hand.hasSelection)
                continue;
            foreach (var hovered in hand.interactablesHovered)
            {
                // (Pas de contour sur un pot posé sur le pistolet : il clignotait en blanc quand la main s'approchait.)
                if (hovered is XRGrabInteractable grab && !grab.isSelected && !drawn.Contains(grab) &&
                    !(grab.TryGetComponent(out PaintReservoir pot) && pot.OnRail) &&
                    grab.GetDistance(hand.transform.position).distanceSqr <= distance * distance &&
                    manager.IsSelectPossible(hand, grab))
                {
                    drawn.Add(grab);
                    Draw(grab);
                }
            }
        }
    }

    private void Draw(XRGrabInteractable grab)
    {
        grab.GetComponentsInChildren(false, meshes);
        foreach (var filter in meshes)
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null || !filter.TryGetComponent<MeshRenderer>(out var renderer) ||
                !renderer.enabled || renderer.forceRenderingOff || filter.GetComponentInParent<XRGrabInteractable>() != grab)
                continue;
            var settings = new RenderParams(mask) { layer = filter.gameObject.layer, worldBounds = renderer.bounds,
                shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false };
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                settings.material = mask;
                settings.matProps = null;
                Graphics.RenderMesh(settings, mesh, sub, renderer.localToWorldMatrix);
                settings.material = edge;
                settings.matProps = properties;
                foreach (Vector2 direction in Directions)
                {
                    properties.SetVector(offset, new Vector4(direction.x, direction.y, 0, 0));
                    Graphics.RenderMesh(settings, mesh, sub, renderer.localToWorldMatrix);
                }
            }
        }
    }

    private void OnDestroy()
    {
        Destroy(mask);
        Destroy(edge);
    }
}
