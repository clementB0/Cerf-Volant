using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

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

    [Header("Son du tracé")]
    [SerializeField] private AudioClip sonTrait;
    [SerializeField, Range(0f, 1f)] private float volumeTrait = 1f;
    private AudioSource sourceTrait;
    private float dernierMouvement;
    [SerializeField, Range(0f, 1f)] private float vibrationTrace = 0.09f;
    private float prochaineVibration;

    [Header("Détection de la pointe")]
    [Tooltip("Portée de chaque côté de la pointe, en mètres.")]
    [SerializeField, Min(0.001f)] private float contactTolerance = 0.008f;

    [Header("Diagnostic en Play")]
    [SerializeField] private bool toucheSurface;
    [SerializeField] private Vector2Int pixelTouche;

    private void Awake()
    {
        prise = GetComponent<XRGrabInteractable>();
        if (tip != null && sonTrait != null)
        {
            sourceTrait = tip.gameObject.AddComponent<AudioSource>();
            sourceTrait.playOnAwake = false;
            sourceTrait.clip = sonTrait;
            sourceTrait.loop = true;
            sourceTrait.spatialBlend = 0.35f;
            sourceTrait.dopplerLevel = 0f;
            sourceTrait.minDistance = 1f;
            sourceTrait.priority = 64;
            sourceTrait.volume = volumeTrait;
        }
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
        if (sourceTrait != null)
            sourceTrait.Stop();
        zonePrecedente = null;
        prochaineVibration = 0f;
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
        bool trace = toucheSurface && (zone != zonePrecedente || point != pixelTouche);
        if (trace)
        {
            zone.DrawPenLine(zone == zonePrecedente ? pixelTouche : point, point, couleurMine);
            if (Time.unscaledTime >= prochaineVibration && prise != null &&
                prise.firstInteractorSelecting is XRBaseInputInteractor manette)
            {
                manette.SendHapticImpulse(vibrationTrace, 0.035f);
                prochaineVibration = Time.unscaledTime + 0.05f;
            }
            pixelTouche = point;
        }
        if (sourceTrait != null)
        {
            if (trace)
            {
                dernierMouvement = Time.unscaledTime;
                if (!sourceTrait.isPlaying)
                    sourceTrait.Play();
            }
            else if (sourceTrait.isPlaying &&
                     (zone == null || Time.unscaledTime - dernierMouvement >= 0.15f))
            {
                sourceTrait.Stop();
            }
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
