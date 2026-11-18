using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// FONCTIONNALITÉ : dessiner la forme du cerf-volant en traits blancs (pointillés) sur le tissu.
// Quand la pointe du cutter passe sur un trait, le tissu est découpé à cet endroit : le trait devient une fente foncée
// qui rejoint ses voisines (petit "tic" de découpe à chaque fois). Trait après trait, les pointillés deviennent une découpe.
// À mettre sur l'objet "Tissu" (un Quad couché, de 1 m x 1 m quand son échelle vaut 1).
public class PatronPointilles : MonoBehaviour
{
    // La forme à découper (un fichier "Forme" : Papillon, Raie, Dragon... dans ScenesClement/Formes).
    // Pour changer de cerf-volant, glisser une autre forme ici dans l'Inspector.
    [SerializeField] private FormeCerfVolant forme;

    [SerializeField] private float ecartEntreTraits = 0.06f; // un trait tous les 6 cm (le trait fait la moitié : 3 cm)
    [SerializeField] private float epaisseurTrait = 0.01f;   // 1 cm (la fente fait la moitié : 5 mm)
    [SerializeField] private Material materiauTrait;         // couleur des traits (blanc)
    [SerializeField] private float rayonContact = 0.02f;     // un trait se relie si la pointe du cutter passe à moins de 2 cm
    [SerializeField] private Material materiauDecoupe;       // couleur de la fente une fois découpé (presque noir : Fente.mat)
    [SerializeField] private AudioClip sonDecoupe;           // petit "tic" joué à chaque trait découpé (SonDecoupe.wav)
    [SerializeField] private float volumeDecoupe = 0.4f;     // léger
    [SerializeField] private int papiersParTrait = 6;        // combien de petits bouts de tissu sautent à chaque trait découpé

    // Ce qui se passe quand tous les traits sont reliés. Se règle dans l'Inspector (liste "Quand Tout Est Relie").
    // Dans la scène : affiche la PieceDecoupee et cache le Tissu.
    [SerializeField] private UnityEvent quandToutEstRelie;

    private List<Transform> traitsPasRelies = new List<Transform>(); // les traits que le cutter n'a pas encore touchés
    private bool toutEstRelie;                                       // devient vrai une seule fois, à la fin
    private AudioSource hautParleur;                                 // d'où sort le "tic" : le tissu
    private ParticleSystem papiers;                                  // les petits bouts de tissu qui sautent pendant la découpe

    // La forme, en lecture seule, pour que PieceDecoupee ait exactement la même.
    public FormeCerfVolant Forme => forme;

    // Appelé par ChoixCerfVolant quand on choisit un cerf-volant dans le menu (avant que le tissu apparaisse).
    public void ChangerForme(FormeCerfVolant nouvelleForme)
    {
        forme = nouvelleForme;
    }

    private void Start()
    {
        // Le son part du tissu, en 3D : on l'entend venir de l'endroit où on découpe.
        hautParleur = gameObject.AddComponent<AudioSource>();
        hautParleur.playOnAwake = false;
        hautParleur.spatialBlend = 1f;

        CreerPapiers();

        Vector2[] coins = forme.Coins;

        // On relie chaque coin au suivant. Le "% coins.Length" fait revenir le dernier coin au premier : la forme est fermée.
        for (int i = 0; i < coins.Length; i++)
        {
            Vector2 depart = coins[i];
            Vector2 arrivee = coins[(i + 1) % coins.Length];

            // Combien de traits tiennent sur ce côté, et la place exacte de chacun.
            int nombreDeTraits = Mathf.CeilToInt(Vector2.Distance(depart, arrivee) / ecartEntreTraits);
            Vector2 pas = (arrivee - depart) / nombreDeTraits;

            for (int j = 0; j < nombreDeTraits; j++)
            {
                // Le trait est centré au milieu de sa case : relié (longueur x 2), il touche pile ses deux voisins.
                Vector2 centre = depart + pas * (j + 0.5f);
                CreerTrait(centre, pas);
            }
        }
    }

