using UnityEngine;

// FONCTIONNALITÉ : le tissu du cerf-volant se gonfle quand le vent rentre dedans, comme une voile de bateau.
// Plus le vent est fort (rafale, ou quand tu tires sur le fil), plus le tissu se creuse. Il frissonne aussi un peu.
// À mettre sur l'objet "PieceDecoupee", à côté de VolCerfVolant (c'est lui qui connaît le vent).
public class GonflementCerfVolant : MonoBehaviour
{
    [SerializeField] private float profondeur = 0.15f;          // creux maximum au milieu du tissu (0.15 = 15 % de sa largeur)
    [SerializeField] private float ventPourGonflerAFond = 8f;   // à cette vitesse de vent (m/s), le tissu est gonflé au maximum
    [SerializeField] private float frisson = 0.01f;             // taille des petites vagues qui courent sur le tissu

    // Le tissu d'origine n'a qu'un point au milieu : impossible de faire un creux arrondi.
    // On le redécoupe donc en anneaux, du milieu vers le bord (comme une cible).
    private const int Anneaux = 4;

    private VolCerfVolant vol;   // le script du vol : il nous donne le vent
    private Mesh maillage;       // le tissu redécoupé en anneaux (créé au premier vol)
    private Vector3[] plat;      // la position de chaque point quand le tissu est à plat
    private float[] creux;       // de combien chaque point peut s'enfoncer : 1 au milieu, 0 sur le bord
    private Vector3[] sommets;   // la position de chaque point en ce moment (gonflé)
    private float gonflement;    // de -1 à 1 : à quel point le tissu est gonflé, et de quel côté

    private void Start()
    {
        vol = GetComponent<VolCerfVolant>();
    }

    // LateUpdate passe après tous les Update : le vol a déjà calculé le vent de cette image.
    private void LateUpdate()
    {
        // Tant que le cerf-volant n'a jamais volé, on ne touche pas au tissu (il sert encore à la peinture).
        if (maillage == null)
        {
            if (!vol.EnVol)
                return;
            Redecouper();
        }

        // 1) À quel point il doit être gonflé : rien du tout au sol, sinon selon la force du vent qu'il sent.
        float cible = 0f;
        if (vol.EnVol)
        {
            // Le vent pousse le tissu du côté opposé à celui d'où il arrive ("z" = devant/derrière le tissu).
            float sens = Mathf.Sign(transform.InverseTransformDirection(vol.VentApparent).z);
            cible = sens * Mathf.Clamp01(vol.VentApparent.magnitude / ventPourGonflerAFond);
        }
        // Le tissu met un petit moment à se gonfler et à se dégonfler.
        gonflement = Mathf.Lerp(gonflement, cible, 5f * Time.deltaTime);

        // 2) On enfonce chaque point : beaucoup au milieu, pas du tout sur le bord.
        for (int i = 0; i < plat.Length; i++)
        {
            // Une petite vague qui court le long du tissu (elle dépend du temps et de la hauteur du point).
            float vague = Mathf.Sin(Time.time * 18f + plat[i].y * 25f) * frisson;

            sommets[i] = plat[i];
            sommets[i].z += creux[i] * gonflement * (profondeur + vague);
        }

        // 3) On donne les nouvelles positions au tissu.
        maillage.vertices = sommets;
        maillage.RecalculateNormals();
        maillage.RecalculateBounds();
    }

    // Fabrique le tissu en anneaux à partir du tissu d'origine (point 0 = le milieu, les autres = le contour).
    private void Redecouper()
    {
        MeshFilter filtre = GetComponent<MeshFilter>();
        Vector3[] origine = filtre.sharedMesh.vertices;
        Vector2[] uvOrigine = filtre.sharedMesh.uv; // "uv" = où chaque point tombe sur le dessin peint
        int contour = origine.Length - 1;           // nombre de points sur le bord

        plat = new Vector3[1 + contour * Anneaux];
        creux = new float[plat.Length];
        sommets = new Vector3[plat.Length];
        Vector2[] uv = new Vector2[plat.Length];

        // Le point du milieu : c'est lui qui s'enfonce le plus.
        plat[0] = origine[0];
        uv[0] = uvOrigine[0];
        creux[0] = 1f;

        // Les anneaux : l'anneau 1 est près du milieu, le dernier est le bord du tissu.
        for (int anneau = 1; anneau <= Anneaux; anneau++)
        {
            float t = anneau / (float)Anneaux; // de 0 (milieu) à 1 (bord)
            for (int i = 0; i < contour; i++)
            {
                int point = 1 + (anneau - 1) * contour + i;
                plat[point] = Vector3.Lerp(origine[0], origine[i + 1], t);
                uv[point] = Vector2.Lerp(uvOrigine[0], uvOrigine[i + 1], t);
                creux[point] = 1f - t * t; // forme arrondie : 1 au milieu, 0 sur le bord
            }
        }

        // Les triangles : un éventail du milieu vers l'anneau 1, puis des bandes entre deux anneaux qui se suivent.
        int[] triangles = new int[contour * 3 + contour * (Anneaux - 1) * 6];
        int n = 0;
        for (int i = 0; i < contour; i++)
        {
            int suivant = (i + 1) % contour; // le point d'après sur l'anneau (le dernier rejoint le premier)

            triangles[n++] = 0;
            triangles[n++] = 1 + i;
            triangles[n++] = 1 + suivant;

            for (int anneau = 1; anneau < Anneaux; anneau++)
            {
                int dedans = 1 + (anneau - 1) * contour; // premier point de l'anneau intérieur
                int dehors = dedans + contour;           // premier point de l'anneau extérieur

                // Chaque case de la bande est un carré, coupé en deux triangles.
                triangles[n++] = dedans + i;
                triangles[n++] = dehors + i;
                triangles[n++] = dehors + suivant;

                triangles[n++] = dedans + i;
                triangles[n++] = dehors + suivant;
                triangles[n++] = dedans + suivant;
            }
        }

        maillage = new Mesh();
        maillage.name = "CerfVolant_Gonfle";
        maillage.vertices = plat;
        maillage.uv = uv;
        maillage.triangles = triangles;

        // Le cerf-volant affiche maintenant ce tissu-là. Le dessin peint reste à la même place (mêmes "uv").
        filtre.sharedMesh = maillage;
    }

    private void OnDestroy()
    {
        // Nettoyage du tissu créé pendant la partie.
        if (maillage != null)
            Destroy(maillage);
    }
}
