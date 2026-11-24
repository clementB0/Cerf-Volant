using UnityEngine;

// FONCTIONNALITÉ : un oiseau qui a frôlé le cerf-volant (sans le toucher) fait une figure, choisie au hasard :
//  - Tour : un tour complet autour du cerf-volant (un cercle face au joueur) ;
//  - Spirale : deux tours à plat autour du cerf-volant en montant d'un mètre (un tire-bouchon) ;
//  - Huit : un 8 couché au-dessus du cerf-volant ;
//  - Looping : une boucle verticale au-dessus du cerf-volant, dans son sens de vol.
// Puis il s'envole haut dans le ciel et disparaît.
// Rien à poser dans la scène : ObstaclesOiseaux l'ajoute tout seul sur l'oiseau qui vient de frôler.
public class FigureOiseau : MonoBehaviour
{
    private enum Figure { Tour, Spirale, Huit, Looping }

    private const float vitesseEnvol = 4f;    // après la figure, il monte à 4 m/s...
    private const float dureeEnvol = 3f;      // ... pendant 3 secondes, et disparaît (il est alors très haut)

    private Figure figure;
    private float duree;            // la durée de la figure choisie
    private Transform cerfVolant;
    private Vector3 droite, haut;   // le plan face au joueur (perpendiculaire à son regard)
    private Vector3 profondeur;     // à plat, du joueur vers le cerf-volant
    private Vector3 sens;           // à plat, la direction dans laquelle volait l'oiseau
    private float rayon;
    private float angleDepart;
    private float temps;
    private Vector3 ecartDepart;    // où il était par rapport au cerf-volant au moment du frôlement
    private Vector3 vitesse;        // pour l'envol

    // Appelé par ObstaclesOiseaux au moment du frôlement, avec la direction dans laquelle volait l'oiseau.
    public void Lancer(Transform autourDe, Vector3 direction)
    {
        cerfVolant = autourDe;

        // Les directions utiles, vues par le joueur.
        Vector3 regard = Camera.main != null ? cerfVolant.position - Camera.main.transform.position : Vector3.forward;
        profondeur = Vector3.ProjectOnPlane(regard, Vector3.up).normalized;
        if (profondeur == Vector3.zero)
            profondeur = Vector3.forward;
        droite = Vector3.Cross(Vector3.up, profondeur).normalized;
        haut = Vector3.Cross(regard, droite).normalized;
        sens = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
        if (sens == Vector3.zero)
            sens = profondeur;

        // La figure, au hasard, et combien de temps elle dure.
        figure = (Figure)Random.Range(0, 4);
        duree = figure == Figure.Tour ? 1.6f : figure == Figure.Looping ? 1.4f : 2.4f;

        // Les cercles commencent du côté où il est, à sa distance (au moins 50 cm : il tourne autour des ailes, sans passer dedans).
        ecartDepart = transform.position - cerfVolant.position;
        rayon = Mathf.Clamp(ecartDepart.magnitude, 0.5f, 0.8f);
        angleDepart = figure == Figure.Spirale
            ? Mathf.Atan2(Vector3.Dot(ecartDepart, profondeur), Vector3.Dot(ecartDepart, droite))
            : Mathf.Atan2(Vector3.Dot(ecartDepart, haut), Vector3.Dot(ecartDepart, droite));
    }

    private void Update()
    {
        temps += Time.deltaTime;
        Vector3 avant = transform.position;

        if (temps < duree && cerfVolant != null)
        {
            // 1) La figure, autour du cerf-volant (qui bouge : la figure le suit), en douceur au début et à la fin.
            Vector3 surLaFigure = Ecart(Mathf.SmoothStep(0f, 1f, temps / duree));
            // Les 0,3 premières secondes, il glisse de là où il était jusqu'à la figure (pas de saut).
            transform.position = cerfVolant.position + Vector3.Lerp(ecartDepart, surLaFigure, Mathf.SmoothStep(0f, 1f, temps / 0.3f));
            vitesse = (transform.position - avant) / Mathf.Max(Time.deltaTime, 0.001f);
        }
        else
        {
            // 2) L'envol : il part vers le haut, en gardant un peu de son élan, de plus en plus droit vers le ciel.
            vitesse = Vector3.Lerp(vitesse, Vector3.up * vitesseEnvol, 3f * Time.deltaTime);
            transform.position += vitesse * Time.deltaTime;
            if (temps > duree + dureeEnvol)
                Destroy(gameObject);
        }

        // Il regarde là où il va.
        if (transform.position != avant)
            transform.rotation = Quaternion.LookRotation(transform.position - avant);
    }

    // Où est l'oiseau par rapport au cerf-volant, à un moment de la figure (s = 0 au début, 1 à la fin).
    private Vector3 Ecart(float s)
    {
        float tour = s * Mathf.PI * 2f; // un tour complet sur toute la figure
        switch (figure)
        {
            case Figure.Tour:
                // Un cercle face au joueur.
                float a = angleDepart + tour;
                return (droite * Mathf.Cos(a) + haut * Mathf.Sin(a)) * rayon;

            case Figure.Spirale:
                // Deux cercles à plat autour du cerf-volant, en montant d'un mètre.
                float b = angleDepart + tour * 2f;
                return (droite * Mathf.Cos(b) + profondeur * Mathf.Sin(b)) * rayon + Vector3.up * s;

            case Figure.Huit:
                // Un 8 couché, 50 cm au-dessus du centre du cerf-volant : il passe au-dessus de lui, jamais à travers.
                return droite * (0.9f * Mathf.Sin(tour)) + haut * (0.5f + 0.3f * Mathf.Sin(tour * 2f));

            default:
                // Looping : une boucle verticale de 45 cm de rayon, centrée 85 cm au-dessus du cerf-volant,
                // qui part du bas de la boucle et tourne dans le sens où il volait.
                return Vector3.up * 0.85f + (sens * Mathf.Sin(tour) - Vector3.up * Mathf.Cos(tour)) * 0.45f;
        }
    }
}
