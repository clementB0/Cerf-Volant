
using System.Collections.Generic;
using UnityEngine;

public class Fusion : MonoBehaviour
{
    [Header("Fusion")]
    [SerializeField] private Transform newParent;

    [Header("Etoiles - Apparence")]
    [SerializeField] private bool afficherEtoiles = true;
    [SerializeField, Min(1)] private int nombreEtoiles = 5;
    [SerializeField] private Color couleurEtoiles = new Color(1f, 0.8f, 0.1f);
    [SerializeField, Min(0.01f)] private float tailleEtoiles = 0.12f;
    [SerializeField, Range(0.1f, 1f)] private float profondeur = 0.35f;

    [Header("Etoiles - Position")]
    [SerializeField] private float hauteur = 0.5f;
    [SerializeField] private float rayon = 0.25f;

    [Header("Etoiles - Animation")]
    [SerializeField] private float amplitudeSaut = 0.06f;
    [SerializeField] private float vitesseSaut = 3f;
    [SerializeField] private float vitesseRotation = 35f;
    [SerializeField] private float amplitudeOscillation = 0.025f;
    [SerializeField] private float vitesseApparition = 5f;

    private class Etoile
    {
        public Transform objet;
        public Vector3 positionInitiale;
        public Vector3 tailleInitiale;
        public float decalage;
    }

    private readonly List<Etoile> etoiles = new();

    private Mesh meshEtoile;
    private Material materiauEtoile;

    private float progressionApparition;
    private bool fusionEffectuee;

    // =========================================
    // INITIALISATION
    // =========================================

    private void Start()
    {
        if (afficherEtoiles)
        {
            CreerRessources();
            FaireApparaitreEtoiles();
        }
    }

    private void Update()
    {
        if (!afficherEtoiles || fusionEffectuee)
            return;

        progressionApparition = Mathf.MoveTowards(
            progressionApparition,
            1f,
            Time.deltaTime * vitesseApparition
        );

        float apparition = Mathf.SmoothStep(
            0f, 1f, progressionApparition
        );

        for (int i = 0; i < etoiles.Count; i++)
        {
            Etoile e = etoiles[i];

            if (e.objet == null)
                continue;

            float temps = Time.time * vitesseSaut + e.decalage;

            // Petit saut vertical
            float saut = Mathf.Sin(temps) * amplitudeSaut;

            // Mouvement horizontal léger
            float oscillation =
                Mathf.Cos(temps * 0.7f) * amplitudeOscillation;

            e.objet.localPosition =
                e.positionInitiale +
                new Vector3(oscillation, saut, 0f);

            // Rotation douce de l'étoile
            e.objet.Rotate(
                0f,
                0f,
                vitesseRotation * Time.deltaTime,
                Space.Self
            );

            // Apparition progressive
            e.objet.localScale =
                e.tailleInitiale * apparition;
        }
    }

    // =========================================
    // FUSION DU CERF-VOLANT
    // =========================================

    private void OnTriggerEnter(Collider other)
    {
        if (fusionEffectuee)
            return;

        if (other.gameObject.GetComponent<CerfVolant>())
        {
            fusionEffectuee = true;

            // Conserve le comportement initial
            Destroy(gameObject);

            if (newParent != null && newParent.gameObject != gameObject)
            {
                Destroy(newParent.gameObject);
            }
        }
    }

    // =========================================
    // CREATION DES RESSOURCES
    // =========================================

    private void CreerRessources()
    {
        meshEtoile = GenererMeshEtoile();

        Shader shader = Shader.Find(
            "Universal Render Pipeline/Lit"
        );

        if (shader == null)
            shader = Shader.Find("Standard");

        if (shader == null)
        {
            Debug.LogError(
                "[Fusion] Aucun shader compatible trouvé.",
                this
            );
            return;
        }

        materiauEtoile = new Material(shader);
        materiauEtoile.name = "Materiau Etoiles Fusion";

        materiauEtoile.color = couleurEtoiles;

        if (materiauEtoile.HasProperty("_BaseColor"))
        {
            materiauEtoile.SetColor(
                "_BaseColor",
                couleurEtoiles
            );
        }

        if (materiauEtoile.HasProperty("_EmissionColor"))
        {
            materiauEtoile.EnableKeyword("_EMISSION");

            materiauEtoile.SetColor(
                "_EmissionColor",
                couleurEtoiles * 0.35f
            );
        }
    }

