using UnityEngine;

// FONCTIONNALITÉ : un livre ouvert posé sur la table, où arrivent les photos prises avec l'appareil (avec un "pouf").
// 4 places (2 par page) ; à la 5e photo, elle remplace la plus ancienne.
// À mettre sur un objet vide posé sur la table, à côté de l'appareil photo (le joueur est côté -z du livre).
public class LivrePhotos : MonoBehaviour
{
    [SerializeField] private ParticleSystem fumeePrefab; // le nuage de fumée (VFX/APPARITION)
    [SerializeField] private AudioClip sonPouf;          // le "pouf !" (SonFumee.wav)

    // Les 4 places des photos sur les pages (gauche haut, gauche bas, droite haut, droite bas), au-dessus du papier.
    private readonly Vector3[] places =
    {
        new Vector3(-0.083f, 0.018f, 0.05f), new Vector3(-0.083f, 0.018f, -0.05f),
        new Vector3(0.083f, 0.018f, 0.05f), new Vector3(0.083f, 0.018f, -0.05f),
    };
    private GameObject[] photos = new GameObject[4];
    private int nombre; // combien de photos sont déjà arrivées
    private Material papier;

    private void Start()
    {
        // Le livre ouvert, en formes simples : la couverture, deux pages crème, et le pli au milieu.
        Bloc(new Vector3(0.36f, 0.012f, 0.25f), new Vector3(0f, 0.006f, 0f), new Color(0.72f, 0.23f, 0.23f), true);
        Bloc(new Vector3(0.165f, 0.01f, 0.23f), new Vector3(-0.087f, 0.016f, 0f), new Color(1f, 0.97f, 0.9f), false);
        Bloc(new Vector3(0.165f, 0.01f, 0.23f), new Vector3(0.087f, 0.016f, 0f), new Color(1f, 0.97f, 0.9f), false);
        Bloc(new Vector3(0.004f, 0.011f, 0.23f), new Vector3(0f, 0.017f, 0f), new Color(0.5f, 0.45f, 0.4f), false);
        papier = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        papier.color = Color.white;
    }

    // Appelé par AppareilPhoto : la photo apparaît dans le livre avec un "pouf".
    public void AjouterPhoto(Texture2D image)
    {
        int place = nombre % places.Length;
        nombre++;
        if (photos[place] != null)
            Destroy(photos[place]); // le livre est plein : on remplace la plus ancienne

        // Un cadre blanc (comme un polaroid) avec la photo dessus, couché sur la page, un peu de travers.
        GameObject cadre = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(cadre.GetComponent<Collider>());
        cadre.transform.SetParent(transform, false);
        cadre.transform.localPosition = places[place];
        // Couché face au ciel (un Quad se voit du côté -z : on le tourne de 90 degrés), le haut de la photo loin du joueur.
        cadre.transform.localRotation = Quaternion.Euler(90f, Random.Range(-8f, 8f), 0f);
        cadre.transform.localScale = new Vector3(0.075f, 0.085f, 1f);
        cadre.GetComponent<Renderer>().sharedMaterial = papier;

        GameObject dessin = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(dessin.GetComponent<Collider>());
        dessin.transform.SetParent(cadre.transform, false);
        dessin.transform.localPosition = new Vector3(0f, 0.07f, -0.002f); // un peu plus haut (le bas du cadre est plus large) et juste au-dessus du blanc
        dessin.transform.localScale = new Vector3(0.85f, 0.72f, 1f);
        Material matiere = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        matiere.mainTexture = image;
        dessin.GetComponent<Renderer>().material = matiere;

        photos[place] = cadre;
        Pouf(cadre.transform.position);
    }

    // Crée un bloc (un cube) du livre, d'une couleur plate.
    private void Bloc(Vector3 taille, Vector3 place, Color couleur, bool garderCollider)
    {
        GameObject bloc = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bloc.transform.SetParent(transform, false);
        bloc.transform.localPosition = place;
        bloc.transform.localScale = taille;
        if (!garderCollider)
            Destroy(bloc.GetComponent<Collider>()); // seule la couverture a un collider (l'appareil peut se poser dessus)
        Material matiere = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        matiere.color = couleur;
        bloc.GetComponent<Renderer>().material = matiere;
    }

    private void Pouf(Vector3 position)
    {
        if (sonPouf != null)
            AudioSource.PlayClipAtPoint(sonPouf, position, 0.6f);
        if (fumeePrefab == null)
            return;
        ParticleSystem fumee = Instantiate(fumeePrefab, position, Quaternion.identity);
        fumee.gameObject.SetActive(true);
        fumee.Play(true);
        Destroy(fumee.gameObject, 2.5f); // l'effet APPARITION dure 2,5 s
    }
}
