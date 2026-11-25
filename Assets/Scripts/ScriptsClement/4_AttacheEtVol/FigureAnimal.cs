using UnityEngine;

// FONCTIONNALITÉ : les figures de l'animal. Quand 5 oiseaux tournent autour du cerf-volant et s'envolent tous
// (voir ObstaclesOiseaux), le cerf-volant fête ça : il fait une figure de SON animal (celui choisi au début du jeu), avec son bruit à lui.
// (Pas de bouton pour ces figures : c'est la récompense quand on a rassemblé les 5 oiseaux.)
// Chaque animal a 3 figures, choisies au hasard (jamais deux fois la même de suite) :
//  - Dragon (du feu, un rugissement) :
//      0) le crache-flamme : il s'élance en avant en tremblant et crache une petite flamme ;
//      1) le cercle de feu : il tourne sur lui-même en crachant, ça dessine un rond de flammes autour de lui ;
//      2) le feu d'artifice : il monte en tremblant et, tout en haut, crache une gerbe de flammes dans toutes les directions.
//  - Raie (de l'eau, des bulles) :
//      0) la vrille : elle fait un tour sur elle-même en éclaboussant ;
//      1) le jet d'eau : elle se balance sur place et souffle un jet d'eau par la tête, comme une baleine ;
//      2) le saut   : elle bondit hors de l'eau, dans une grande éclaboussure, puis replonge.
//  - Papillon (des paillettes roses, un carillon) :
//      0) la danse   : il monte un peu en se dandinant de gauche à droite ;
//      1) la pirouette : il monte en faisant 2 tours sur lui-même ;
//      2) le frisson   : il frétille très vite sur place, dans un gros nuage de paillettes.
// Aucune figure ne l'emmène sur les côtés : il monte, il tourne, il tremble, mais il reste au même endroit du ciel.
// (Les figures communes aux trois cerfs-volants sont sur le bouton B : voir FigureCerfVolant.)
// À mettre sur "PieceDecoupee", avec VolCerfVolant et ObstaclesOiseaux.
// Même principe que FigureCerfVolant : VolCerfVolant place le cerf-volant à chaque image ;
// nous, on AJOUTE la figure par-dessus après lui (LateUpdate), et on l'ENLÈVE au début de l'image suivante, avant lui (Update).
[DefaultExecutionOrder(-100)] // notre Update passe avant celui de VolCerfVolant
[RequireComponent(typeof(VolCerfVolant))]
public class FigureAnimal : MonoBehaviour
{
    [SerializeField] private float duree = 1.2f;            // une figure dure 1,2 seconde
    [SerializeField] private AudioClip sonDragon;           // le rugissement (SonDragon.wav)
    [SerializeField] private AudioClip sonRaie;             // les bulles (SonRaie.wav)
    [SerializeField] private AudioClip sonPapillon;         // le carillon (SonPapillon.wav)
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;
    [SerializeField] private float tailleEffet = 0.18f;     // la taille des flammes et des gouttes (grosses : le cerf-volant est loin)

    // Vrai pendant la figure (FigureCerfVolant attend qu'elle soit finie avant de lancer la sienne).
    public bool EnCours => temps >= 0f;

    private VolCerfVolant vol;
    private FigureCerfVolant figureCommune; // l'autre figure (bouton B), s'il y en a une : jamais les deux en même temps
    private ObstaclesOiseaux oiseaux;  // les oiseaux : ils nous préviennent quand ils s'envolent tous
    private bool demandee;             // vrai quand les oiseaux viennent de s'envoler : la figure part dès que possible
    private string animal;             // "Dragon", "Raie" ou "Papillon" (le nom de la forme choisie)
    private int numero;                // quelle figure de cet animal : 0, 1 ou 2 (voir la liste tout en haut)
    private Vector3 tete;              // où est sa tête, dans son repère à lui (le haut de la forme)
    private ParticleSystem effet;      // les flammes, les gouttes ou les paillettes (fabriqué à la première figure)
    private AudioSource hautParleur;
    private float temps = -1f;         // depuis combien de temps la figure dure (-1 = pas de figure en cours)
    private float aLancer;             // les flammes / gouttes / paillettes qu'il reste à lancer (on en lance un peu à chaque image)
    private Vector3 decalage;          // ce qu'on a ajouté à sa position à l'image d'avant
    private Quaternion tour = Quaternion.identity; // ce qu'on a ajouté à sa rotation à l'image d'avant

