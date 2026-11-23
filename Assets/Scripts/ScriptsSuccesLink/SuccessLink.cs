using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SuccessLink : MonoBehaviour
{
    [SerializeField] private AudioClip sonReussite;

    private const int NombreEtoiles = 8;
    private const float Duree = 2.4f;

    private Transform groupe;
    private Transform[] etoiles;
    private Mesh meshEtoile;
    private Material materiauEtoile;
    private Renderer toile;
    private float temps;

    internal void Lancer()
    {
        if (sonReussite != null)
            AudioSource.PlayClipAtPoint(sonReussite, transform.position, 1f);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            return;

        toile = GetComponent<Renderer>();
        meshEtoile = GenererMeshEtoile();
        materiauEtoile = new Material(shader) { color = new Color(1f, 0.8f, 0.1f) };

        // Les étoiles suivent la position du cerf-volant et restent orientées dans le monde.
        var objet = new GameObject("Étoiles d’assemblage");
        SceneManager.MoveGameObjectToScene(objet, gameObject.scene);
        groupe = objet.transform;
        temps = 0f;
        etoiles = new Transform[NombreEtoiles];
        for (int i = 0; i < etoiles.Length; i++)
        {
            var etoile = new GameObject("Étoile");
            etoiles[i] = etoile.transform;
            etoiles[i].SetParent(groupe, false);
            etoiles[i].localScale = Vector3.zero;

            etoile.AddComponent<MeshFilter>().sharedMesh = meshEtoile;
            var rendu = etoile.AddComponent<MeshRenderer>();
            rendu.sharedMaterial = materiauEtoile;
            rendu.shadowCastingMode = ShadowCastingMode.Off;
            rendu.receiveShadows = false;
        }
    }

    private void LateUpdate()
    {
        if (groupe == null)
            return;

        temps += Time.deltaTime;
        if (temps >= Duree)
        {
            Nettoyer();
            return;
        }

        Bounds contour = toile != null ? toile.bounds : new Bounds(transform.position, Vector3.one * 0.4f);
        groupe.position = contour.center;
        Camera regard = Camera.main;
        if (regard != null)
            groupe.rotation = regard.transform.rotation;
        float rayon = contour.extents.magnitude + 0.075f;
        for (int i = 0; i < etoiles.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / etoiles.Length;
            float saut = Mathf.Sin(temps * 5f + i * 1.3f) * 0.017f;
            float apparition = Mathf.SmoothStep(0f, 1f, (temps - i * 0.045f) / 0.3f);
            float disparition = Mathf.SmoothStep(0f, 1f, (Duree - temps) / 0.5f);
            Transform etoile = etoiles[i];
            etoile.localPosition = new Vector3(Mathf.Cos(angle) * rayon, Mathf.Sin(angle) * rayon + saut, 0f);
            etoile.localRotation = Quaternion.Euler(0f, 0f, temps * 35f + i * 17f);
            etoile.localScale = Vector3.one * (0.11f * apparition * disparition);
        }
    }

    private void OnDisable()
    {
        Nettoyer();
    }

    private void Nettoyer()
    {
        if (groupe != null)
            Destroy(groupe.gameObject);
        if (meshEtoile != null)
            Destroy(meshEtoile);
        if (materiauEtoile != null)
            Destroy(materiauEtoile);

        groupe = null;
        meshEtoile = null;
        materiauEtoile = null;
        etoiles = null;
    }

    private static Mesh GenererMeshEtoile()
    {
        const int Points = 10;
        const float DemiEpaisseur = 0.07f;
        var sommets = new Vector3[2 + Points * 2];
        var triangles = new int[Points * 12];
        sommets[0] = Vector3.back * DemiEpaisseur;
        sommets[1] = Vector3.forward * DemiEpaisseur;

        for (int i = 0; i < Points; i++)
        {
            float angle = i * Mathf.PI / 5f + Mathf.PI / 2f;
            float rayon = i % 2 == 0 ? 0.5f : 0.22f;
            float x = Mathf.Cos(angle) * rayon;
            float y = Mathf.Sin(angle) * rayon;
            int avant = 2 + i * 2;
            int suivant = 2 + ((i + 1) % Points) * 2;
            sommets[avant] = new Vector3(x, y, -DemiEpaisseur);
            sommets[avant + 1] = new Vector3(x, y, DemiEpaisseur);

            int index = i * 12;
            // Faces avant, arrière, puis les deux triangles de la tranche.
            triangles[index] = 0;
            triangles[index + 1] = suivant;
            triangles[index + 2] = avant;
            triangles[index + 3] = 1;
            triangles[index + 4] = avant + 1;
            triangles[index + 5] = suivant + 1;
            triangles[index + 6] = avant;
            triangles[index + 7] = suivant;
            triangles[index + 8] = avant + 1;
            triangles[index + 9] = suivant;
            triangles[index + 10] = suivant + 1;
            triangles[index + 11] = avant + 1;
        }

        var mesh = new Mesh { name = "Étoile d’assemblage", vertices = sommets, triangles = triangles };
        mesh.RecalculateNormals();
        return mesh;
    }
}
