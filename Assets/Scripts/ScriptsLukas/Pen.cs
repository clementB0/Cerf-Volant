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
    private ParticleSystem particulesMine;
    private Material materiauParticules;
    private float prochaineEmission;

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
        if (tip != null)
            CreerParticulesMine();

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
        if (particulesMine != null)
            particulesMine.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        prochaineEmission = 0f;
        zonePrecedente = null;
        prochaineVibration = 0f;
        toucheSurface = false;
    }

    private void OnDestroy()
    {
        if (particulesMine != null)
            Destroy(particulesMine.gameObject);
        if (materiauParticules != null)
            Destroy(materiauParticules);
    }

    private void LateUpdate()
    {
        PenSurface zone = null;
        Vector2Int point = default;
        RaycastHit contact = default;
        float distanceMin = float.PositiveInfinity;
        if (tip != null && (!dessinerEnMainSeulement || (prise != null && prise.isSelected)))
        {
            foreach (PenSurface candidate in zonesAutorisees)
            {
                if (candidate == null ||
                    !candidate.TryGetContact(tip.position, contactTolerance, out var pixel,
                                             out float distance, out var impact) ||
                    distance >= distanceMin)
                    continue;

                zone = candidate;
                point = pixel;
                contact = impact;
                distanceMin = distance;
            }
        }

        toucheSurface = zone != null;
        bool trace = toucheSurface && (zone != zonePrecedente || point != pixelTouche);
        if (trace)
        {
            if (zone.DrawPenLine(zone == zonePrecedente ? pixelTouche : point, point, couleurMine))
                EmettreParticulesMine(contact);

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

    private void CreerParticulesMine()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            return;

        GameObject objet = new GameObject("Particules de la mine");
        objet.SetActive(false);
        objet.transform.SetParent(tip, false);
        particulesMine = objet.AddComponent<ParticleSystem>();

        var reglages = particulesMine.main;
        reglages.playOnAwake = false;
        reglages.simulationSpace = ParticleSystemSimulationSpace.World;
        reglages.scalingMode = ParticleSystemScalingMode.Shape;
        reglages.useUnscaledTime = true;
        reglages.maxParticles = 24;
        reglages.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.4f);
        reglages.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.01f);

        var emission = particulesMine.emission;
        emission.enabled = false;
        var forme = particulesMine.shape;
        forme.enabled = false;
        var taille = particulesMine.sizeOverLifetime;
        taille.enabled = true;
        taille.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        materiauParticules = new Material(shader);
        objet.GetComponent<ParticleSystemRenderer>().sharedMaterial = materiauParticules;
        objet.SetActive(true);
    }

    private void EmettreParticulesMine(RaycastHit contact)
    {
        if (particulesMine == null || Time.unscaledTime < prochaineEmission)
            return;

        prochaineEmission = Time.unscaledTime + 0.065f;
        var particule = new ParticleSystem.EmitParams
        {
            position = contact.point + contact.normal * 0.006f,
            startColor = couleurMine
        };
        for (int i = 0; i < 3; i++)
        {
            particule.velocity = contact.normal * 0.11f + Random.insideUnitSphere * 0.09f;
            particulesMine.Emit(particule, 1);
        }
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