    // Crée un petit trait blanc (un cube aplati) posé sur le tissu, dans la direction du côté.
    private void CreerTrait(Vector2 centre, Vector2 pas)
    {
        GameObject trait = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trait.name = "Trait";
        Destroy(trait.GetComponent<Collider>()); // pas de collision : le trait ne doit pas bloquer les mains ni les rayons
        trait.GetComponent<Renderer>().sharedMaterial = materiauTrait;

        // On le range dans le tissu : il bouge avec lui. z = 0 = à la surface du tissu.
        trait.transform.SetParent(transform, false);
        trait.transform.localPosition = new Vector3(centre.x, centre.y, 0f);

        // On le tourne dans la direction du côté (Atan2 donne l'angle de la direction).
        float angle = Mathf.Atan2(pas.y, pas.x) * Mathf.Rad2Deg;
        trait.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        // x = longueur (la moitié de la case, pour laisser un trou), y et z = épaisseur.
        trait.transform.localScale = new Vector3(pas.magnitude / 2f, epaisseurTrait, epaisseurTrait);

        traitsPasRelies.Add(trait.transform);
    }

    // Prépare les petits bouts de tissu (un système de particules), sans en lancer aucun pour l'instant.
    private void CreerPapiers()
    {
        // Rangé à côté du tissu (pas dedans) : quand le tissu disparaît à la fin, les derniers bouts finissent de tomber.
        GameObject objet = new GameObject("PapiersDecoupe");
        objet.transform.SetParent(transform.parent, false);
        papiers = objet.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule reglages = papiers.main;
        reglages.simulationSpace = ParticleSystemSimulationSpace.World; // les bouts restent où ils sont lancés
        reglages.startLifetime = 0.7f;                                  // ils vivent 0,7 seconde
        reglages.startSize = new ParticleSystem.MinMaxCurve(0.004f, 0.01f); // carrés de 4 mm à 1 cm
        reglages.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f); // chacun tourné au hasard
        reglages.gravityModifier = 0.2f;                                // ils retombent doucement, comme du papier

        // On les lance nous-mêmes (Emit) : pas de flot automatique.
        ParticleSystem.EmissionModule flot = papiers.emission;
        flot.enabled = false;

        // Ils tournoient en tombant...
        ParticleSystem.RotationOverLifetimeModule tournoie = papiers.rotationOverLifetime;
        tournoie.enabled = true;
        tournoie.z = new ParticleSystem.MinMaxCurve(-8f, 8f);

        // ... et rapetissent jusqu'à disparaître (pas de "pop" sec à la fin).
        ParticleSystem.SizeOverLifetimeModule rapetisse = papiers.sizeOverLifetime;
        rapetisse.enabled = true;
        rapetisse.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Même matière que le tissu : ce sont bien des bouts du tissu.
        objet.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetComponent<Renderer>().sharedMaterial;
    }

    // Lance quelques bouts de tissu depuis la fente, vers le haut, chacun dans une direction un peu différente.
    private void LancerPapiers(Vector3 position)
    {
        ParticleSystem.EmitParams bout = new ParticleSystem.EmitParams();
        bout.position = position;
        for (int i = 0; i < papiersParTrait; i++)
        {
            bout.velocity = Vector3.up * 0.4f + Random.insideUnitSphere * 0.25f; // en mètres par seconde
            papiers.Emit(bout, 1);
        }
    }

    // Appelé par TraceurCutter avec la position de la pointe du cutter : relie les traits tout près d'elle.
    public void RelierTraitsPres(Vector3 positionPointe)
    {
        // On parcourt la liste à l'envers : retirer un élément ne fait alors sauter aucune case.
        for (int i = traitsPasRelies.Count - 1; i >= 0; i--)
        {
            Transform trait = traitsPasRelies[i];
            if (Vector3.Distance(trait.position, positionPointe) < rayonContact)
            {
                // Le trait devient une fente : il double de longueur (il bouche les deux demi-trous et touche ses voisins),
                // devient deux fois plus fin, et foncé comme l'ombre dans une vraie entaille.
                Vector3 taille = trait.localScale;
                trait.localScale = new Vector3(taille.x * 2f, taille.y / 2f, taille.z);
                trait.GetComponent<Renderer>().sharedMaterial = materiauDecoupe;
                traitsPasRelies.RemoveAt(i);

                // Petit "tic", un peu plus aigu ou plus grave à chaque fois pour que ce ne soit pas toujours le même.
                hautParleur.pitch = Random.Range(0.9f, 1.2f);
                hautParleur.PlayOneShot(sonDecoupe, volumeDecoupe);

                // Quelques petits bouts de tissu sautent de la fente.
                LancerPapiers(trait.position);
            }
        }

        // Plus aucun trait à relier : la découpe est finie, on prévient la scène (une seule fois).
        if (traitsPasRelies.Count == 0 && !toutEstRelie)
        {
            toutEstRelie = true;
            quandToutEstRelie.Invoke();
        }
    }
}
