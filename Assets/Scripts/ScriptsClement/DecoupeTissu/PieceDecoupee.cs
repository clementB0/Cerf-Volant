using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Construit la pièce avant les scripts exécutés avec l'ordre par défaut.
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[RequireComponent(typeof(BoxCollider), typeof(MeshCollider))]
public class PieceDecoupee : MonoBehaviour
{
    [Header("Forme à récupérer")]
    [SerializeField]
    private PatronPointilles patron;

    [Header("Prise en main")]
    [SerializeField, Min(0.001f)]
    private float epaisseurPrise = 0.01f;

    private Mesh maillage;

    private MeshFilter filtre;
    private MeshCollider colliderPeinture;
    private BoxCollider colliderPrise;

    private XRGrabInteractable interaction;
    private Rigidbody corps;

    public bool EstConstruite => maillage != null;

    private void Awake()
    {
        Construire();
    }

    public void Construire()
    {
        // Évite de reconstruire la pièce si cette méthode est rappelée.
        if (EstConstruite)
            return;

        if (patron == null)
        {
            Debug.LogError(
                "PieceDecoupee : assigne le Tissu dans le champ Patron.",
                this
            );

            return;
        }

        // La forme vient du fichier Forme du patron (Papillon, Raie, Dragon...). Sans forme : erreur juste en dessous.
        Vector2[] coins = patron.Forme != null ? patron.Forme.Coins : null;

        if (coins == null || coins.Length < 3)
        {
            Debug.LogError(
                "PieceDecoupee : le patron doit posséder au moins trois coins.",
                this
            );

            return;
        }

        filtre = GetComponent<MeshFilter>();
        colliderPeinture = GetComponent<MeshCollider>();
        colliderPrise = GetComponent<BoxCollider>();

        interaction = GetComponent<XRGrabInteractable>();
        corps = GetComponent<Rigidbody>();

        // La pièce doit rester immobile pendant la peinture.
        // Le Rigidbody devient cinématique AVANT d'activer
        // le MeshCollider non convexe.
        if (interaction != null)
            interaction.enabled = false;

        if (corps != null)
            corps.isKinematic = true;

        colliderPrise.enabled = false;
        colliderPeinture.enabled = false;

        // Même parent et même transformation que le tissu.
        // La pièce reste indépendante : masquer Tissu ne la masque pas.
        transform.SetParent(patron.transform.parent, false);

        transform.localPosition = patron.transform.localPosition;
        transform.localRotation = patron.transform.localRotation;
        transform.localScale = patron.transform.localScale;

        int nombreSommets = coins.Length + 1;

        Vector3[] sommets = new Vector3[nombreSommets];
        Vector2[] uv = new Vector2[nombreSommets];

        // Sommet central.
        sommets[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);

        // Sommets du contour.
        for (int i = 0; i < coins.Length; i++)
        {
            sommets[i + 1] = new Vector3(
                coins[i].x,
                coins[i].y,
                0f
            );

            // Le tissu va de -0.5 à +0.5.
            // Une texture utilise des coordonnées de 0 à 1.
            uv[i + 1] = new Vector2(
                coins[i].x + 0.5f,
                coins[i].y + 0.5f
            );
        }

        // On conserve ici la construction en éventail de ton ami.
        int[] triangles = new int[coins.Length * 3];

        for (int i = 0; i < coins.Length; i++)
        {
            int index = i * 3;

            triangles[index] = 0;
            triangles[index + 1] = i + 1;
            triangles[index + 2] =
                (i + 1) % coins.Length + 1;
        }

        maillage = new Mesh();
        maillage.name = "CerfVolant_Decoupe";

        maillage.vertices = sommets;
        maillage.uv = uv;
        maillage.triangles = triangles;

        maillage.RecalculateNormals();
        maillage.RecalculateBounds();

        filtre.sharedMesh = maillage;

        // Collider exact utilisé par les rayons de peinture.
        colliderPeinture.convex = false;
        colliderPeinture.isTrigger = false;
        colliderPeinture.sharedMesh = maillage;
        colliderPeinture.enabled = true;

        // Collider simplifié réservé à la prise finale.
        colliderPrise.center = maillage.bounds.center;

        colliderPrise.size = new Vector3(
            maillage.bounds.size.x,
            maillage.bounds.size.y,
            epaisseurPrise
        );

        colliderPrise.enabled = false;
    }

    // Le futur contrôleur appellera cette méthode
    // une fois la peinture terminée.
    public void AutoriserPriseFinale()
    {
        if (!EstConstruite)
            return;

        // Le collider de peinture ne sert plus.
        colliderPeinture.enabled = false;

        // Le collider de prise redevient actif.
        colliderPrise.enabled = true;

        if (interaction != null)
        {
            // Le XR Grab utilise uniquement le collider de prise.
            interaction.colliders.Clear();
            interaction.colliders.Add(colliderPrise);

            interaction.enabled = true;
        }
    }

    private void OnDestroy()
    {
        // Nettoyage du maillage créé pendant la partie.
        if (maillage != null)
            Destroy(maillage);
    }
}