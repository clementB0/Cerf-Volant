using UnityEngine;
using UnityEngine.InputSystem;

// FONCTIONNALITÉ : les figures communes. Pendant le vol, on appuie sur le bouton B (manette droite) :
// le cerf-volant fait une figure choisie au hasard parmi 3 (les mêmes pour les trois cerfs-volants, jamais deux fois la même de suite) :
//  - le salto  : il s'envole d'un coup vers le haut et fait un tour complet vers l'avant ;
//  - la toupie : il monte un peu et tourne 2 fois sur lui-même, à plat, comme une hélice ;
//  - le rebond : il fait 3 petits bonds vers le haut, en hochant la tête.
// Aucune figure ne l'emmène sur les côtés : il monte, il tourne, mais il reste au même endroit du ciel.
// Avec des étoiles et le son de clochettes (les mêmes que pendant la découpe, voir EtoilesEtape).
// (Les figures de chaque animal se font toutes seules quand les 5 oiseaux s'envolent : voir FigureAnimal.)
// À mettre sur "PieceDecoupee", avec VolCerfVolant.
// Comment ça marche : VolCerfVolant place le cerf-volant à chaque image. Nous, on AJOUTE la figure par-dessus,
// après lui (LateUpdate), et on l'ENLÈVE au début de l'image suivante, avant lui (Update) : il ne voit jamais la figure,
// donc son calcul du vol n'est pas dérangé.
[DefaultExecutionOrder(-100)] // notre Update passe avant celui de VolCerfVolant (pour enlever la figure avant son calcul)
[RequireComponent(typeof(VolCerfVolant))]
public class FigureCerfVolant : MonoBehaviour
{
    [SerializeField] private float duree = 1.2f;            // une figure dure 1,2 seconde
    [SerializeField] private float hauteur = 1f;            // le salto monte de 1 m (la toupie et les bonds du rebond : la moitié)
    [SerializeField] private AudioClip sonFigure;           // le son de clochettes (SonEtoiles.wav)
    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;
    [SerializeField] private int nombreEtoiles = 8;         // combien d'étoiles jaillissent au départ
    [SerializeField] private float tempsEntreEtoiles = 0.12f; // pendant la figure : une étoile de plus toutes les 0,12 s (la traînée)
    [SerializeField] private float tailleEtoiles = 0.12f;   // plus grosses que pour la découpe : le cerf-volant est loin
    [SerializeField] private float vitesseEtoiles = 1.5f;   // à quelle vitesse elles s'envolent (mètres par seconde)

    // Vrai pendant la figure (FigureAnimal attend qu'elle soit finie avant de lancer la sienne).
    public bool EnCours => temps >= 0f;

    private VolCerfVolant vol;
    private FigureAnimal figureAnimal; // l'autre figure (celle de l'animal), s'il y en a une : jamais les deux en même temps
    private InputAction boutonB;       // le bouton B de la manette droite
    private ParticleSystem etoiles;
    private AudioSource hautParleur;
    private int numero;                // quelle figure : 0 = salto, 1 = toupie, 2 = rebond
    private float temps = -1f;         // depuis combien de temps la figure dure (-1 = pas de figure en cours)
    private float prochaineEtoile;     // le moment de la prochaine étoile de la traînée
    private Vector3 decalage;          // ce qu'on a ajouté à sa position à l'image d'avant
    private Quaternion tour = Quaternion.identity; // ce qu'on a ajouté à sa rotation à l'image d'avant

    private void Start()
    {
        vol = GetComponent<VolCerfVolant>();
        figureAnimal = GetComponent<FigureAnimal>();
        numero = Random.Range(0, 3); // la toute première figure : au hasard aussi

        // Les étoiles et le son, rangés à côté du cerf-volant (pas dedans : les étoiles ne tournent pas avec lui).
        GameObject objet = new GameObject("EtoilesFigure");
        objet.transform.SetParent(transform.parent, false);
        etoiles = EtoilesEtape.CreerEtoiles(objet, vitesseEtoiles, tailleEtoiles, new Color(1f, 0.85f, 0.2f), Color.white);
        hautParleur = objet.AddComponent<AudioSource>();
        hautParleur.playOnAwake = false; // (son "2D" par défaut : on l'entend bien même si le cerf-volant est loin)
    }

