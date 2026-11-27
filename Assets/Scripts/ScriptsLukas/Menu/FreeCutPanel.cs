using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(RawImage), typeof(Mask))]
public sealed class FreeCutPanel : MonoBehaviour
{
    private const float PixelsPerMeter = 1200f;
    private const float RegionCheckInterval = 0.08f;
    private const float SoundInterval = 0.085f;
    private const int MinimumFragmentArea = 16;

    [SerializeField] private Transform cutterTip;
    [SerializeField, Min(0f)] private float contactDepth = 0.035f;
    [SerializeField, Min(0.001f)] private float cutWidth = 0.002f;
    [SerializeField] private AudioClip cutSound;
    [SerializeField, Range(0f, 1f)] private float cutVolume = 0.35f;

    private sealed class Piece
    {
        public RawImage Surface;
        public RectInt SheetPixels;
        public Texture2D Texture;
        public Vector3? PreviousTip;
        public bool NeedsRegionCheck;
        public float NextRegionCheck;
        public float MoveStarted;
        public bool Moving;
        public Vector3 StartPosition;
        public Quaternion StartRotation;
        public Vector3 MoveOffset;
        public Quaternion Tilt;

        public RectTransform Rect => Surface.rectTransform;
    }

    private readonly List<Piece> pieces = new();
    private XRGrabInteractable cutter;
    private AudioSource speaker;
    private float nextSoundTime;
    private RawImage originalSurface;
    private TextMeshProUGUI originalLabel;
    private Vector2Int sheetSize;
    private Vector2 unitsPerPixel;
    private float localContactDepth;
    private CutPaperRegions regionMap;

    private void Awake()
    {
        originalSurface = GetComponent<RawImage>();
        originalLabel = GetComponentInChildren<TextMeshProUGUI>();
        if (cutterTip != null)
            cutter = cutterTip.GetComponentInParent<XRGrabInteractable>();

        if (cutSound != null)
        {
            var soundObject = new GameObject("Cut sound");
            soundObject.transform.SetParent(transform, false);
            speaker = soundObject.AddComponent<AudioSource>();
            speaker.playOnAwake = false;
            speaker.spatialBlend = 1f;
        }
    }

    private void Start()
    {
        Rect rect = originalSurface.rectTransform.rect;
        Vector3 scale = transform.lossyScale;
        sheetSize = new Vector2Int(
            Mathf.Max(1, Mathf.CeilToInt(rect.width * Mathf.Abs(scale.x) * PixelsPerMeter)),
            Mathf.Max(1, Mathf.CeilToInt(rect.height * Mathf.Abs(scale.y) * PixelsPerMeter)));
        unitsPerPixel = new Vector2(rect.width / sheetSize.x, rect.height / sheetSize.y);
        localContactDepth = contactDepth / Mathf.Abs(scale.z);
        pieces.Add(new Piece
        {
            Surface = originalSurface,
            SheetPixels = new RectInt(Vector2Int.zero, sheetSize)
        });
    }

