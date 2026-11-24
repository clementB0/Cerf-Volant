using UnityEngine;

// FONCTIONNALITÉ : un oiseau qui a frôlé le cerf-volant (sans le toucher) vient tourner calmement autour de lui,
// sur un cercle à plat autour du cerf-volant (le cercle le suit en douceur quand il bouge). Les oiseaux se répartissent sur le cercle.
// Quand il y en a assez, ou quand le cerf-volant est touché, ils s'envolent tous haut dans le ciel et disparaissent (voir ObstaclesOiseaux).
// Rien à poser dans la scène : ObstaclesOiseaux l'ajoute tout seul sur l'oiseau qui vient de frôler.
public class OiseauEnOrbite : MonoBehaviour
{
    private const float rayon = 0.65f;          // ils tournent à 65 cm du cerf-volant (autour des ailes, sans les toucher)
    private const float hauteur = 0.1f;         // un peu au-dessus du milieu du cerf-volant
    private const float toursParSeconde = 0.25f; // calmement : un tour en 4 secondes
    private const float inclinaison = 20f;      // il penche un peu vers l'intérieur du virage, comme un vrai oiseau
    private const float dureeArrivee = 1f;      // le temps de rejoindre sa place sur le cercle, en douceur
    private const float vitesseEnvol = 4f;      // l'envol : il monte à 4 m/s...
    private const float dureeEnvol = 3f;        // ... pendant 3 secondes, puis disparaît (il est alors très haut)

    private Transform cerfVolant;
    private Vector3 centre;       // le centre du cercle : il suit le cerf-volant en douceur (le cerf-volant tremble un peu dans le vent)
    private float decalage;      // sa place sur le cercle (de 0 à 1 tour), pour que les oiseaux soient bien répartis
    private Vector3 ecartDepart; // où il était par rapport au centre au moment du frôlement
    private float temps;
    private bool envol;
    private float tempsEnvol;
    private Vector3 vitesse;

    // Appelé par ObstaclesOiseaux : le numéro de sa place (0, 1, 2...) parmi "places" places sur le cercle.
    public void Rejoindre(Transform autourDe, int numero, int places)
    {
        cerfVolant = autourDe;
        centre = autourDe.position;
        decalage = numero / (float)places;
        ecartDepart = transform.position - centre;
    }

    // Appelé par ObstaclesOiseaux : il quitte le cercle et part vers le ciel.
    public void Envoler()
    {
        envol = true;
    }

    // LateUpdate : après que le cerf-volant a bougé dans cette image (sinon l'oiseau aurait une image de retard : il tremblerait).
    private void LateUpdate()
    {
        float dt = Time.deltaTime;

        if (!envol && cerfVolant != null)
        {
            // Le centre suit le cerf-volant en douceur.
            centre = Vector3.Lerp(centre, cerfVolant.position, 1f - Mathf.Exp(-4f * dt));

            // Sa place sur le cercle : tous les oiseaux tournent ensemble, chacun à sa place.
            temps += dt;
            float angle = (Time.time * toursParSeconde + decalage) * Mathf.PI * 2f;
            Vector3 surLeCercle = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * rayon + Vector3.up * hauteur;

            // Au début, il glisse de là où il était jusqu'à sa place (pas de saut).
            transform.position = centre + Vector3.Lerp(ecartDepart, surLeCercle, Mathf.SmoothStep(0f, 1f, temps / dureeArrivee));

            // Il regarde dans le sens du cercle (pas selon ses petites secousses), penché dans le virage, et s'y tourne en douceur.
            Vector3 sensDuCercle = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            Quaternion cap = Quaternion.LookRotation(sensDuCercle) * Quaternion.Euler(0f, 0f, inclinaison);
            transform.rotation = Quaternion.Slerp(transform.rotation, cap, 1f - Mathf.Exp(-5f * dt));

            vitesse = sensDuCercle * (2f * Mathf.PI * rayon * toursParSeconde); // son élan, pour un envol sans à-coup
        }
        else
        {
            // L'envol (ou le cerf-volant a disparu) : il part vers le haut, de plus en plus droit, puis disparaît.
            vitesse = Vector3.Lerp(vitesse, Vector3.up * vitesseEnvol, 1f - Mathf.Exp(-2f * dt));
            transform.position += vitesse * dt;
            if (vitesse.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(vitesse), 1f - Mathf.Exp(-5f * dt));

            tempsEnvol += dt;
            if (tempsEnvol > dureeEnvol)
                Destroy(gameObject);
        }
    }
}