    private void OnEnable()
    {
        // "{SecondaryButton}" = le 2e bouton de la manette : B sur la manette droite d'un Quest.
        boutonB = new InputAction(binding: "<XRController>{RightHand}/{SecondaryButton}");
        boutonB.Enable();
    }

    private void OnDisable()
    {
        boutonB.Dispose(); // on arrête d'écouter le bouton
        temps = -1f;       // figure coupée net : rien à enlever à la prochaine apparition
        decalage = Vector3.zero;
        tour = Quaternion.identity;
    }

    // Avant VolCerfVolant : on enlève la figure de l'image d'avant, et on regarde le bouton.
    private void Update()
    {
        transform.position -= decalage;
        transform.rotation *= Quaternion.Inverse(tour);
        decalage = Vector3.zero;
        tour = Quaternion.identity;

        // Bouton B : une figure démarre, seulement s'il vole (pas tombé) et qu'aucune figure n'est déjà en cours.
        bool autreFigure = figureAnimal != null && figureAnimal.EnCours;
        if (temps < 0f && !autreFigure && boutonB.WasPressedThisFrame() && vol.EnVol && !vol.Tombe)
        {
            // Au hasard parmi les 2 autres figures : on avance de 1 ou 2 cases, et "% 3" fait revenir au début après la dernière.
            numero = (numero + Random.Range(1, 3)) % 3;
            temps = 0f;
            etoiles.transform.position = transform.position;
            etoiles.Emit(nombreEtoiles);
            if (sonFigure != null)
                hautParleur.PlayOneShot(sonFigure, volume);
        }
    }

    // Après VolCerfVolant : on ajoute la figure par-dessus sa position et sa rotation.
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
        float t = temps / duree;               // de 0 (début) à 1 (fin)
        float bosse = Mathf.Sin(t * Mathf.PI); // monte de 0 à 1 puis redescend à 0 : rien au début, le plus fort au milieu, rien à la fin
        float doux = Mathf.SmoothStep(0f, 1f, t); // va de 0 à 1 comme "t", mais doucement au départ et à l'arrivée

        // Dans son repère à lui : "right" = le long de ses ailes, "up" = vers sa tête, "forward" = à travers le tissu.
        if (numero == 0)
        {
            // Le salto : il monte de "hauteur" puis redescend, en faisant un tour complet (360°) autour de ses ailes.
            decalage = Vector3.up * (hauteur * bosse);
            tour = Quaternion.AngleAxis(360f * doux, Vector3.right);
        }
        else if (numero == 1)
        {
            // La toupie : il monte un peu et fait 2 tours (720°) à plat, comme une hélice.
            decalage = Vector3.up * (hauteur * 0.5f * bosse);
            tour = Quaternion.AngleAxis(720f * doux, Vector3.forward);
        }
        else
        {
            // Le rebond : 3 petits bonds vers le haut ("Abs" garde la bosse du sinus toujours vers le haut : ça fait 3 bosses),
            // en hochant la tête d'avant en arrière à chaque bond (20° au plus fort).
            decalage = Vector3.up * (hauteur * 0.5f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f)));
            tour = Quaternion.AngleAxis(20f * Mathf.Sin(t * Mathf.PI * 6f) * bosse, Vector3.right);
        }

        transform.position += decalage;
        transform.rotation *= tour;

        // Une petite traînée d'étoiles le suit pendant la figure (une étoile de temps en temps, pas à chaque image).
        if (Time.time >= prochaineEtoile)
        {
            prochaineEtoile = Time.time + tempsEntreEtoiles;
            etoiles.transform.position = transform.position;
            etoiles.Emit(1);
        }
    }
}