    private void LateUpdate()
    {
        bool held = cutter != null && cutter.isSelected;
        float now = Time.unscaledTime;
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            Piece piece = pieces[i];
            if (piece.Moving)
            {
                UpdateMotion(piece, now);
                continue;
            }

            if (held)
                TraceCutter(piece, now);
            else
                piece.PreviousTip = null;

            if (piece.NeedsRegionCheck && now >= piece.NextRegionCheck)
            {
                piece.NeedsRegionCheck = false;
                piece.NextRegionCheck = now + RegionCheckInterval;
                SeparateRegions(i, piece, now);
            }
        }
    }

    private void TraceCutter(Piece piece, float now)
    {
        Vector3 current = piece.Rect.InverseTransformPoint(cutterTip.position);
        Vector3 previous = piece.PreviousTip ?? current;
        piece.PreviousTip = current;
        if (!ClipToDepth(previous.z, current.z, localContactDepth, out float enter, out float exit))
            return;

        Rect rect = piece.Rect.rect;
        Vector2 from = ToPixel(Vector3.LerpUnclamped(previous, current, enter), rect);
        Vector2 to = ToPixel(Vector3.LerpUnclamped(previous, current, exit), rect);
        if (CutStroke(piece, from, to))
        {
            piece.NeedsRegionCheck = true;
            PlayCutSound(now);
        }
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

    private Vector2 ToPixel(Vector3 local, Rect rect)
    {
        return new Vector2((local.x - rect.xMin) / unitsPerPixel.x, (local.y - rect.yMin) / unitsPerPixel.y);
    }

    private bool CutStroke(Piece piece, Vector2 from, Vector2 to)
    {
        float radius = cutWidth * PixelsPerMeter * 0.5f;
        int width = piece.SheetPixels.width;
        int height = piece.SheetPixels.height;
        int left = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(from.x, to.x) - radius));
        int right = Mathf.Min(width - 1, Mathf.FloorToInt(Mathf.Max(from.x, to.x) + radius));
        int bottom = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(from.y, to.y) - radius));
        int top = Mathf.Min(height - 1, Mathf.FloorToInt(Mathf.Max(from.y, to.y) + radius));
        if (left > right || bottom > top)
            return false;

        if (piece.Texture == null)
        {
            piece.Texture = CreateMask(width, height);
            var initialPixels = piece.Texture.GetRawTextureData<Color32>();
            var white = new Color32(255, 255, 255, 255);
            for (int i = 0; i < initialPixels.Length; i++)
                initialPixels[i] = white;
            piece.Texture.Apply(false);
            piece.Surface.texture = piece.Texture;
        }

        var pixels = piece.Texture.GetRawTextureData<Color32>();
        Vector2 direction = to - from;
        float lengthSquared = direction.sqrMagnitude;
        float radiusSquared = radius * radius;
        var transparent = new Color32(255, 255, 255, 0);
        bool changed = false;
        for (int y = bottom; y <= top; y++)
        {
            for (int x = left; x <= right; x++)
            {
                int index = y * width + x;
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
            piece.Texture.Apply(false);
        return changed;
    }

    private void SeparateRegions(int index, Piece source, float now)
    {
        regionMap ??= new CutPaperRegions(sheetSize.x * sheetSize.y);
        var pixels = source.Texture.GetRawTextureData<Color32>();
        IReadOnlyList<CutPaperRegions.Region> regions = regionMap.Find(pixels, source.SheetPixels.width, source.SheetPixels.height);
        if (regions.Count == 1)
            return;

        var largest = default(CutPaperRegions.Region);
        var secondLargest = default(CutPaperRegions.Region);
        foreach (var region in regions)
        {
            if (region.Area > largest.Area)
            {
                secondLargest = largest;
                largest = region;
            }
            else if (region.Area > secondLargest.Area)
            {
                secondLargest = region;
            }
        }

        if (largest.Area > 0 && secondLargest.Area < MinimumFragmentArea)
        {
            // Remove isolated pixels at the edge of the slit without detaching a fragment.
            for (int i = 0; i < pixels.Length; i++)
            {
                if (regionMap.LabelAt(i) != largest.Id)
                    pixels[i] = new Color32(255, 255, 255, 0);
            }
            source.Texture.Apply(false);
            return;
        }

        foreach (var region in regions)
        {
            if (region.Area < MinimumFragmentArea)
                continue;

            bool peel = region.Id != largest.Id;
            Vector2 outward = region.Center - (peel ? largest.Center : secondLargest.Center);
            if (outward.sqrMagnitude < 0.001f)
                outward = peel ? Vector2.down : Vector2.up;

            pieces.Add(CreateFragment(source, region, pixels, outward.normalized, peel, now));
        }

        if (regions.Count > 1)
            PlayCutSound(now);

        if (source.Surface == originalSurface)
        {
            originalSurface.texture = null;
            originalSurface.enabled = false;
            GetComponent<Mask>().enabled = false;
            if (originalLabel != null)
                originalLabel.gameObject.SetActive(false);
        }
        else
        {
            source.Surface.gameObject.SetActive(false);
            Destroy(source.Surface.gameObject);
        }
        Destroy(source.Texture);
        pieces.RemoveAt(index);
    }

    private Piece CreateFragment(Piece source, CutPaperRegions.Region region, NativeArray<Color32> sourcePixels,
        Vector2 outward, bool peel, float now)
    {
        RectInt bounds = region.Bounds;
        RectInt sheetPixels = new RectInt(source.SheetPixels.position + bounds.position, bounds.size);
        Texture2D texture = CreateMask(bounds.width, bounds.height);
        var pixels = texture.GetRawTextureData<Color32>();
        var transparent = new Color32(255, 255, 255, 0);
        for (int y = 0; y < bounds.height; y++)
        {
            int sourceRow = (bounds.yMin + y) * source.SheetPixels.width + bounds.xMin;
            for (int x = 0; x < bounds.width; x++)
            {
                int sourceIndex = sourceRow + x;
                pixels[y * bounds.width + x] = regionMap.LabelAt(sourceIndex) == region.Id
                    ? sourcePixels[sourceIndex] : transparent;
            }
        }
        texture.Apply(false);

        var fragment = new GameObject("Paper fragment", typeof(RectTransform), typeof(RawImage), typeof(Mask));
        fragment.layer = gameObject.layer;
        var surface = fragment.GetComponent<RawImage>();
        surface.color = originalSurface.color;
        surface.raycastTarget = false;
        surface.texture = texture;
        RectTransform rect = surface.rectTransform;
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.Scale(bounds.size, unitsPerPixel);

        Vector2 fromSourceCenter = bounds.center - (Vector2)source.SheetPixels.size * 0.5f;
        Vector2 localCenter = source.Rect.rect.center + Vector2.Scale(fromSourceCenter, unitsPerPixel);
        Vector3 startPosition = source.Rect.TransformPoint(localCenter);
        Quaternion startRotation = source.Rect.rotation;
        rect.SetPositionAndRotation(startPosition, startRotation);

        if (originalLabel != null)
        {
            TextMeshProUGUI label = Instantiate(originalLabel, rect, false);
            Vector2 fromFragmentCenter = (Vector2)sheetSize * 0.5f - sheetPixels.center;
            label.rectTransform.anchoredPosition += Vector2.Scale(fromFragmentCenter, unitsPerPixel);
            label.gameObject.SetActive(true);
        }

        return new Piece
        {
            Surface = surface,
            SheetPixels = sheetPixels,
            Texture = texture,
            Moving = true,
            MoveStarted = now,
            StartPosition = startPosition,
            StartRotation = startRotation,
            MoveOffset = startRotation * PaperSeparationMotion.Offset(outward, peel),
            Tilt = PaperSeparationMotion.Tilt(peel)
        };
    }

    private static Texture2D CreateMask(int width, int height)
    {
        return new Texture2D(width, height, TextureFormat.RGBA32, false, true)
        {
            name = "Menu paper mask",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
    }

    private static void UpdateMotion(Piece piece, float now)
    {
        float elapsed = now - piece.MoveStarted;
        float travel = PaperSeparationMotion.Progress(elapsed);
        piece.Rect.SetPositionAndRotation(piece.StartPosition + piece.MoveOffset * travel,
            piece.StartRotation * Quaternion.Slerp(Quaternion.identity, piece.Tilt, travel));
        piece.Moving = elapsed < PaperSeparationMotion.Duration;
    }

    private void PlayCutSound(float now)
    {
        if (speaker == null || now < nextSoundTime)
            return;

        nextSoundTime = now + SoundInterval;
        speaker.transform.position = cutterTip.position;
        speaker.pitch = Random.Range(0.9f, 1.2f);
        speaker.PlayOneShot(cutSound, cutVolume);
    }

    private void OnDisable()
    {
        foreach (Piece piece in pieces)
            piece.PreviousTip = null;
    }

    private void OnDestroy()
    {
        foreach (Piece piece in pieces)
            if (piece.Texture != null)
                Destroy(piece.Texture);
    }
}
