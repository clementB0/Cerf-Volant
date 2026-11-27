using UnityEngine;

internal static class PaperSeparationMotion
{
    public const float Duration = 0.58f;

    public static float Progress(float elapsed)
    {
        return 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / Duration), 3f);
    }

    public static Vector3 Offset(Vector2 outward, bool peel)
    {
        return (Vector3)outward * (peel ? 0.079f : 0.064f)
            + Vector3.back * (peel ? 0.12f : 0.027f);
    }

    public static Quaternion Tilt(bool peel)
    {
        return peel ? Quaternion.Euler(22f, -27f, -16f) : Quaternion.Euler(-14f, 17f, 12f);
    }
}
