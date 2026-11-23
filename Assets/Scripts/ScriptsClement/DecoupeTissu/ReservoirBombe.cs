using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// FONCTIONNALITÉ : la bombe de peinture se vide, et il faut la secouer pour la recharger.
//  - Elle a une charge (1 = pleine, 0 = vide) qui baisse pendant qu'on peint.
//  - Vide : elle ne peint plus, et la manette qui la tient vibre très fort.
//  - On la secoue (des allers-retours rapides avec la main) : "cling cling", elle se recharge un peu à chaque secousse.
// À mettre sur une bombe de peinture (dans la scène), à côté de son script SprayInput.
public class ReservoirBombe : MonoBehaviour
{
    [SerializeField] private float dureePleine = 6f;          // pleine, elle peint pendant 6 secondes
    [SerializeField] private float rechargeParSecousse = 0.1f; // chaque secousse rend 10 % : 10 secousses = pleine
    [SerializeField] private float vitesseSecousse = 0.6f;     // en dessous de 0,6 m/s, ce n'est pas une secousse (juste un mouvement)
    [SerializeField] private AudioClip sonSecouer;             // le "cling cling" de la bille dans la bombe (PaintShake.mp3)
    [SerializeField, Range(0f, 1f)] private float volumeSecouer = 0.8f;

    // Combien de peinture il reste (1 = pleine, 0 = vide). Lu par SonSpray pour baisser le son quand elle se vide.
    public float Charge { get; private set; } = 1f;

    private SprayInput spray;           // sait si on appuie sur la gâchette en la tenant
    private XRGrabInteractable prise;   // sait si on la tient, et avec quelle manette
    private SprayConeVFX jet;           // le jet de peinture (sans lui, la bombe ne peint plus)
    private SprayPainter peinture;      // éteint par la fin de la peinture : on ne rallume plus le jet après
    private AudioSource hautParleur;    // d'où sort le "cling cling" : la bombe
    private Vector3 anciennePlace;      // où elle était à l'image d'avant
    private Vector3 ancienneVitesse;    // et à quelle vitesse elle bougeait

    private void Start()
    {
        spray = GetComponent<SprayInput>();
        prise = GetComponent<XRGrabInteractable>();
        jet = GetComponent<SprayConeVFX>();
        peinture = GetComponent<SprayPainter>();
        anciennePlace = transform.position;

        hautParleur = gameObject.AddComponent<AudioSource>();
        hautParleur.clip = sonSecouer;
        hautParleur.volume = volumeSecouer;
        hautParleur.playOnAwake = false;
        hautParleur.spatialBlend = 1f; // en 3D : on l'entend venir de la bombe
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f)
            return; // jeu en pause : pas de calcul de vitesse

        // 1) Elle se vide pendant qu'on peint.
        if (spray.IsSpraying && Charge > 0f)
        {
            Charge = Mathf.Max(0f, Charge - Time.deltaTime / dureePleine);
            if (Charge == 0f)
            {
                jet.enabled = false; // plus de jet : le script de peinture ne peint plus sans lui
                Vibrer(1f, 0.6f);    // vide : la manette vibre très fort
            }
        }

        // 2) On la secoue : un aller-retour rapide = la main change brusquement de sens.
        Vector3 vitesse = (transform.position - anciennePlace) / Time.deltaTime;
        bool changeDeSens = Vector3.Dot(vitesse, ancienneVitesse) < 0f;
        bool assezVite = ancienneVitesse.magnitude > vitesseSecousse;
        if (prise.isSelected && changeDeSens && assezVite)
        {
            Charge = Mathf.Min(1f, Charge + rechargeParSecousse);
            if (!hautParleur.isPlaying && sonSecouer != null)
                hautParleur.Play(); // "cling cling"
            Vibrer(0.3f, 0.05f);    // petit tac dans la main à chaque secousse

            // Elle a de nouveau de la peinture : le jet revient (sauf si la peinture est finie : là, on n'y touche plus).
            if (!jet.enabled && peinture != null && peinture.enabled)
                jet.enabled = true;
        }
        anciennePlace = transform.position;
        ancienneVitesse = vitesse;
    }

    // Fait vibrer la manette qui la tient (force de 0 à 1, durée en secondes).
    // (Une main suivie sans manette ne peut pas vibrer : dans ce cas, il ne se passe rien.)
    private void Vibrer(float force, float duree)
    {
        if (prise.isSelected && prise.interactorsSelecting[0] is XRBaseInputInteractor manette)
            manette.SendHapticImpulse(force, duree);
    }
}
