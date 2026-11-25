using UnityEngine;
using UnityEngine.InputSystem;

// FONCTIONNALITÉ : la figure. Pendant le vol, on appuie sur le bouton B (manette droite) :
// le cerf-volant s'envole d'un coup vers le haut, fait un salto avant (un tour complet), puis revient à sa place,
// avec des étoiles et le son de clochettes (les mêmes que pendant la découpe, voir EtoilesEtape).
// À mettre sur "PieceDecoupee", avec VolCerfVolant.
// Comment ça marche : VolCerfVolant place le cerf-volant à chaque image. Nous, on AJOUTE la figure par-dessus,
// après lui (LateUpdate), et on l'ENLÈVE au début de l'image suivante, avant lui (Update) : il ne voit jamais la figure,
// donc son calcul du vol n'est pas dérangé.
[DefaultExecutionOrder(-100)] // notre Update passe avant celui de VolCerfVolant (pour enlever la figure avant son calcul)
[RequireComponent(typeof(VolCerfVolant))]
public class FigureCerfVolant : MonoBehaviour
{
    [SerializeField] private float duree = 1.2f;            // la figure dure 1,2 seconde
    [SerializeField] private float hauteur = 1f;            // il monte de 1 m pendant la figure
    [SerializeField] private AudioClip sonFigure;           // le son de clochettes (SonEtoiles.wav)
    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;
    [SerializeField] private int nombreEtoiles = 25;        // combien d'étoiles jaillissent au départ
    [SerializeField] private float tailleEtoiles = 0.12f;   // plus grosses que pour la découpe : le cerf-volant est loin
    [SerializeField] private float vitesseEtoiles = 1.5f;   // à quelle vitesse elles s'envolent (mètres par seconde)

    private VolCerfVolant vol;
    private InputAction boutonB;       // le bouton B de la manette droite
    private ParticleSystem etoiles;
    private AudioSource hautParleur;
    private float temps = -1f;         // depuis combien de temps la figure dure (-1 = pas de figure en cours)
    private Vector3 decalage;          // ce qu'on a ajouté à sa position à l'image d'avant
    private Quaternion tour = Quaternion.identity; // ce qu'on a ajouté à sa rotation à l'image d'avant

    private void Start()
    {
        vol = GetComponent<VolCerfVolant>();

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

        // Bouton B : la figure démarre, seulement s'il vole (pas tombé) et qu'il n'en fait pas déjà une.
        if (temps < 0f && boutonB.WasPressedThisFrame() && vol.EnVol && !vol.Tombe)
        {
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
        float t = temps / duree; // de 0 (début) à 1 (fin)

        // Il s'envole : une bosse, il monte de "hauteur" puis redescend à sa place.
        decalage = Vector3.up * (hauteur * Mathf.Sin(t * Mathf.PI));
        // Le salto avant : un tour complet (360°) autour de ses ailes, doux au départ et à l'arrivée.
        tour = Quaternion.AngleAxis(360f * Mathf.SmoothStep(0f, 1f, t), Vector3.right);

        transform.position += decalage;
        transform.rotation *= tour;

        // Une traînée d'étoiles le suit pendant toute la figure (une par image).
        etoiles.transform.position = transform.position;
        etoiles.Emit(1);
    }
}