    private void Start()
    {
        vol = GetComponent<VolCerfVolant>();
        figureCommune = GetComponent<FigureCerfVolant>();
        numero = Random.Range(0, 3); // la toute première figure : au hasard aussi

        // On s'abonne à "CercleComplet" : ObstaclesOiseaux appellera Demander au moment où les 5 oiseaux s'envolent.
        oiseaux = GetComponent<ObstaclesOiseaux>();
        if (oiseaux != null)
            oiseaux.CercleComplet += Demander;
    }

    private void OnDestroy()
    {
        if (oiseaux != null)
            oiseaux.CercleComplet -= Demander;
    }

    // Les 5 oiseaux viennent de s'envoler : on veut une figure. (Elle démarre dans Update, dès que c'est possible.)
    private void Demander()
    {
        demandee = true;
    }

    private void OnDisable()
    {
        demandee = false;
        temps = -1f;
        decalage = Vector3.zero;
        tour = Quaternion.identity;
    }

    // Avant VolCerfVolant : on enlève la figure de l'image d'avant, et on regarde si une figure est demandée.
    private void Update()
    {
        transform.position -= decalage;
        transform.rotation *= Quaternion.Inverse(tour);
        decalage = Vector3.zero;
        tour = Quaternion.identity;

        // Il ne vole plus (tombé, ou poignées lâchées) : la figure demandée est oubliée.
        if (!vol.EnVol || vol.Tombe)
            demandee = false;

        // Une figure est demandée : elle démarre dès qu'aucune autre figure n'est en cours
        // (si un salto du bouton B est en train de se faire, elle attend qu'il soit fini).
        bool autreFigure = figureCommune != null && figureCommune.EnCours;
        if (demandee && temps < 0f && !autreFigure)
        {
            demandee = false;
            if (effet == null)
                Preparer();

            // Au hasard parmi les 2 autres figures : on avance de 1 ou 2 cases, et "% 3" fait revenir au début après la dernière.
            numero = (numero + Random.Range(1, 3)) % 3;
            temps = 0f;
            aLancer = animal == "Raie" && numero == 2 ? 20f : 0f; // le saut de la raie commence par une grande éclaboussure (20 gouttes d'un coup)

            AudioClip son = animal == "Dragon" ? sonDragon : animal == "Raie" ? sonRaie : sonPapillon;
            if (son != null)
                hautParleur.PlayOneShot(son, volume);
        }
    }

    // Après VolCerfVolant : on ajoute la figure par-dessus sa position et sa rotation, et on lance l'effet.
    private void LateUpdate()
    {
        if (temps < 0f)
            return; // pas de figure en cours

        temps += Time.deltaTime;
        if (temps >= duree || !vol.EnVol || vol.Tombe)
        {
            temps = -1f; // finie (ou il est tombé / on a lâché les poignées) : il reprend son vol normal
            return;
        }
        float t = temps / duree;                  // de 0 (début) à 1 (fin)
        float bosse = Mathf.Sin(t * Mathf.PI);    // monte de 0 à 1 puis redescend à 0 : rien au début, le plus fort au milieu, rien à la fin
        float doux = Mathf.SmoothStep(0f, 1f, t); // va de 0 à 1 comme "t", mais doucement au départ et à l'arrivée

        if (animal == "Dragon")
            FigureDragon(t, bosse, doux);
        else if (animal == "Raie")
            FigureRaie(t, bosse, doux);
        else
            FigurePapillon(t, bosse, doux);
    }

