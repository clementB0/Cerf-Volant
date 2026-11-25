using System.Collections;
using UnityEngine;

// FONCTIONNALITÉ : quand la découpe est finie, le cerf-volant fait "pouf" et réapparaît sur un petit chevalet
// posé sur la table (comme une petite toile de peintre), un peu incliné vers l'arrière, face au joueur.
// Une fois la peinture validée (bouton "Passer au montage"), le chevalet disparaît dans un pouf.
// À mettre sur l'objet "PieceDecoupee" : il apparaît pile à la fin de la découpe, donc son Start = "découpe finie".
public class PresentoirCerfVolant : MonoBehaviour
{
    [SerializeField] private ParticleSystem fumeePrefab;     // le nuage de fumée (VFX/APPARITION)
    [SerializeField] private AudioClip sonPouf;              // le "pouf !" (SonFumee.wav)
    [SerializeField] private Material materiauPresentoir;    // la couleur du chevalet (Table.mat : bois marron)
    [SerializeField] private float attente = 0.9f;           // on laisse d'abord la pièce faire son petit saut de victoire (ReussiteDecoupe)
    [SerializeField] private float inclinaison = 15f;        // la toile penche vers l'arrière de 15 degrés
    [SerializeField] private float hauteurRebord = 0.05f;    // le rebord qui porte la toile est à 5 cm au-dessus de la table
    [SerializeField] private float epaisseur = 0.015f;       // les baguettes de bois font 1,5 cm

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

        // 2) Le chevalet se pose sur la table, là où était le tissu (il est à plat sur la table : sa hauteur = celle de la table).
        Vector3 surTable = transform.position;

        // "devant" = du joueur vers le chevalet, à plat : la toile sera tournée vers le joueur.
        // shortcut: sans caméra principale, on suppose le joueur côté -z du monde ; en VR la caméra est la tête du joueur.
        Vector3 joueur = Camera.main != null ? Camera.main.transform.position : surTable + Vector3.back;
        Vector3 devant = Vector3.ProjectOnPlane(surTable - joueur, Vector3.up).normalized;
        if (devant == Vector3.zero)
            devant = Vector3.forward;
        Vector3 droite = Vector3.Cross(Vector3.up, devant); // la droite du joueur

        // La toile penchée : son "haut" part un peu vers l'arrière (loin du joueur).
        float angle = inclinaison * Mathf.Deg2Rad;
        Vector3 hautToile = Vector3.up * Mathf.Cos(angle) + devant * Mathf.Sin(angle);
        Vector3 dosToile = devant * Mathf.Cos(angle) - Vector3.up * Mathf.Sin(angle); // la direction vers laquelle regarde le dos

        // 3) Le cerf-volant : sa face peinte vers le joueur, sa tête vers le haut, son bas posé pile sur le rebord.
        Vector3 rebord = surTable + Vector3.up * hauteurRebord;
        Bounds forme = GetComponent<MeshFilter>().sharedMesh.bounds;   // la taille de la forme (avant l'échelle)
        float echelle = transform.lossyScale.y;                         // l'échelle réelle dans le monde
        float bas = -forme.min.y * echelle;                             // du milieu de la forme jusqu'à son bas
        float haut = forme.max.y * echelle;                             // du milieu jusqu'à sa tête
        float demiLargeur = forme.extents.x * echelle;
        Vector3 centre = rebord + hautToile * bas;
        transform.SetPositionAndRotation(centre, Quaternion.LookRotation(dosToile, hautToile));
        // Pile au milieu du chevalet : le milieu de la forme n'est pas toujours son point (0, 0) (ex. la raie, plus large d'un côté).
        transform.position -= transform.right * (forme.center.x * transform.lossyScale.x);

        // 4) Le chevalet autour, puis pouf là où il apparaît.
        GameObject presentoir = ConstruirePresentoir(surTable, rebord, centre + hautToile * haut, droite, devant, hautToile, dosToile, demiLargeur);
        Pouf(centre);

        // 5) La peinture est validée (bouton "Passer au montage") : le chevalet ne sert plus, il disparaît dans un pouf.
        PieceDecoupee piece = GetComponent<PieceDecoupee>();
        yield return new WaitUntil(() => piece == null || piece.PriseFinaleAutorisee);
        Pouf(surTable + hautToile * 0.1f);
        Destroy(presentoir);
    }

    // Un petit chevalet comme ceux des toiles miniatures : un rebord, deux pieds écartés devant,
    // un montant derrière la toile qui dépasse en haut, et un pied d'appui derrière.
    private GameObject ConstruirePresentoir(Vector3 surTable, Vector3 rebord, Vector3 sommetToile, Vector3 droite, Vector3 devant,
                                      Vector3 hautToile, Vector3 dosToile, float demiLargeur)
    {
        GameObject presentoir = new GameObject("Presentoir");
        float largeur = demiLargeur * 0.8f;    // le rebord est un peu moins large que la toile
        Vector3 derriere = dosToile * 0.012f;  // le bois est juste derrière la toile, pour ne pas passer devant

        // Le rebord, sous le bas de la toile.
        Baguette(presentoir, rebord - droite * largeur, rebord + droite * largeur);

        // Les deux pieds avant : des bouts du rebord jusqu'à la table, un peu écartés et un peu vers le joueur.
        Baguette(presentoir, rebord - droite * largeur * 0.8f, surTable - droite * (largeur * 0.8f + 0.02f) - devant * 0.02f);
        Baguette(presentoir, rebord + droite * largeur * 0.8f, surTable + droite * (largeur * 0.8f + 0.02f) - devant * 0.02f);

        // Le montant du milieu, derrière la toile, penché comme elle, qui dépasse un peu au-dessus.
        Vector3 piedMontant = surTable + derriere;
        Vector3 hautMontant = sommetToile + hautToile * 0.02f + derriere;
        Baguette(presentoir, piedMontant, hautMontant);

        // Le pied d'appui : du milieu du montant jusqu'à la table, loin derrière : c'est lui qui tient tout incliné.
        Baguette(presentoir, Vector3.Lerp(piedMontant, hautMontant, 0.6f), surTable + devant * 0.12f);
        return presentoir;
    }

    // Une baguette de bois (un cube très allongé) qui va d'un point à un autre.
    private void Baguette(GameObject parent, Vector3 de, Vector3 a)
    {
        GameObject baguette = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baguette.name = "Baguette";
        baguette.transform.SetParent(parent.transform, false);
        Destroy(baguette.GetComponent<Collider>()); // pas de collision : il ne doit pas bloquer le jet de peinture
        baguette.GetComponent<Renderer>().sharedMaterial = materiauPresentoir;

        // Au milieu des deux points, tournée vers le second, aussi longue que l'écart entre eux.
        baguette.transform.position = (de + a) / 2f;
        baguette.transform.rotation = Quaternion.LookRotation(a - de);
        baguette.transform.localScale = new Vector3(epaisseur, epaisseur, Vector3.Distance(de, a));
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
        Destroy(fumee.gameObject, 2.5f); // l'effet APPARITION dure 2,5 s
    }
}
