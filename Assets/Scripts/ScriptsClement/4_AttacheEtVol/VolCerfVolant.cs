
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VolCerfVolant : MonoBehaviour
{
    [Header("Poignées et cordes")]
    [SerializeField] private XRGrabInteractable poigneeGauche;
    [SerializeField] private XRGrabInteractable poigneeDroite;
    [SerializeField] private Transform boutCordeGauche;
    [SerializeField] private Transform boutCordeDroite;

    [Header("Paramètres du vol")]
    [SerializeField] private float longueurFil = 4f;
    [SerializeField] private Vector3 directionDuVent = Vector3.forward;
    [SerializeField] private float vitesseDuVent = 6f;
    [SerializeField] private float rafales = 0.4f;

    [Header("Portance")]
    [SerializeField] private float portanceMainsBasses = 4f;   // mains basses : il descend nettement
    [SerializeField] private float portanceMainsHautes = 13f;  // mains hautes : il monte nettement
    [SerializeField] private float frottement = 1f;
    [SerializeField] private float angleDAttaque = 20f;

    [Header("Direction")]
    [SerializeField] private float angleDeVirageMax = 60f;
    [SerializeField] private float sensibiliteVirage = 1.6f;      // plus grand = il tourne avec moins d'écart de hauteur entre les mains (1 = pente des mains telle quelle)
    [SerializeField] private float lissageVirage = 0.4f;          // il met environ 0,4 s à suivre un changement gauche/droite des mains (0 = tout de suite)
    [SerializeField] private float amplitudeHauteurMains = 0.3f;  // de mains basses à mains hautes en 30 cm de mouvement (plus grand = le haut/bas réagit moins aux petits gestes)

    [Header("Sol et chute")]
    [SerializeField] private float hauteurDuSol = 0.3f;
    [SerializeField] private float dureeChute = 3f;        // il reste au moins 3 s au sol avant de pouvoir repartir
    [SerializeField] private float vitesseRelance = 0.7f;  // pour repartir : lever les deux poignées en même temps, à plus de 0,7 m/s

    [Header("Vibrations VR")]
    [SerializeField] private bool activerVibrations = true;

    [SerializeField, Range(0f, 1f)]
    private float intensiteVibration = 0.15f;

    [SerializeField, Min(0.01f)]
    private float dureeVibration = 0.08f;

    [SerializeField, Min(0.01f)]
    private float intervalleVibration = 0.1f;

    // Informations accessibles aux autres scripts
    public bool EnVol { get; private set; }
    public Vector3 Vent { get; private set; }
    public Vector3 VentApparent { get; private set; }
    // Touché par un oiseau : il tombe et reste au sol jusqu'à ce que le joueur le relance (en levant les deux poignées).
    public bool Tombe => aTerre;
    // Il est tombé depuis assez longtemps : il attend que le joueur lève les deux poignées (MessageRelance affiche "Monte").
    public bool AttendRelance => aTerre && Time.time >= finDeChute;

    private bool aTerre;
    private float finDeChute;
    private float pencheLisse;           // la pente des mains, lissée (gauche/droite moins direct)
    private float ancienneHauteurGauche; // la hauteur des poignées à l'image d'avant (pour savoir si elles montent)
    private float ancienneHauteurDroite;
    private float prochaineVibration;

    private XRGrabInteractable prise;
    private Vector3 vitesse;

    private bool cordeGaucheValidee;
    private bool cordeDroiteValidee;
    private bool assemblageAnnonce;

    private void Start()
    {
        prise = GetComponent<XRGrabInteractable>();
    }

    // Appelé lorsqu'un obstacle touche le cerf-volant
    public void Tomber()
    {
        aTerre = true;
        finDeChute = Time.time + dureeChute;
    }

    internal void ConfirmerConnexion(Transform bout)
    {
        if (bout == boutCordeGauche)
            cordeGaucheValidee = true;
        else if (bout == boutCordeDroite)
            cordeDroiteValidee = true;
    }

    private void OnDisable()
    {
        EnVol = false;
    }

    private void Update()
    {
        // Vérifier que les deux cordes sont accrochées
        // et que les deux poignées sont tenues.
        bool cordesAccrochees =
            boutCordeGauche == null &&
            boutCordeDroite == null;

        if (cordesAccrochees && cordeGaucheValidee && cordeDroiteValidee && !assemblageAnnonce)
        {
            assemblageAnnonce = true;
            if (TryGetComponent<SuccessLink>(out var feedback) && feedback.isActiveAndEnabled)
                feedback.Lancer();
        }

        EnVol =
            cordesAccrochees &&
            poigneeGauche.isSelected &&
            poigneeDroite.isSelected;

        // Empêcher d'attraper le cerf-volant pendant le vol
        prise.enabled = !EnVol;

        // La hauteur des poignées, gardée pour l'image suivante (pour savoir si elles montent).
        float hauteurGauche = poigneeGauche.transform.position.y;
        float hauteurDroite = poigneeDroite.transform.position.y;

        if (!EnVol)
        {
            // Tombé (oiseau, ou poignées lâchées : voir RetourPoignees) et personne ne le tient :
            // il continue de tomber jusqu'au sol, au lieu de rester figé en l'air.
            if (aTerre && !prise.isSelected && transform.position.y > hauteurDuSol)
            {
                vitesse += Physics.gravity * Time.deltaTime;
                Vector3 chute = transform.position + vitesse * Time.deltaTime;
                if (chute.y <= hauteurDuSol)
                {
                    chute.y = hauteurDuSol;
                    vitesse = Vector3.zero;
                }
                transform.position = chute;
            }
            else
            {
                vitesse = Vector3.zero;
            }
            ancienneHauteurGauche = hauteurGauche;
            ancienneHauteurDroite = hauteurDroite;
            return;
        }

        // Relance après une chute : les deux poignées montent vite en même temps (le geste de tirer le cerf-volant vers le ciel).
        if (AttendRelance && Time.deltaTime > 0f)
        {
            float monteeGauche = (hauteurGauche - ancienneHauteurGauche) / Time.deltaTime;
            float monteeDroite = (hauteurDroite - ancienneHauteurDroite) / Time.deltaTime;
            if (monteeGauche > vitesseRelance && monteeDroite > vitesseRelance)
                aTerre = false;
        }
        ancienneHauteurGauche = hauteurGauche;
        ancienneHauteurDroite = hauteurDroite;

        // =========================================
        // VIBRATIONS DES MANETTES
        // =========================================

        if (activerVibrations && !Tombe)
        {
            if (Time.time >= prochaineVibration)
            {
                if (poigneeGauche.firstInteractorSelecting
                    is XRBaseInputInteractor mainGauche)
                {
                    mainGauche.SendHapticImpulse(
                        intensiteVibration,
                        dureeVibration
                    );
                }

                if (poigneeDroite.firstInteractorSelecting
                    is XRBaseInputInteractor mainDroite)
                {
                    mainDroite.SendHapticImpulse(
                        intensiteVibration,
                        dureeVibration
                    );
                }

                prochaineVibration =
                    Time.time + intervalleVibration;
            }
        }

        // =========================================
        // SIMULATION DU VOL
        // =========================================

        float dt = Mathf.Min(Time.deltaTime, 0.03f);

        // Position des deux poignées
        Vector3 gauche = poigneeGauche.transform.position;
        Vector3 droite = poigneeDroite.transform.position;
        Vector3 milieu = (gauche + droite) / 2f;

        // =========================================
        // 1. VENT ET RAFALES
        // =========================================

        float rafale =
            Mathf.PerlinNoise(Time.time * 0.3f, 0f) * 2f - 1f;

        float deviation =
            (Mathf.PerlinNoise(0f, Time.time * 0.2f) * 2f - 1f) * 15f;

        Vent =
            Quaternion.AngleAxis(deviation, Vector3.up) *
            directionDuVent.normalized *
            vitesseDuVent *
            (1f + rafale * rafales);

        // =========================================
        // 2. DIRECTION DU FIL
        // =========================================

        Vector3 fil = transform.position - milieu;

        fil = fil.magnitude > 0.01f
            ? fil.normalized
            : Vent.normalized;

        fil.y = Mathf.Max(fil.y, 0.3f);
        fil.Normalize();

        // =========================================
        // 3. ORIENTATION DU CERF-VOLANT
        // =========================================

        Vector3 tete =
            Vector3.ProjectOnPlane(Vector3.up, fil).normalized;

        if (tete == Vector3.zero)
        {
            tete = -Vent.normalized;
        }

        // =========================================
        // 4. CONTRÔLE AVEC LES POIGNÉES
        // =========================================

        // Une main un peu plus basse que l'autre suffit : la pente des mains est amplifiée (sensibiliteVirage),
        // ex. 10 cm d'écart de hauteur (mains à 40 cm l'une de l'autre) = déjà plus de la moitié du virage maximum.
        float penche = Mathf.Clamp(-(droite - gauche).normalized.y * sensibiliteVirage, -1f, 1f);
        // Lissé : le gauche/droite suit les mains en douceur, sans copier chaque petit mouvement.
        pencheLisse = lissageVirage > 0f ? Mathf.Lerp(pencheLisse, penche, 1f - Mathf.Exp(-dt / lissageVirage)) : penche;
        float virage = pencheLisse * angleDeVirageMax;

        float azimut = Vector3.SignedAngle(
            Vector3.ProjectOnPlane(Vent, Vector3.up),
            Vector3.ProjectOnPlane(fil, Vector3.up),
            Vector3.up
        );

        Vector3 cote = Vector3.Cross(tete, fil);

        float inclinaison = Mathf.Clamp(
            (virage - azimut) * 2f // il suit le virage demandé sans à-coup
            - Vector3.Dot(vitesse, cote) * 25f,
            -60f,
            60f
        );

        tete = Quaternion.AngleAxis(-inclinaison, fil) * tete;

        // =========================================
        // 5. ANGLE D'ATTAQUE
        // =========================================

        float attaque = angleDAttaque * Mathf.Deg2Rad;

        Vector3 dos =
            fil * Mathf.Cos(attaque) +
            tete * Mathf.Sin(attaque);

        // =========================================
        // 6. FORCES DU VENT
        // =========================================

        VentApparent = Vent - vitesse;

        float mainsHautes = 0.55f;

        if (Camera.main != null)
        {
            // Les mains au milieu = 50 cm sous les yeux. De là, monter ou descendre les mains de la moitié de
            // "amplitudeHauteurMains" (15 cm) suffit pour la portance la plus forte ou la plus faible.
            float milieuMains = Camera.main.transform.position.y - 0.5f;
            mainsHautes = Mathf.InverseLerp(
                milieuMains - amplitudeHauteurMains / 2f,
                milieuMains + amplitudeHauteurMains / 2f,
                milieu.y
            );
        }

        float portance = Mathf.Lerp(
            portanceMainsBasses,
            portanceMainsHautes,
            mainsHautes
        );

        Vector3 pousseeDuVent =
            dos * (Vector3.Dot(VentApparent, dos) * portance);

        if (Tombe)
        {
            pousseeDuVent = Vector3.zero;
        }

        Vector3 frein = VentApparent * frottement;

        vitesse +=
            (pousseeDuVent + frein + Physics.gravity) * dt;

        // =========================================
        // 7. POSITION DU CERF-VOLANT
        // =========================================

        Vector3 position = transform.position + vitesse * dt;

        position = milieu + Vector3.ClampMagnitude(
            position - milieu,
            longueurFil
        );

        position.y = Mathf.Max(position.y, hauteurDuSol);

        vitesse = (position - transform.position) / dt;

        transform.position = position;

        // =========================================
        // 8. ROTATION DU CERF-VOLANT
        // =========================================

        Quaternion orientation =
            Quaternion.LookRotation(dos, tete);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            orientation,
            10f * Time.deltaTime
        );
    }
}
