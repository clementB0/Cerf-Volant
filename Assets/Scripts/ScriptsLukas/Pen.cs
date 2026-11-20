using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Pen : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private Transform tip;
    [SerializeField, InspectorName("Zones de dessin autorisées")]
    private List<PenSurface> zonesAutorisees = new List<PenSurface>();
    [SerializeField] private Color couleurMine = Color.black;
    [SerializeField] private Renderer indicateur;
    [SerializeField] private bool dessinerEnMainSeulement;

    private XRGrabInteractable prise;
    private PenSurface zonePrecedente;

    [Header("Détection de la pointe")]
    [Tooltip("Portée de chaque côté de la pointe, en mètres.")]
    [SerializeField, Min(0.001f)] private float contactTolerance = 0.008f;

    [Header("Diagnostic en Play")]
    [SerializeField] private bool toucheSurface;
    [SerializeField] private Vector2Int pixelTouche;

    private void Awake()
    {
        prise = GetComponent<XRGrabInteractable>();
        if (indicateur != null)
        {
            var proprietes = new MaterialPropertyBlock();
            indicateur.GetPropertyBlock(proprietes);
            proprietes.SetColor("_BaseColor", couleurMine);
            indicateur.SetPropertyBlock(proprietes);
        }
    }

    private void OnDisable()
    {
        zonePrecedente = null;
        toucheSurface = false;
    }

    private void LateUpdate()
    {
        PenSurface zone = null;
        Vector2Int point = default;
        float distanceMin = float.PositiveInfinity;
        if (tip != null && (!dessinerEnMainSeulement || (prise != null && prise.isSelected)))
        {
            foreach (PenSurface candidate in zonesAutorisees)
            {
                if (candidate == null ||
                    !candidate.TryGetContact(tip.position, contactTolerance, out var pixel, out float distance) ||
                    distance >= distanceMin)
                    continue;

                zone = candidate;
                point = pixel;
                distanceMin = distance;
            }
        }

        toucheSurface = zone != null;
        if (toucheSurface)
        {
            if (zone != zonePrecedente || point != pixelTouche)
                zone.DrawPenLine(zone == zonePrecedente ? pixelTouche : point, point, couleurMine);
            pixelTouche = point;
        }
        zonePrecedente = zone;
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
