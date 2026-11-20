using System.Collections;
using UnityEngine;

// FONCTIONNALITÉ : quand la découpe est finie, le cerf-volant fait "pouf" et réapparaît debout sur un présentoir
// (un chevalet de peintre) devant le joueur, face à lui, comme une toile prête à être peinte.
// À mettre sur l'objet "PieceDecoupee" : il apparaît pile à la fin de la découpe, donc son Start = "découpe finie".
public class PresentoirCerfVolant : MonoBehaviour
{
    [SerializeField] private ParticleSystem fumeePrefab;     // le nuage de fumée (le même que pour les bombes : FumeeTransition)
    [SerializeField] private AudioClip sonPouf;              // le "pouf !" (SonFumee.wav)
    [SerializeField] private Material materiauPresentoir;    // la couleur du chevalet (Table.mat : bois marron)
    [SerializeField] private float attente = 0.9f;           // on laisse d'abord la pièce faire son petit saut de victoire (ReussiteDecoupe)
    [SerializeField] private float distance = 0.8f;          // le présentoir est à 80 cm devant le joueur
    [SerializeField] private float hauteurSousLesYeux = 0.3f; // le milieu du cerf-volant est 30 cm sous les yeux (à hauteur de poitrine)

    private void Start()
    {
        StartCoroutine(AllerSurLePresentoir());
    }

    private IEnumerator AllerSurLePresentoir()
    {
        yield return new WaitForSeconds(attente);

        // 1) Pouf là où il est.
        Pouf(transform.position);
        yield return new WaitForSeconds(0.2f); // le temps que la fumée le cache

        // 2) Où se mettre : devant le joueur, dans la direction où il regarde (à plat), à hauteur de poitrine.
        // shortcut: sans caméra principale, rien ne bouge ; c'est le cas normal en VR d'en avoir une (la tête du joueur).
        Transform tete = Camera.main != null ? Camera.main.transform : null;
        if (tete == null)
            yield break;
        Vector3 devant = Vector3.ProjectOnPlane(tete.forward, Vector3.up).normalized;
        if (devant == Vector3.zero)
            devant = Vector3.forward; // il regarde pile en haut ou en bas : on prend une direction par défaut
        Vector3 sol = Vector3.ProjectOnPlane(tete.position, Vector3.up) + devant * distance; // au sol, devant lui (le sol est à y = 0)
        Vector3 centre = sol + Vector3.up * Mathf.Max(tete.position.y - hauteurSousLesYeux, 0.8f);

        // 3) Le cerf-volant debout : sa face peinte vers le joueur (son "dos" vers l'avant), sa tête vers le ciel.
        transform.SetPositionAndRotation(centre, Quaternion.LookRotation(devant, Vector3.up));

        // 4) Le présentoir autour de lui, puis pouf là où il apparaît.
        ConstruirePresentoir(sol, centre, devant);
        Pouf(centre);
    }

    // Un chevalet de peintre en 4 baguettes de bois : deux pieds devant, un pied derrière, et un rebord sous la toile.
    private void ConstruirePresentoir(Vector3 sol, Vector3 centre, Vector3 devant)
    {
        Vector3 droite = Vector3.Cross(Vector3.up, devant);           // la droite du joueur
        float demi = GetComponent<Renderer>().bounds.extents.y;       // la moitié de la hauteur du cerf-volant
        Vector3 derriere = devant * 0.03f;                            // les pieds sont juste derrière la toile
        Vector3 haut = centre + Vector3.up * (demi + 0.1f) + derriere; // le sommet du chevalet, un peu au-dessus de la toile

        GameObject presentoir = new GameObject("Presentoir");
        Baguette(presentoir, sol - droite * 0.2f + derriere, haut);    // pied avant gauche
        Baguette(presentoir, sol + droite * 0.2f + derriere, haut);    // pied avant droit
        Baguette(presentoir, sol + devant * 0.4f, haut);               // pied arrière (penché vers l'arrière)

        // Le rebord : une baguette couchée, juste sous le bas du cerf-volant, qui le "porte".
        Vector3 rebord = centre - Vector3.up * (demi + 0.01f);
        Baguette(presentoir, rebord - droite * 0.25f, rebord + droite * 0.25f);
    }

    // Une baguette de bois (un cube très allongé) qui va d'un point à un autre.
    private void Baguette(GameObject parent, Vector3 de, Vector3 a)
    {
        GameObject baguette = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baguette.name = "Baguette";
        baguette.transform.SetParent(parent.transform, false);
        Destroy(baguette.GetComponent<Collider>()); // pas de collision : il ne doit pas bloquer le jet de peinture
        baguette.GetComponent<Renderer>().sharedMaterial = materiauPresentoir;

        // Au milieu des deux points, tournée vers le second, aussi longue que l'écart entre eux, et épaisse de 2,5 cm.
        baguette.transform.position = (de + a) / 2f;
        baguette.transform.rotation = Quaternion.LookRotation(a - de);
        baguette.transform.localScale = new Vector3(0.025f, 0.025f, Vector3.Distance(de, a));
    }

    // Un nuage de fumée et un "pouf !" à cet endroit (comme TransitionDecoupePeinture pour les bombes).
    private void Pouf(Vector3 position)
    {
        if (sonPouf != null)
            AudioSource.PlayClipAtPoint(sonPouf, position, 0.6f);

        if (fumeePrefab == null)
            return;
        ParticleSystem fumee = Instantiate(fumeePrefab, position, Quaternion.identity);
        fumee.gameObject.SetActive(true); // au cas où le prefab est enregistré caché
        fumee.Play(true);
        Destroy(fumee.gameObject, 1.2f);
    }
}