    // =========================================
    // GENERATION DU MODELE 3D
    // =========================================

    private Mesh GenererMeshEtoile()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Etoile 3D Fusion";

        List<Vector3> sommets = new();
        List<int> triangles = new();

        float rayonExterne = 0.5f;
        float rayonInterne = 0.22f;
        float epaisseur = profondeur * 0.2f;

        // Centres avant et arrière
        sommets.Add(new Vector3(0f, 0f, -epaisseur));
        sommets.Add(new Vector3(0f, 0f, epaisseur));

        // Création des cinq branches
        for (int i = 0; i < 10; i++)
        {
            float angle =
                i * Mathf.PI / 5f + Mathf.PI / 2f;

            float r = i % 2 == 0
                ? rayonExterne
                : rayonInterne;

            float x = Mathf.Cos(angle) * r;
            float y = Mathf.Sin(angle) * r;

            sommets.Add(new Vector3(x, y, -epaisseur));
            sommets.Add(new Vector3(x, y, epaisseur));
        }

        for (int i = 0; i < 10; i++)
        {
            int suivant = (i + 1) % 10;

            int avant = 2 + i * 2;
            int arriere = avant + 1;

            int avantSuivant = 2 + suivant * 2;
            int arriereSuivant = avantSuivant + 1;

            // Face avant
            triangles.Add(0);
            triangles.Add(avantSuivant);
            triangles.Add(avant);

            // Face arrière
            triangles.Add(1);
            triangles.Add(arriere);
            triangles.Add(arriereSuivant);

            // Côtés
            triangles.Add(avant);
            triangles.Add(arriere);
            triangles.Add(avantSuivant);

            triangles.Add(avantSuivant);
            triangles.Add(arriere);
            triangles.Add(arriereSuivant);
        }

        mesh.SetVertices(sommets);
        mesh.SetTriangles(triangles, 0);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    // =========================================
    // APPARITION DES ETOILES
    // =========================================

    public void FaireApparaitreEtoiles()
    {
        if (meshEtoile == null || materiauEtoile == null)
            return;

        FaireDisparaitreEtoiles();

        afficherEtoiles = true;
        progressionApparition = 0f;

        for (int i = 0; i < nombreEtoiles; i++)
        {
            GameObject objet = new GameObject(
                "Etoile_" + i
            );

            objet.transform.SetParent(transform, false);

            MeshFilter filtre =
                objet.AddComponent<MeshFilter>();

            MeshRenderer rendu =
                objet.AddComponent<MeshRenderer>();

            filtre.sharedMesh = meshEtoile;
            rendu.sharedMaterial = materiauEtoile;

            float angle =
                i * Mathf.PI * 2f / nombreEtoiles;

            Vector3 position = new Vector3(
                Mathf.Cos(angle) * rayon,
                hauteur,
                Mathf.Sin(angle) * rayon
            );

            objet.transform.localPosition = position;
            objet.transform.localRotation = Quaternion.identity;
            objet.transform.localScale = Vector3.zero;

            Etoile nouvelleEtoile = new Etoile
            {
                objet = objet.transform,
                positionInitiale = position,
                tailleInitiale = Vector3.one * tailleEtoiles,
                decalage = i * 1.3f
            };

            etoiles.Add(nouvelleEtoile);
        }
    }

    // =========================================
    // DISPARITION DES ETOILES
    // =========================================

    public void FaireDisparaitreEtoiles()
    {
        foreach (Etoile e in etoiles)
        {
            if (e.objet != null)
                Destroy(e.objet.gameObject);
        }

        etoiles.Clear();
    }

    private void OnDestroy()
    {
        if (meshEtoile != null)
            Destroy(meshEtoile);

        if (materiauEtoile != null)
            Destroy(materiauEtoile);
    }
}
