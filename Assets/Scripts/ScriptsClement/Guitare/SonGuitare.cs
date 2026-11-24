using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// FONCTIONNALITÉ : jouer de la guitare. Pendant que la main gauche tient le manche,
// passer la main DROITE à travers les cordes (de haut en bas ou de bas en haut) joue un accord de guitare électrique saturée.
//  - Le son est fabriqué par le script (pas de fichier audio) : des cordes "pincées" + l'effet de saturation d'un ampli.
//  - Plus on gratte vite, plus c'est fort, et la manette droite vibre un petit coup.
// À mettre sur le même objet que PriseGuitare.
public class SonGuitare : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;
    [SerializeField] private float vitessePleinVolume = 1.5f; // en gratant à 1,5 m/s, c'est le plus fort

    private XRGrabInteractable prise;
    private Transform zone;          // l'endroit des cordes où on gratte (le repère "Zone_Grattage" du modèle)
    private Transform mainDroite;
    private AudioSource ampli;
    private float ancienCote;        // de quel côté des cordes était la main droite à l'image d'avant (-1 ou 1, 0 = loin)
    private float dernierCoup;

    private static AudioClip accord; // fabriqué une seule fois, même s'il y a plusieurs guitares

    private void Start()
    {
        prise = GetComponent<XRGrabInteractable>(); // ajoutée par PriseGuitare (dans son Awake, donc déjà là)
        foreach (Transform t in GetComponentsInChildren<Transform>())
            if (t.name == "Zone_Grattage")
                zone = t;

        if (zone == null)
            Debug.LogWarning("SonGuitare : pas de repère \"Zone_Grattage\" dans le modèle, la guitare ne peut pas jouer.");

        if (accord == null)
            accord = FabriquerAccord();
        ampli = gameObject.AddComponent<AudioSource>();
        ampli.clip = accord;
        ampli.playOnAwake = false;
        ampli.spatialBlend = 0.6f; // surtout en 3D (on l'entend venir de la guitare), mais pas trop : on la tient tout près
    }

    private void Update()
    {
        if (zone == null || !prise.isSelected)
        {
            ancienCote = 0f;
            return;
        }

        // La main droite peut changer en cours de jeu (manette allumée plus tard, passage aux mains suivies...) : on la recherche si besoin.
        if (mainDroite == null || !mainDroite.gameObject.activeInHierarchy)
            mainDroite = ChercherMainDroite();
        if (mainDroite == null)
            return;

        // La main droite, vue depuis les cordes : x = en travers des cordes, y = le long des cordes, z = devant la guitare.
        // La zone est large : le point suivi par la manette n'est pas exactement au bout des doigts.
        Vector3 ici = zone.InverseTransformPoint(mainDroite.position);
        bool surLesCordes = Mathf.Abs(ici.y) < 0.18f && Mathf.Abs(ici.z) < 0.20f && Mathf.Abs(ici.x) < 0.25f;
        float cote = surLesCordes ? Mathf.Sign(ici.x) : 0f;

        // Un coup de gratte = la main passe d'un côté des cordes à l'autre (sans sortir de la zone).
        if (ancienCote != 0f && cote != 0f && cote != ancienCote && Time.time - dernierCoup > 0.06f)
        {
            dernierCoup = Time.time;
            float vitesse = Mathf.Abs(ici.x) / Mathf.Max(Time.deltaTime, 0.001f);
            Jouer(Mathf.Clamp01(0.35f + vitesse / vitessePleinVolume));
        }
        ancienCote = cote;
    }

    // Cherche une main droite active de la scène (les mains éteintes, ex. une manette pas encore suivie, ne comptent pas).
    private static Transform ChercherMainDroite()
    {
        foreach (XRBaseInteractor main in FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None))
            if (main.handedness == InteractorHandedness.Right)
                return main.transform;
        return null;
    }

    private void Jouer(float force)
    {
        // Rejouer coupe l'accord d'avant, comme sur une vraie guitare.
        ampli.volume = volume * force;
        ampli.Play();

        // Petit tac dans la manette droite.
        if (mainDroite.TryGetComponent(out XRBaseInputInteractor manette))
            manette.SendHapticImpulse(0.3f * force, 0.05f);
    }

    // Fabrique 3 secondes de son : un accord de quinte en Mi (Mi grave, Si, Mi aigu), gratté vers le bas, saturé.
    private static AudioClip FabriquerAccord()
    {
        const int frequence = 44100;           // échantillons par seconde
        int n = frequence * 3;
        float[] son = new float[n];
        float[] notes = { 82.41f, 123.47f, 164.81f };

        // Chaque corde : une "corde pincée" (méthode de Karplus-Strong) : un petit bruit qui tourne en boucle
        // et s'adoucit à chaque tour, ce qui donne une note qui sonne puis s'éteint, comme une vraie corde.
        System.Random hasard = new System.Random(7);
        for (int c = 0; c < notes.Length; c++)
        {
            float[] boucle = new float[Mathf.RoundToInt(frequence / notes[c])];
            for (int i = 0; i < boucle.Length; i++)
                boucle[i] = (float)(hasard.NextDouble() * 2.0 - 1.0);

            int debut = c * frequence / 80; // les cordes partent l'une après l'autre (12 ms) : le geste de gratter
            for (int i = 0, k = 0; debut + i < n; i++, k = (k + 1) % boucle.Length)
            {
                int suivant = (k + 1) % boucle.Length;
                son[debut + i] += boucle[k];
                boucle[k] = (boucle[k] + boucle[suivant]) * 0.4985f; // moyenne (adoucit) + perte (s'éteint)
            }
        }

        // La saturation de l'ampli : on pousse le son très fort puis on l'écrase en douceur (Tanh),
        // et un léger filtre enlève le grésillement trop aigu. Enfin, un fondu sur la fin.
        float filtre = 0f;
        for (int i = 0; i < n; i++)
        {
            float sature = (float)System.Math.Tanh(son[i] * 4f);
            filtre += (sature - filtre) * 0.35f;
            son[i] = filtre * 0.7f * Mathf.Clamp01((n - i) / (float)(frequence / 4));
        }

        AudioClip clip = AudioClip.Create("AccordGuitare", n, 1, frequence, false);
        clip.SetData(son, 0);
        return clip;
    }
}