    // Dans les figures : "decalage" = de combien on le déplace, "tour" = de combien on le tourne, puis "Poser()" l'applique.
    // Dans son repère à lui : "right" = le long de ses ailes, "up" = vers sa tête, "forward" = à travers le tissu.

    private void FigureDragon(float t, float bosse, float doux)
    {
        Quaternion tremble = Quaternion.AngleAxis(Mathf.Sin(temps * 50f) * 5f * bosse, Vector3.forward); // il tremble comme s'il rugissait de toutes ses forces
        if (numero == 0)
        {
            // Le crache-flamme : il s'élance de 50 cm du côté de sa tête, et crache au milieu de la figure.
            decalage = transform.up * (0.5f * bosse);
            tour = tremble;
            Poser();
            if (t > 0.25f && t < 0.8f)
                Flammes(Combien(40f), tete, transform.up);
        }
        else if (numero == 1)
        {
            // Le cercle de feu : il fait un tour à plat en crachant tout le temps : sa tête tourne, donc les flammes partent tout autour.
            tour = Quaternion.AngleAxis(360f * doux, Vector3.forward);
            Poser();
            Flammes(Combien(40f), tete, transform.up);
        }
        else
        {
            // Le feu d'artifice : il monte de 80 cm en tremblant et, tout en haut (au milieu de la figure),
            // il crache une gerbe de flammes dans toutes les directions ("onUnitSphere" = une direction au hasard).
            decalage = Vector3.up * (0.8f * bosse);
            tour = tremble;
            Poser();
            if (t > 0.4f && t < 0.6f)
                for (int i = Combien(100f); i > 0; i--)
                    Lancer(transform.TransformPoint(tete), Random.onUnitSphere * Random.Range(1.2f, 2f), tailleEffet * Random.Range(0.7f, 1.3f), 0.5f);
        }
    }

    private void FigureRaie(float t, float bosse, float doux)
    {
        float angle = t * Mathf.PI * 2f; // un aller-retour complet pendant la figure
        if (numero == 0)
        {
            // La vrille : un tour complet (360°) sur elle-même, autour de l'axe tête-queue, en éclaboussant.
            tour = Quaternion.AngleAxis(360f * doux, Vector3.up);
            Poser();
            Eclaboussure(Combien(30f));
        }
        else if (numero == 1)
        {
            // Le jet d'eau : elle monte à peine (20 cm) en se balançant doucement (elle penche 2 fois de chaque côté, 15° au plus fort),
            // et souffle un jet d'eau par la tête, comme une baleine : les gouttes montent tout droit, puis retombent.
            decalage = Vector3.up * (0.2f * bosse);
            tour = Quaternion.AngleAxis(15f * Mathf.Sin(angle * 2f) * bosse, Vector3.forward);
            Poser();
            if (t > 0.15f && t < 0.75f)
                for (int i = Combien(35f); i > 0; i--)
                    Lancer(transform.TransformPoint(tete), Vector3.up * Random.Range(3.5f, 4.5f) + Random.insideUnitSphere * 0.5f, tailleEffet * 0.45f, 0.9f);
        }
        else
        {
            // Le saut : elle bondit de 1 m, le nez levé en montant puis baissé en replongeant. (La grande éclaboussure du départ : voir Update.)
            decalage = Vector3.up * bosse;
            tour = Quaternion.AngleAxis(-35f * Mathf.Sin(angle), Vector3.right);
            Poser();
            Eclaboussure(Combien(15f));
        }
    }

