using UnityEngine;

public class Pen : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private Transform tip;
    [SerializeField] private DrawCanvas surface;

    [Header("Détection de la pointe")]
    [Tooltip("Portée de chaque côté de la pointe, en mètres.")]
    [SerializeField, Min(0.001f)] private float contactTolerance = 0.008f;

    [Header("Diagnostic en Play")]
    [SerializeField] private bool toucheSurface;
    [SerializeField] private Vector2Int pixelTouche;

    private void OnEnable()
    {
        toucheSurface = false;

        if (tip == null || surface == null)
        {
            Debug.LogWarning("Pen : assigne Tip et Surface dans l’Inspector.", this);
        }
    }

    private void LateUpdate()
    {
        if (tip == null || surface == null ||
            !surface.TryGetPixel(tip.position, tip.forward, contactTolerance, out Vector2Int pixel))
        {
            toucheSurface = false;
            return;
        }

        if (!toucheSurface)
        {
            // Premier contact : pose un point à l’endroit touché.
            surface.Drawing.PaintLine(pixel, pixel);
        }
        else if (pixel != pixelTouche)
        {
            // Contact maintenu : relie les deux positions de la pointe.
            surface.Drawing.PaintLine(pixelTouche, pixel);
        }

        pixelTouche = pixel;
        toucheSurface = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (tip == null)
            return;

        // Jaune en édition, rouge en Play, vert au contact de GFX.
        Gizmos.color = Application.isPlaying
            ? (toucheSurface ? Color.green : Color.red)
            : Color.yellow;

        Vector3 marge = tip.forward * contactTolerance;
        Gizmos.DrawLine(tip.position - marge, tip.position + marge);
        Gizmos.DrawWireSphere(tip.position, 0.003f);
    }
}