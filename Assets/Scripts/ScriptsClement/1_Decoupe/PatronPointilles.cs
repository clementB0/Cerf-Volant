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
    [Tooltip("Points d'un tracé ouvert. Si la liste est vide, le contour fermé de la forme est utilisé.")]
    [SerializeField] private Vector2[] traceOuvert;
    public IReadOnlyList<Vector2> TraceOuvert => traceOuvert;

    [SerializeField] private float ecartEntreTraits = 0.06f; // un trait tous les 6 cm (le trait fait la moitié : 3 cm)
    [SerializeField] private float epaisseurTrait = 0.01f;   // 1 cm (la fente fait la moitié : 5 mm)
    [SerializeField] private Material materiauTrait;         // couleur des traits (Pointilles.mat : bleu, visible sur le tissu blanc)
    [SerializeField] private float rayonContact = 0.02f;     // un trait se relie si la pointe du cutter passe à moins de 2 cm
    [SerializeField] private Material materiauDecoupe;       // couleur de la fente une fois découpé (presque noir : Fente.mat)
    [SerializeField] private AudioClip sonDecoupe;           // petit "tic" joué à chaque trait découpé (SonDecoupe.wav)
    [SerializeField] private float volumeDecoupe = 0.4f;     // léger
    [SerializeField] private int papiersParTrait = 6;        // combien de petits bouts de tissu sautent à chaque trait découpé
    [SerializeField] private int nombreEtapes = 4;           // le contour est découpé en 4 morceaux, qui apparaissent l'un après l'autre
    [SerializeField] private float opacitePale = 0.35f;      // les pointillés pas encore découpables sont visibles à 35 % (1 = blancs comme les autres)
    [SerializeField] private float attenteDebut = 0.7f;     // le temps que l'atelier finisse d'arriver sur la table (ChoixCerfVolant) avant le 1er mouvement

    // Ce qui se passe quand tous les traits sont reliés. Se règle dans l'Inspector (liste "Quand Tout Est Relie").
    // Dans la scène : affiche la PieceDecoupee et cache le Tissu.
    [SerializeField] private UnityEvent quandToutEstRelie;

    private List<Transform> tousLesTraits = new List<Transform>();   // tous les traits, dans l'ordre du contour
    private List<Vector2> debutsTraits = new List<Vector2>();        // où commence chaque trait sur le tissu (pour découper les chutes)
    private Material materiauPale;                                   // les pointillés des étapes suivantes (pâles)
    private List<List<Transform>> etapes = new List<List<Transform>>(); // pour chaque étape : ses traits pas encore découpés
    private int etapeEnCours;                                        // l'étape qu'on est en train de découper (0 = la première)
    private bool attendLaSuite;                                      // vrai pendant que le tissu tourne, avant d'afficher l'étape suivante
    private TourneTissu tourneur;                                    // fait tourner le tissu entre deux étapes (facultatif)
    private ChutesTissu chutes;                                      // fait tomber le tissu en trop à chaque étape (facultatif)
    private float debut;                                             // le moment du tout premier mouvement
    private bool premierMouvementFait;
    private bool toutEstRelie;                                       // devient vrai une seule fois, à la fin
    private bool finirApresMouvement;                                // vrai pendant le mouvement final, avant de prévenir la scène
    private AudioSource hautParleur;                                 // d'où sort le "tic" : le tissu
    private ParticleSystem papiers;                                  // les petits bouts de tissu qui sautent pendant la découpe

    // La forme, en lecture seule, pour que PieceDecoupee ait exactement la même.
    public FormeCerfVolant Forme => forme;

    // La distance de découpe, en lecture seule, pour que TraceurCutter dessine la zone de coupe dans la vue Scene.
    public float RayonContact => rayonContact;

    // Pour l'aide à la découpe (AideDecoupe) : les traits encore à découper de l'étape en cours, dans l'ordre du contour
    // (rien pendant que le tissu tourne ou une fois tout découpé)...
    public List<Transform> TraitsEnCours => attendLaSuite || toutEstRelie || etapes.Count == 0 ? null : etapes[etapeEnCours];
    // ... et le moment de la dernière action du joueur (un trait découpé, ou une nouvelle étape apparue).
    public float DerniereActivite { get; private set; }
    // Le moment du dernier trait vraiment découpé (pas les nouvelles étapes). "Jamais" au début.
    // Lu par TraceurCutter (vibrer seulement en découpant) et AideDecoupe (cacher le fantôme pendant qu'on découpe).
    public float DerniereDecoupe { get; private set; } = float.NegativeInfinity;

    // Prévient quand une étape est finie (EtoilesEtape s'en sert), avec l'endroit du dernier trait découpé.
    public event System.Action<Vector3> EtapeFinie;

    // Appelé par ChoixCerfVolant quand on choisit un cerf-volant dans le menu (avant que le tissu apparaisse).
    // Raccourci (RaccourciMontage) : fait comme si toute la découpe venait d'être finie.
    // On prévient la scène tout de suite (la pièce apparaît, le tissu disparaît), sauf si c'était déjà fait.
    public void FinirTout()
    {
        if (toutEstRelie && !finirApresMouvement)
            return; // la découpe était déjà finie et la scène déjà prévenue
        toutEstRelie = true;
        finirApresMouvement = false;
        quandToutEstRelie.Invoke();
    }

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

        bool utiliserTraceOuvert = traceOuvert != null && traceOuvert.Length >= 2;
        if (!utiliserTraceOuvert && forme == null)
        {
            Debug.LogError("PatronPointilles : rien à découper. Remplis \"Forme\" (un cerf-volant) ou \"Trace Ouvert\" (au moins 2 points) dans l'Inspector.", this);
            enabled = false;
            return;
        }
        Vector2[] coins = utiliserTraceOuvert ? traceOuvert : forme.Coins;
        int nombreDeCotes = utiliserTraceOuvert ? coins.Length - 1 : coins.Length;

        // On relie chaque coin au suivant. Un contour fermé relie aussi le dernier coin au premier (le "% coins.Length") ;
        // un tracé ouvert s'arrête au dernier point.
        for (int i = 0; i < nombreDeCotes; i++)
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
                debutsTraits.Add(depart + pas * j); // le début de sa case
            }
        }

        // Les pointillés pâles : on mélange le blanc des traits avec la couleur du tissu.
        // (Le tissu est uni : ça donne exactement le même effet qu'un blanc à moitié transparent.)
        materiauPale = new Material(materiauTrait);
        materiauPale.color = Color.Lerp(GetComponent<Renderer>().sharedMaterial.color, materiauTrait.color, opacitePale);

        // On range les traits en étapes : des morceaux du contour qui se suivent (le 1er quart, le 2e quart...).
        // Au départ, tous sont pâles : le tissu va d'abord tourner pour mettre la 1re étape devant le joueur.
        int nombre = Mathf.Clamp(nombreEtapes, 1, tousLesTraits.Count);
        Vector2[] debutsEtapes = new Vector2[nombre]; // où commence chaque étape sur le contour
        for (int e = 0; e < nombre; e++)
            etapes.Add(new List<Transform>());
        for (int i = 0; i < tousLesTraits.Count; i++)
        {
            int e = i * nombre / tousLesTraits.Count; // de 0 à nombre-1, dans l'ordre du contour
            if (etapes[e].Count == 0)
                debutsEtapes[e] = debutsTraits[i]; // le premier trait de l'étape
            etapes[e].Add(tousLesTraits[i]);
            tousLesTraits[i].GetComponent<Renderer>().sharedMaterial = materiauPale;
        }

        // Le tissu est redécoupé : la forme du cerf-volant + une chute par étape (elle tombera quand l'étape sera découpée).
        chutes = GetComponent<ChutesTissu>();
        if (chutes != null)
            chutes.Preparer(coins, debutsEtapes);

        tourneur = GetComponent<TourneTissu>();
        attendLaSuite = true;
        debut = Time.time + attenteDebut;
    }

    private void Update()
    {
        // Le tout premier mouvement, une fois l'atelier arrivé sur la table : la 1re étape vient devant le joueur.
        if (!premierMouvementFait && Time.time >= debut)
        {
            premierMouvementFait = true;
            if (tourneur != null)
                tourneur.Tourner(CentreEtape(0), false);
        }

        bool immobile = tourneur == null || !tourneur.EnMouvement;

        // Le tissu a fini de tourner : les pointillés de l'étape en cours deviennent blancs, on peut les découper.
        if (attendLaSuite && premierMouvementFait && immobile)
        {
            foreach (Transform trait in etapes[etapeEnCours])
                trait.GetComponent<Renderer>().sharedMaterial = materiauTrait;
            attendLaSuite = false;
            DerniereActivite = Time.time; // une nouvelle étape : le compte d'inactivité repart de zéro
        }

        // Le mouvement final est fini (le cerf-volant est face au joueur) : on prévient la scène, une seule fois.
        if (finirApresMouvement && immobile)
        {
            finirApresMouvement = false;
            quandToutEstRelie.Invoke();
        }
    }

    // Le milieu des traits d'une étape, sur le tissu (à appeler avant de les découper : la liste se vide pendant la découpe).
    private Vector3 CentreEtape(int e)
    {
        Vector3 somme = Vector3.zero;
        foreach (Transform trait in etapes[e])
            somme += trait.localPosition;
        return somme / etapes[e].Count;
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

        tousLesTraits.Add(trait.transform);
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
        RelierTraitsPres(positionPointe, positionPointe);
    }

    public void MasquerTrace()
    {
        foreach (Transform trait in tousLesTraits)
            trait.gameObject.SetActive(false);
    }

    public void RelierTraitsPres(Vector3 positionPrecedente, Vector3 positionPointe)
    {
        // Pendant que le tissu tourne, une fois tout découpé, ou si les pointillés n'ont pas pu être créés : on ne découpe pas.
        if (attendLaSuite || toutEstRelie || etapes.Count == 0)
            return;

        // Seuls les traits de l'étape en cours peuvent être découpés.
        List<Transform> traitsPasRelies = etapes[etapeEnCours];
        Vector3 dernierTrait = positionPointe; // l'endroit du dernier trait découpé (pour les étoiles de fin d'étape)

        // On parcourt la liste à l'envers : retirer un élément ne fait alors sauter aucune case.
        Vector3 mouvement = positionPointe - positionPrecedente;
        float amplitude = mouvement.sqrMagnitude;
        for (int i = traitsPasRelies.Count - 1; i >= 0; i--)
        {
            Transform trait = traitsPasRelies[i];
            Vector3 contact = positionPointe;
            if (amplitude > 0f)
            {
                float progression = Mathf.Clamp01(Vector3.Dot(trait.position - positionPrecedente, mouvement) / amplitude);
                contact = positionPrecedente + progression * mouvement;
            }
            if ((trait.position - contact).sqrMagnitude < rayonContact * rayonContact)
            {
                // Le trait devient une fente : il double de longueur (il bouche les deux demi-trous et touche ses voisins),
                // devient deux fois plus fin, et foncé comme l'ombre dans une vraie entaille.
                Vector3 taille = trait.localScale;
                trait.localScale = new Vector3(taille.x * 2f, taille.y / 2f, taille.z);
                trait.GetComponent<Renderer>().sharedMaterial = materiauDecoupe;
                traitsPasRelies.RemoveAt(i);
                dernierTrait = trait.position;
                DerniereActivite = Time.time; // le joueur découpe : il n'est pas inactif
                DerniereDecoupe = Time.time;

                // Petit "tic", un peu plus aigu ou plus grave à chaque fois pour que ce ne soit pas toujours le même.
                hautParleur.pitch = Random.Range(0.9f, 1.2f);
                hautParleur.PlayOneShot(sonDecoupe, volumeDecoupe);

                // Quelques petits bouts de tissu sautent de la fente.
                LancerPapiers(trait.position);
            }
        }

        if (traitsPasRelies.Count > 0)
            return; // il reste des traits dans cette étape

        // Étape finie : des étoiles (si EtoilesEtape est là), et le morceau de tissu en trop, derrière ces pointillés, tombe.
        EtapeFinie?.Invoke(dernierTrait);
        if (chutes != null)
            chutes.FaireTomber(etapeEnCours);

        // Étape finie. C'était la dernière : la découpe est finie.
        // Le tissu se met face au joueur, puis on prévient la scène (voir Update) : la pièce apparaîtra dans cette position.
        if (etapeEnCours == etapes.Count - 1)
        {
            toutEstRelie = true;
            if (tourneur != null)
            {
                tourneur.MettreFaceAuJoueur();
                finirApresMouvement = true;
            }
            else
            {
                quandToutEstRelie.Invoke();
            }
            return;
        }

        // Sinon, on passe à l'étape suivante : le tissu tourne pour la mettre devant le joueur
        // (en se retournant une fois sur deux), puis ses pointillés apparaîtront (voir Update).
        etapeEnCours++;
        attendLaSuite = true;
        if (tourneur != null)
            tourneur.Tourner(CentreEtape(etapeEnCours), etapeEnCours % 2 == 0);
    }
}