    private void FigurePapillon(float t, float bosse, float doux)
    {
        if (numero == 0)
        {
            // La danse : il monte de 40 cm et se dandine : il penche 3 fois à gauche et à droite (30° au plus fort).
            decalage = Vector3.up * (0.4f * bosse);
            tour = Quaternion.AngleAxis(Mathf.Sin(t * Mathf.PI * 6f) * 30f * bosse, Vector3.forward);
        }
        else if (numero == 1)
        {
            // La pirouette : il monte de 50 cm en faisant 2 tours (720°) sur lui-même, autour de l'axe tête-queue.
            decalage = Vector3.up * (0.5f * bosse);
            tour = Quaternion.AngleAxis(720f * doux, Vector3.up);
        }
        else
        {
            // Le frisson : il reste sur place et frétille très vite, comme s'il secouait ses ailes :
            // deux petits tremblements en même temps (il penche sur le côté, et d'avant en arrière), à des rythmes un peu différents.
            tour = Quaternion.AngleAxis(Mathf.Sin(temps * 45f) * 10f * bosse, Vector3.forward)
                 * Quaternion.AngleAxis(Mathf.Sin(temps * 38f) * 8f * bosse, Vector3.right);
        }
        Poser();

        // Les paillettes (des étoiles roses) jaillissent du milieu du papillon (deux fois plus pendant le frisson : un gros nuage).
        effet.transform.position = transform.position;
        effet.Emit(Combien(numero == 2 ? 25f : 12f));
    }

    // Applique la figure au cerf-volant (elle sera enlevée au début de l'image suivante, dans Update).
    private void Poser()
    {
        transform.position += decalage;
        transform.rotation *= tour;
    }

    // Combien de flammes / gouttes / paillettes lancer à cette image pour en avoir "parSeconde" chaque seconde.
    // "aLancer" garde les morceaux (ex. 0,4 flamme) pour l'image suivante.
    private int Combien(float parSeconde)
    {
        aLancer += parSeconde * Time.deltaTime;
        int entiers = (int)aLancer;
        aLancer -= entiers;
        return entiers;
    }

    // Des boules de feu : elles sortent d'un point du cerf-volant ("depart", dans son repère à lui : sa tête) et filent dans "direction",
    // avec un peu de hasard pour faire une flamme qui s'élargit.
    private void Flammes(int combien, Vector3 depart, Vector3 direction)
    {
        for (; combien > 0; combien--)
        {
            Vector3 elan = direction * Random.Range(1.5f, 2.2f) + Random.insideUnitSphere * 0.3f;
            Lancer(transform.TransformPoint(depart), elan, tailleEffet * Random.Range(0.7f, 1.3f), 0.45f);
        }
    }

    // Des gouttes : elles partent du milieu de la raie, dans des directions au hasard à plat sur le tissu (comme un tourniquet mouillé).
    private void Eclaboussure(int combien)
    {
        for (; combien > 0; combien--)
        {
            Vector3 direction = transform.right * Random.Range(-1f, 1f) + transform.up * Random.Range(-1f, 1f);
            Lancer(transform.position, direction.normalized * Random.Range(1f, 1.8f), tailleEffet * 0.5f, 0.6f);
        }
    }

    // Lance une flamme ou une goutte : d'où elle part, son élan, sa taille, et combien de temps elle vit (en secondes).
    private void Lancer(Vector3 depart, Vector3 elan, float taille, float vie)
    {
        ParticleSystem.EmitParams reglages = new ParticleSystem.EmitParams();
        reglages.position = depart;
        reglages.velocity = elan;
        reglages.startSize = taille;
        reglages.startLifetime = vie;
        effet.Emit(reglages, 1);
    }

