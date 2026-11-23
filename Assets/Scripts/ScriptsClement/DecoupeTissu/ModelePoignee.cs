using UnityEngine;

// FONCTIONNALITÉ : le bâton qu'on tient pour faire voler le cerf-volant prend l'apparence du modèle 3D de poignée (SM_Poignee).
// Au lancement : le cylindre noir est caché, le modèle prend sa place (posé au même endroit sur la table),
// la zone qu'on attrape prend la taille du modèle, et le fil part du bout de la poignée.
// À mettre sur "PoigneeGauche" et "PoigneeDroite" (les bâtons). Le modèle n'apparaît qu'en jeu (pas dans la vue Scene).
public class ModelePoignee : MonoBehaviour
{
    [SerializeField] private GameObject modele;          // le modèle 3D (SM_Poignee)
    [SerializeField] private float hauteur = 0.2f;       // la vraie hauteur de la poignée, en mètres
    [SerializeField] private bool noeudEnHaut = true;    // le petit nœud de la corde en haut (vers le cerf-volant) ; décocher pour le mettre en bas

    private void Awake()
    {
        if (modele == null)
            return;

        // 1) Le cylindre ne se voit plus.
        GetComponent<MeshRenderer>().enabled = false;

        // 2) Un support, rangé dans le bâton, qui annule sa taille : dedans, 1 = 1 vrai mètre.
        Vector3 echelle = transform.lossyScale;
        Transform support = new GameObject("Poignee (modele)").transform;
        support.SetParent(transform, false);
        support.localScale = new Vector3(1f / echelle.x, 1f / echelle.y, 1f / echelle.z);

        Transform poignee = Instantiate(modele, support).transform;
        poignee.localPosition = Vector3.zero; // au centre du support (pour que l'agrandir ne la décale pas)
        if (noeudEnHaut)
            poignee.localRotation = Quaternion.Euler(180f, 0f, 0f) * poignee.localRotation; // retournée : le nœud en haut

        // 3) À la bonne hauteur.
        Bounds boite = Boite(poignee, support);
        float agrandir = hauteur / boite.size.y;
        poignee.localScale *= agrandir;
        boite = new Bounds(boite.center * agrandir, boite.size * agrandir); // (la poignée est au centre du support : tout grandit pareil)

        // 4) Posée là où était le bas du cylindre (le cylindre va de -1 à 1 en hauteur, dans le bâton).
        float bas = -echelle.y;
        poignee.localPosition = new Vector3(-boite.center.x, bas - boite.min.y, -boite.center.z);

        // 5) La zone qu'on attrape (et qui tient sur la table) = la taille du modèle.
        BoxCollider zone = GetComponent<BoxCollider>();
        if (zone != null)
        {
            zone.size = new Vector3(boite.size.x / echelle.x, boite.size.y / echelle.y, boite.size.z / echelle.z);
            zone.center = new Vector3(0f, -1f + boite.size.y / 2f / echelle.y, 0f);
        }

        // 6) Le fil part du haut de la poignée (l'objet "Bout").
        Transform bout = transform.Find("Bout");
        if (bout != null)
            bout.localPosition = new Vector3(0f, -1f + boite.size.y / echelle.y, 0f);
    }

    // La boîte autour du modèle, mesurée dans le repère du support.
    private static Bounds Boite(Transform objet, Transform repere)
    {
        bool premier = true;
        Bounds boite = new Bounds();
        foreach (MeshFilter filtre in objet.GetComponentsInChildren<MeshFilter>())
        {
            Matrix4x4 versRepere = repere.worldToLocalMatrix * filtre.transform.localToWorldMatrix;
            Bounds b = filtre.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 coin = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = versRepere.MultiplyPoint3x4(coin);
                if (premier) { boite = new Bounds(p, Vector3.zero); premier = false; }
                else boite.Encapsulate(p);
            }
        }
        return boite;
    }
}
