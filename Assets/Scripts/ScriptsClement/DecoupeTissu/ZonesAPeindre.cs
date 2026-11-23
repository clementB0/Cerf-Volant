using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// FONCTIONNALITÉ : pendant qu'on tient une bombe de peinture, les zones du cerf-volant pas encore peintes s'illuminent
// (un voile lumineux qui pulse, seulement là où il n'y a pas encore de peinture). Il s'éteint à mesure qu'on peint.
// On lit la peinture déjà posée dans l'image de la pièce (celle du script de peinture) : là où elle est encore blanche, ce n'est pas peint.
// À mettre sur l'objet "PieceDecoupee". Il trouve tout seul les bombes de la scène.
public class ZonesAPeindre : MonoBehaviour
{
    [SerializeField] private Color couleur = Color.white;          // la couleur du voile
    [SerializeField, Range(0f, 1f)] private float opacite = 0.6f;  // le voile au plus fort (il pulse en dessous)
    [SerializeField] private int resolution = 128;                 // la finesse du voile, en pixels (plus = plus précis, plus lent)
    [SerializeField] private float rafraichissement = 0.25f;       // il se met à jour 4 fois par seconde

    private SprayInput[] bombes;
    private Renderer rendu;        // l'affichage de la pièce (son matériau contient l'image de la peinture)
    private VolCerfVolant vol;     // allumé à la fin de la peinture : on arrête alors
    private GameObject voile;      // une copie de la forme de la pièce, juste devant elle
    private Material matiereVoile;
    private Texture2D masque;      // blanc là où ce n'est pas peint, transparent ailleurs
    private Color32[] pixelsMasque;
    private float prochaineMiseAJour;

    private void Start()
    {
        bombes = FindObjectsByType<SprayInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        rendu = GetComponent<Renderer>();
        vol = GetComponent<VolCerfVolant>();

        masque = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        masque.wrapMode = TextureWrapMode.Clamp;
        pixelsMasque = new Color32[resolution * resolution];
        matiereVoile = new Material(Shader.Find("Sprites/Default")); // un matériau simple qui gère la transparence, visible des deux côtés
        matiereVoile.mainTexture = masque;

        // Le voile : la même forme que la pièce, rangée dedans, à peine devant sa face peinte (son côté -z).
        voile = new GameObject("ZonesAPeindre");
        voile.transform.SetParent(transform, false);
        voile.transform.localPosition = new Vector3(0f, 0f, -0.003f);
        voile.AddComponent<MeshFilter>().sharedMesh = GetComponent<MeshFilter>().sharedMesh;
        voile.AddComponent<MeshRenderer>().sharedMaterial = matiereVoile;
        voile.SetActive(false);
    }

    private void Update()
    {
        bool bombeEnMain = false;
        foreach (SprayInput bombe in bombes)
            if (bombe != null && bombe.isActiveAndEnabled && bombe.GetComponent<XRGrabInteractable>().isSelected)
                bombeEnMain = true;
        bool peintureFinie = vol != null && vol.enabled;

        bool montrer = bombeEnMain && !peintureFinie;
        voile.SetActive(montrer);
        if (!montrer)
            return;

        // Il pulse doucement, pour bien attirer l'œil.
        matiereVoile.color = new Color(couleur.r, couleur.g, couleur.b, opacite * (0.55f + 0.45f * Mathf.Sin(Time.time * 4f)));

        if (Time.time >= prochaineMiseAJour)
        {
            prochaineMiseAJour = Time.time + rafraichissement;
            MettreAJourMasque();
        }
    }

    // Regarde l'image de la peinture : chaque pixel encore blanc (pas peint) devient visible dans le voile.
    private void MettreAJourMasque()
    {
        Material matiere = rendu.sharedMaterial;
        Texture2D peinture = (matiere.HasProperty("_BaseMap") ? matiere.GetTexture("_BaseMap") : matiere.mainTexture) as Texture2D;
        if (peinture == null || !peinture.isReadable)
            return; // pas encore d'image de peinture

        Color32[] source = peinture.GetPixels32();
        int largeur = peinture.width, hauteur = peinture.height;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Color32 c = source[(y * hauteur / resolution) * largeur + x * largeur / resolution];
                bool pasPeint = c.r > 235 && c.g > 235 && c.b > 235; // encore blanc = le fond, pas de peinture
                pixelsMasque[y * resolution + x] = new Color32(255, 255, 255, pasPeint ? (byte)255 : (byte)0);
            }
        }
        masque.SetPixels32(pixelsMasque);
        masque.Apply();
    }
}
