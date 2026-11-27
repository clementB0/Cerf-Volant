using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

internal sealed class CutPaperRegions
{
    internal readonly struct Region
    {
        public readonly int Id;
        public readonly int Area;
        public readonly RectInt Bounds;
        public readonly Vector2 Center;

        public Region(int id, int area, RectInt bounds, Vector2 center)
        {
            Id = id;
            Area = area;
            Bounds = bounds;
            Center = center;
        }
    }

    private readonly int[] labels;
    private readonly int[] queue;
    private readonly List<Region> regions = new();

    public CutPaperRegions(int capacity)
    {
        labels = new int[capacity];
        queue = new int[capacity];
    }

    public int LabelAt(int index) => labels[index];

    public IReadOnlyList<Region> Find(NativeArray<Color32> pixels, int width, int height)
    {
        Array.Clear(labels, 0, pixels.Length);
        regions.Clear();
        for (int index = 0; index < pixels.Length; index++)
        {
            if (labels[index] != 0 || pixels[index].a == 0)
                continue;

            int id = regions.Count + 1;
            int count = 0;
            Enqueue(pixels, index, id, ref count);
            int left = index % width;
            int right = left;
            int bottom = index / width;
            int top = bottom;
            long sumX = 0;
            long sumY = 0;
            for (int head = 0; head < count; head++)
            {
                int current = queue[head];
                int x = current % width;
                int y = current / width;
                left = Mathf.Min(left, x);
                right = Mathf.Max(right, x);
                bottom = Mathf.Min(bottom, y);
                top = Mathf.Max(top, y);
                sumX += x;
                sumY += y;

                if (x > 0) Enqueue(pixels, current - 1, id, ref count);
                if (x + 1 < width) Enqueue(pixels, current + 1, id, ref count);
                if (y > 0) Enqueue(pixels, current - width, id, ref count);
                if (y + 1 < height) Enqueue(pixels, current + width, id, ref count);
            }

            var bounds = new RectInt(left, bottom, right - left + 1, top - bottom + 1);
            var center = new Vector2((float)sumX / count + 0.5f, (float)sumY / count + 0.5f);
            regions.Add(new Region(id, count, bounds, center));
        }
        return regions;
    }

    private void Enqueue(NativeArray<Color32> pixels, int index, int id, ref int count)
    {
        if (labels[index] != 0 || pixels[index].a == 0)
            return;
        labels[index] = id;
        queue[count++] = index;
    }
}
