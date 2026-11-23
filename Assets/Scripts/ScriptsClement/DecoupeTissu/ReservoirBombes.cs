using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// FONCTIONNALITÉ : les bombes de peinture se vident, et il faut les secouer pour les recharger.
//  - Chaque bombe a une charge (1 = pleine, 0 = vide) qui baisse pendant qu'on peint.
//  - Vide : elle ne peint plus, et la manette qui la tient vibre très fort.
//  - On la secoue (des allers-retours rapides avec la main) : "cling cling", elle se recharge un peu à chaque secousse.
// À mettre sur l'objet "PieceDecoupee" : il apparaît à la fin de la découpe, juste quand on passe à la peinture.
// Il trouve tout seul les bombes de la scène (celles qui ont le script SprayInput) : on ne modifie pas leurs fichiers.
public class ReservoirBombes : MonoBehaviour
{
    [SerializeField] private float dureePleine = 6f;          // une bombe pleine peint pendant 6 secondes
    [SerializeField] private float rechargeParSecousse = 0.1f; // chaque secousse rend 10 % : 10 secousses = pleine
    [SerializeField] private float vitesseSecousse = 0.6f;     // en dessous de 0,6 m/s, ce n'est pas une secousse (juste un mouvement)
    [SerializeField] private AudioClip sonSecouer;             // le "cling cling" de la bille dans la bombe (PaintShake.mp3)

    private SprayInput[] bombes;        // les bombes de peinture
    private float[] charges;            // la charge de chaque bombe (1 = pleine, 0 = vide)
    private Vector3[] anciennesPlaces;  // où était chaque bombe à l'image d'avant
    private Vector3[] anciennesVitesses; // et à quelle vitesse elle bougeait
    private AudioSource[] hautParleurs; // pour le son de secousse, posé sur chaque bombe

    private void Start()
    {
        // "Include" : on trouve aussi les bombes encore cachées (elles apparaissent juste après la découpe).
        bombes = FindObjectsByType<SprayInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        charges = new float[bombes.Length];
        anciennesPlaces = new Vector3[bombes.Length];
        anciennesVitesses = new Vector3[bombes.Length];
        hautParleurs = new AudioSource[bombes.Length];
        for (int i = 0; i < bombes.Length; i++)
        {
            charges[i] = 1f; // elles commencent pleines
            anciennesPlaces[i] = bombes[i].transform.position;
            hautParleurs[i] = bombes[i].gameObject.AddComponent<AudioSource>();
            hautParleurs[i].clip = sonSecouer;
            hautParleurs[i].playOnAwake = false;
            hautParleurs[i].spatialBlend = 1f; // en 3D : on l'entend venir de la bombe
        }
    }

    // La charge d'une bombe, pour les autres scripts (SonSpray baisse le son quand elle se vide). 1 si on ne la connaît pas.
    public float Charge(SprayInput bombe)
    {
        int i = System.Array.IndexOf(bombes, bombe);
        return i < 0 ? 1f : charges[i];
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f)
            return; // jeu en pause : pas de calcul de vitesse

        for (int i = 0; i < bombes.Length; i++)
        {
            SprayInput bombe = bombes[i];
            if (bombe == null || !bombe.isActiveAndEnabled)
                continue; // bombe cachée (pas encore là, ou rangée à la fin de la peinture)

            XRGrabInteractable prise = bombe.GetComponent<XRGrabInteractable>();
            SprayConeVFX jet = bombe.GetComponent<SprayConeVFX>();
            SprayPainter peinture = bombe.GetComponent<SprayPainter>();

            // 1) Elle se vide pendant qu'on peint.
            if (bombe.IsSpraying && charges[i] > 0f)
            {
                charges[i] = Mathf.Max(0f, charges[i] - Time.deltaTime / dureePleine);
                if (charges[i] == 0f)
                {
                    jet.enabled = false; // plus de jet : le script de peinture ne peint plus sans lui
                    Vibrer(prise, 1f, 0.6f); // vide : la manette vibre très fort
                }
            }

            // 2) On la secoue : un aller-retour rapide = la main change brusquement de sens.
            Vector3 vitesse = (bombe.transform.position - anciennesPlaces[i]) / Time.deltaTime;
            bool changeDeSens = Vector3.Dot(vitesse, anciennesVitesses[i]) < 0f;
            bool assezVite = anciennesVitesses[i].magnitude > vitesseSecousse;
            if (prise.isSelected && changeDeSens && assezVite)
            {
                charges[i] = Mathf.Min(1f, charges[i] + rechargeParSecousse);
                if (!hautParleurs[i].isPlaying && sonSecouer != null)
                    hautParleurs[i].Play(); // "cling cling"
                Vibrer(prise, 0.3f, 0.05f); // petit tac dans la main à chaque secousse

                // Elle a de nouveau de la peinture : le jet revient (sauf si la peinture est finie : là, on n'y touche plus).
                if (!jet.enabled && peinture != null && peinture.enabled)
                    jet.enabled = true;
            }
            anciennesPlaces[i] = bombe.transform.position;
            anciennesVitesses[i] = vitesse;
        }
    }

    // Fait vibrer la manette qui tient cette bombe (force de 0 à 1, durée en secondes).
    // (Une main suivie sans manette ne peut pas vibrer : dans ce cas, il ne se passe rien.)
    private void Vibrer(XRGrabInteractable prise, float force, float duree)
    {
        if (prise.isSelected && prise.interactorsSelecting[0] is XRBaseInputInteractor manette)
            manette.SendHapticImpulse(force, duree);
    }
}
