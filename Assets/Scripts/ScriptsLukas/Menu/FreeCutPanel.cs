using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(RawImage), typeof(Mask))]
public sealed class FreeCutPanel : MonoBehaviour
{
    private const float PixelsPerMeter = 1200f;

    [SerializeField] private Transform cutterTip;
    [SerializeField, Min(0f)] private float contactDepth = 0.035f;
    [SerializeField, Min(0.001f)] private float cutWidth = 0.004f;

    private XRGrabInteractable cutter;
    private RawImage surface;
    private Texture2D cutMask;
    private Vector3? previousTip;
    private Rect panelRect;
    private Vector2Int resolution;
    private float localContactDepth;

    private void Awake()
    {
        surface = GetComponent<RawImage>();
        if (cutterTip != null)
            cutter = cutterTip.GetComponentInParent<XRGrabInteractable>();
    }

    private void Start()
    {
        panelRect = surface.rectTransform.rect;
        Vector3 scale = transform.lossyScale;
        resolution = new Vector2Int(
            Mathf.Max(1, Mathf.CeilToInt(panelRect.width * Mathf.Abs(scale.x) * PixelsPerMeter)),
            Mathf.Max(1, Mathf.CeilToInt(panelRect.height * Mathf.Abs(scale.y) * PixelsPerMeter)));
        localContactDepth = contactDepth / Mathf.Abs(scale.z);
    }

    private void LateUpdate()
    {
        if (cutter == null || !cutter.isSelected)
        {
            previousTip = null;
            return;
        }

        Vector3 current = transform.InverseTransformPoint(cutterTip.position);
        Vector3 previous = previousTip ?? current;
        previousTip = current;

        // Keep only the part of the motion that actually passed through the sheet's contact zone.
        if (!ClipToDepth(previous.z, current.z, localContactDepth, out float enter, out float exit))
            return;

        Vector2 from = ToPixel(Vector3.LerpUnclamped(previous, current, enter));
        Vector2 to = ToPixel(Vector3.LerpUnclamped(previous, current, exit));
        CutStroke(from, to);
    }

    private static bool ClipToDepth(float from, float to, float depth, out float enter, out float exit)
    {
        enter = 0f;
        exit = 1f;
        float delta = to - from;
        if (Mathf.Abs(delta) < 0.00001f)
            return Mathf.Abs(from) <= depth;

        float first = (-depth - from) / delta;
        float second = (depth - from) / delta;
        enter = Mathf.Max(0f, Mathf.Min(first, second));
        exit = Mathf.Min(1f, Mathf.Max(first, second));
        return enter <= exit;
    }

    private Vector2 ToPixel(Vector3 local)
    {
        return new Vector2(
            (local.x - panelRect.xMin) / panelRect.width * resolution.x,
            (local.y - panelRect.yMin) / panelRect.height * resolution.y);
    }

    private void CutStroke(Vector2 from, Vector2 to)
    {
        float radius = cutWidth * PixelsPerMeter * 0.5f;
        int left = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(from.x, to.x) - radius));
        int right = Mathf.Min(resolution.x - 1, Mathf.FloorToInt(Mathf.Max(from.x, to.x) + radius));
        int bottom = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(from.y, to.y) - radius));
        int top = Mathf.Min(resolution.y - 1, Mathf.FloorToInt(Mathf.Max(from.y, to.y) + radius));
        if (left > right || bottom > top)
            return;

        if (cutMask == null)
        {
            cutMask = new Texture2D(resolution.x, resolution.y, TextureFormat.RGBA32, false, true)
            {
                name = "Menu cut mask",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var initialPixels = cutMask.GetRawTextureData<Color32>();
            var white = new Color32(255, 255, 255, 255);
            for (int i = 0; i < initialPixels.Length; i++)
                initialPixels[i] = white;
            cutMask.Apply(false);
            surface.texture = cutMask;
        }

        // The texture holds the result directly, so the number of strokes does not grow memory.
        var pixels = cutMask.GetRawTextureData<Color32>();
        Vector2 direction = to - from;
        float lengthSquared = direction.sqrMagnitude;
        float radiusSquared = radius * radius;
        var transparent = new Color32(255, 255, 255, 0);
        bool changed = false;
        for (int y = bottom; y <= top; y++)
        {
            for (int x = left; x <= right; x++)
            {
                int index = y * resolution.x + x;
                if (pixels[index].a == 0)
                    continue;

                Vector2 offset = new Vector2(x + 0.5f, y + 0.5f) - from;
                float t = lengthSquared > 0f ? Mathf.Clamp01(Vector2.Dot(offset, direction) / lengthSquared) : 0f;
                if ((offset - t * direction).sqrMagnitude > radiusSquared)
                    continue;

                pixels[index] = transparent;
                changed = true;
            }
        }

        if (changed)
            cutMask.Apply(false);
    }

    private void OnDisable()
    {
        previousTip = null;
    }

    private void OnDestroy()
    {
        if (cutMask != null)
            Destroy(cutMask);
    }
}
