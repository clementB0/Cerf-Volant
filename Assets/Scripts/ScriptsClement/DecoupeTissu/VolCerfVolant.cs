
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
    [SerializeField] private float portanceMainsBasses = 5.5f;
    [SerializeField] private float portanceMainsHautes = 10.5f;
    [SerializeField] private float frottement = 1f;
    [SerializeField] private float angleDAttaque = 20f;

    [Header("Direction")]
    [SerializeField] private float angleDeVirageMax = 60f;

    [Header("Sol et chute")]
    [SerializeField] private float hauteurDuSol = 0.3f;
    [SerializeField] private float dureeChute = 3f;

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
    public bool Tombe => Time.time < finDeChute;

    private float finDeChute;
    private float prochaineVibration;

    private XRGrabInteractable prise;
    private Vector3 vitesse;

    private void Start()
    {
        prise = GetComponent<XRGrabInteractable>();
    }

    // Appelé lorsqu'un obstacle touche le cerf-volant
    public void Tomber()
    {
        finDeChute = Time.time + dureeChute;
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

        EnVol =
            cordesAccrochees &&
            poigneeGauche.isSelected &&
            poigneeDroite.isSelected;

        // Empêcher d'attraper le cerf-volant pendant le vol
        prise.enabled = !EnVol;

        if (!EnVol)
        {
            vitesse = Vector3.zero;
            return;
        }

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

        float penche = -(droite - gauche).normalized.y;
        float virage = penche * angleDeVirageMax;

        float azimut = Vector3.SignedAngle(
            Vector3.ProjectOnPlane(Vent, Vector3.up),
            Vector3.ProjectOnPlane(fil, Vector3.up),
            Vector3.up
        );

        Vector3 cote = Vector3.Cross(tete, fil);

        float inclinaison = Mathf.Clamp(
            (virage - azimut) * 2f
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
            mainsHautes = Mathf.InverseLerp(
                Camera.main.transform.position.y - 0.9f,
                Camera.main.transform.position.y - 0.1f,
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