    // À la première figure : on regarde quel animal c'est, où est sa tête, et on fabrique son effet et son haut-parleur.
    private void Preparer()
    {
        // L'animal = la forme de ce cerf-volant : elle est sur le Tissu, à côté de la pièce (dans le même atelier).
        PatronPointilles patron = transform.parent.GetComponentInChildren<PatronPointilles>(true);
        FormeCerfVolant forme = patron != null ? patron.Forme : null;
        animal = forme != null ? forme.name : "Papillon"; // (forme introuvable : on fait les figures du papillon)

        // Sa tête = le haut de la forme (y le plus grand), au milieu de ses ailes (x).
        if (forme != null)
        {
            float gauche = float.MaxValue, droite = float.MinValue, haut = float.MinValue;
            foreach (Vector2 coin in forme.Coins)
            {
                gauche = Mathf.Min(gauche, coin.x);
                droite = Mathf.Max(droite, coin.x);
                haut = Mathf.Max(haut, coin.y);
            }
            tete = new Vector3((gauche + droite) / 2f, haut, 0f);
        }

        // Rangés à côté du cerf-volant (pas dedans : les flammes et les gouttes ne tournent pas avec lui).
        GameObject objet = new GameObject("EffetFigureAnimal");
        objet.transform.SetParent(transform.parent, false);
        hautParleur = objet.AddComponent<AudioSource>();
        hautParleur.playOnAwake = false; // (son "2D" par défaut : on l'entend bien même si le cerf-volant est loin)

        if (animal == "Dragon")
            effet = CreerEffet(objet, new Color(1f, 0.95f, 0.3f), new Color(0.9f, 0.15f, 0.05f), -0.1f); // du jaune au rouge ; les flammes montent un peu
        else if (animal == "Raie")
            effet = CreerEffet(objet, new Color(0.75f, 0.92f, 1f), new Color(0.2f, 0.55f, 1f), 1f);      // du bleu clair au bleu ; les gouttes retombent
        else
            effet = EtoilesEtape.CreerEtoiles(objet, 0.8f, tailleEffet * 0.6f, new Color(1f, 0.5f, 0.8f), Color.white); // les étoiles de la découpe, en rose
    }

    // Fabrique des petites boules rondes (flammes ou gouttes) : chacune passe de la couleur "debut" à la couleur "fin"
    // en rapetissant, puis s'efface. "gravite" : positif = elles retombent, négatif = elles montent.
    private static ParticleSystem CreerEffet(GameObject objet, Color debut, Color fin, float gravite)
    {
        ParticleSystem boules = objet.AddComponent<ParticleSystem>();
        boules.Stop(); // on les lance nous-mêmes (Emit)
        ParticleSystem.MainModule reglages = boules.main;
        reglages.simulationSpace = ParticleSystemSimulationSpace.World;
        reglages.gravityModifier = gravite;
        reglages.playOnAwake = false;

        // Pas de jet automatique : c'est nous qui donnons le départ et l'élan de chaque boule (voir Lancer).
        ParticleSystem.EmissionModule flot = boules.emission;
        flot.enabled = false;
        ParticleSystem.ShapeModule forme = boules.shape;
        forme.enabled = false;

        // La couleur change pendant sa vie, et elle s'efface sur la fin.
        ParticleSystem.ColorOverLifetimeModule teinte = boules.colorOverLifetime;
        teinte.enabled = true;
        Gradient degrade = new Gradient();
        degrade.SetKeys(
            new[] { new GradientColorKey(debut, 0f), new GradientColorKey(fin, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        teinte.color = degrade;

        // Elle rapetisse jusqu'au tiers de sa taille.
        ParticleSystem.SizeOverLifetimeModule rapetisse = boules.sizeOverLifetime;
        rapetisse.enabled = true;
        rapetisse.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

        // Son image : un rond blanc (la couleur de chaque boule le teinte).
        Material matiere = new Material(Shader.Find("Sprites/Default")); // gère la transparence et la couleur des particules
        matiere.mainTexture = ImageRond(32);
        objet.GetComponent<ParticleSystemRenderer>().sharedMaterial = matiere;
        return boules;
    }

    // Dessine un rond blanc sur fond transparent, avec un bord doux (style dessin animé : une couleur unie, bien ronde).
    private static Texture2D ImageRond(int cote)
    {
        Texture2D image = new Texture2D(cote, cote, TextureFormat.RGBA32, false);
        image.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[cote * cote];
        for (int y = 0; y < cote; y++)
        {
            for (int x = 0; x < cote; x++)
            {
                float dx = x + 0.5f - cote / 2f, dy = y + 0.5f - cote / 2f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                byte alpha = (byte)(Mathf.Clamp01(cote * 0.48f - distance + 0.5f) * 255f); // 1 pixel de bord doux
                pixels[y * cote + x] = new Color32(255, 255, 255, alpha);
            }
        }
        image.SetPixels32(pixels);
        image.Apply();
        return image;
    }
}
