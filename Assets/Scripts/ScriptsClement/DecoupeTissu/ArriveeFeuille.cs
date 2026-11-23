using UnityEngine;

// FONCTIONNALITÉ : au début, le vrai cerf-volant en 3D tombe du ciel comme une feuille :
// il descend doucement en se balançant de gauche à droite, en penchant, et ses ailes se plient et se déplient,
// puis il se pose à plat sur la table, à la place de sa miniature. Il remplace alors la miniature plate.
// À mettre sur une miniature du menu (ex. "Mini Raie"), avec MiniatureCerfVolant :
// on peut toujours la choisir comme avant (la main touche la zone de la miniature).
public class ArriveeFeuille : MonoBehaviour
{
    [SerializeField] private GameObject modele;               // le modèle 3D (ex. cerf_volant_3)
    [SerializeField] private FormeCerfVolant forme;           // sa forme plate (pour lui donner la même taille que la miniature)
    [SerializeField] private float delai = 0f;                // il attend ce temps avant de tomber (pour qu'ils ne tombent pas tous ensemble)
    [SerializeField] private float duree = 4f;                // la chute dure 4 secondes
    [SerializeField] private float hauteur = 2f;              // il part 2 m au-dessus de sa place
    [SerializeField] private float balancement = 0.35f;      // de combien il se balance de gauche à droite, en mètres
    [SerializeField] private float pliage = 0.4f;             // de combien ses ailes se plient (0.4 = le bout monte de 40 % de la demi-envergure)
    [SerializeField] private bool retourner = false;          // à cocher si ce modèle arrive sur le dos
    [SerializeField] private Color couleur = new Color(0.85f, 0.2f, 0.2f); // la couleur du modèle (le même rouge que le tissu)
    [SerializeField] private Texture2D dessin;                // si rempli : ce dessin (fond transparent) remplace la couleur

    private Transform pivot;          // porte le modèle : ses axes sont ceux de la miniature (x = envergure, y = tête, z = dessous)
    private MeshFilter filtre;        // le maillage du modèle, qu'on plie
    private Vector3[] sommetsDeBase;  // le maillage au repos
    private Vector3[] sommets;        // le maillage plié (recalculé à chaque image)
    private float[] envergure;        // pour chaque sommet : où il est sur l'aile (0 = corps, 1 = bout de l'aile)
    private Vector3 versLeHaut;       // la direction "monte" du pliage, dans le repère du maillage
    private float demiEnvergure;      // la moitié de l'envergure, dans le repère du maillage
    private float temps;              // depuis combien de temps il tombe (négatif = il attend encore)
    private Vector3 cote;             // la direction du balancement (au hasard, à plat)

    private void Start()
    {
        if (modele == null || forme == null)
            return;

        // 1) Le pivot, rangé dans la miniature (ainsi, s'il est encore en l'air quand on choisit, il disparaît avec elle).
        pivot = new GameObject(modele.name + " (feuille)").transform;
        pivot.SetParent(transform, false);
        pivot.localScale = Vector3.one / transform.lossyScale.x; // taille 1 dans le monde : on règle la vraie taille sur le modèle

        GameObject objet = Instantiate(modele, pivot);
        filtre = objet.GetComponentInChildren<MeshFilter>();

        // Sa couleur : on teinte ses matériaux (des copies : le modèle d'origine ne change pas).
        // Avec un dessin : un matériau simple qui gère la transparence (seul le dessin se voit, pas le reste du modèle).
        Material matiereDessin = null;
        if (dessin != null)
        {
            matiereDessin = new Material(Shader.Find("Sprites/Default"));
            matiereDessin.mainTexture = dessin;
        }
        foreach (Renderer rendu in objet.GetComponentsInChildren<Renderer>())
        {
            if (matiereDessin != null)
            {
                Material[] matieres = new Material[rendu.sharedMaterials.Length];
                for (int m = 0; m < matieres.Length; m++)
                    matieres[m] = matiereDessin;
                rendu.sharedMaterials = matieres;
            }
            else
                foreach (Material matiere in rendu.materials)
                    matiere.color = couleur;
        }
        Transform tMaillage = filtre.transform;
        Vector3[] origine = filtre.sharedMesh.vertices; // (le modèle doit être importé avec "Read/Write" coché)

        // 2) On trouve ses axes : l'envergure = la plus grande longueur, le dessous = la plus petite (son épaisseur),
        //    et l'axe tête-queue = celui du milieu.
        Vector3[] p = new Vector3[origine.Length];
        Bounds boite = new Bounds(pivot.InverseTransformPoint(tMaillage.TransformPoint(origine[0])), Vector3.zero);
        for (int i = 0; i < p.Length; i++)
        {
            p[i] = pivot.InverseTransformPoint(tMaillage.TransformPoint(origine[i]));
            boite.Encapsulate(p[i]);
        }
        int[] axes = { 0, 1, 2 };
        System.Array.Sort(axes, (a, b) => boite.size[a].CompareTo(boite.size[b])); // de la plus petite à la plus grande
        Vector3 dessous = Vector3.zero, tete = Vector3.zero;
        dessous[axes[0]] = retourner ? 1f : -1f; // le signe choisit quelle face du modèle regarde le ciel

        //    De quel côté est la tête ? On compare avec la forme plate (tirée de la silhouette de ce modèle, tête en haut) :
        //    le centre de la surface penche du même côté sur les deux. (Pour le papillon, c'est vers le bas : ses ailes du bas sont plus larges.)
        float decalageModele = CentreDeSurface(filtre.sharedMesh.triangles, p, axes[1]) - boite.center[axes[1]];
        tete[axes[1]] = Mathf.Sign(decalageModele) * Mathf.Sign(DecalageForme(forme));

        // 3) On tourne le modèle pour que : tête → y du pivot, dessous → z du pivot (comme la miniature), centré, à la bonne taille.
        Quaternion redresser = Quaternion.Inverse(Quaternion.LookRotation(dessous, tete));
        float echelle = Largeur(forme) * transform.lossyScale.x / boite.size[axes[2]]; // même envergure que la miniature
        objet.transform.localRotation = redresser * objet.transform.localRotation;
        objet.transform.localScale *= echelle;
        objet.transform.localPosition = -(redresser * boite.center) * echelle;

        // 4) Préparation du pliage : une copie du maillage qu'on pourra plier, visible des deux côtés.
        filtre.mesh = DeuxFaces(filtre.sharedMesh);
        int n = origine.Length;
        sommetsDeBase = filtre.mesh.vertices; // les sommets, puis leurs copies pour l'autre face
        sommets = (Vector3[])sommetsDeBase.Clone();
        envergure = new float[sommets.Length];
        Vector3 axeEnvergure = Vector3.zero; axeEnvergure[axes[2]] = 1f;
        for (int i = 0; i < sommets.Length; i++)
            envergure[i] = Mathf.Abs(Vector3.Dot(p[i % n] - boite.center, axeEnvergure)) / (boite.size[axes[2]] / 2f);
        // Le dessin est posé à plat sur le modèle, comme un calque : sa largeur = l'envergure, sa hauteur = de la queue à la tête.
        // (Le dessin est découpé pile au cadre du cerf-volant dessiné, qui a la même forme que le modèle.)
        if (dessin != null)
        {
            Vector2[] uvDessin = new Vector2[sommetsDeBase.Length];
            for (int i = 0; i < uvDessin.Length; i++)
            {
                Vector3 q = p[i % n];
                float u = (q[axes[2]] - boite.min[axes[2]]) / boite.size[axes[2]];
                float v = (q[axes[1]] - boite.min[axes[1]]) / boite.size[axes[1]];
                uvDessin[i] = new Vector2(u, tete[axes[1]] > 0f ? v : 1f - v); // le haut du dessin du côté de la tête
            }
            filtre.mesh.uv = uvDessin;
        }
        versLeHaut = tMaillage.InverseTransformVector(pivot.TransformVector(-dessous)).normalized;
        demiEnvergure = tMaillage.InverseTransformVector(pivot.TransformVector(axeEnvergure)).magnitude * boite.size[axes[2]] / 2f;

        // Le balancement se fait dans une direction au hasard (chaque feuille tombe un peu différemment).
        float angle = Random.Range(0f, 360f);
        cote = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad));

        // La miniature plate est cachée : c'est le modèle 3D qui la remplace (sa zone de toucher reste la même).
        GetComponent<Renderer>().enabled = false;
        temps = -delai;
        pivot.gameObject.SetActive(delai <= 0f); // il n'apparaît qu'au moment de tomber
        Placer(0f);
    }

    private void Update()
    {
        if (pivot == null || temps > duree + 1f)
            return; // posé depuis un moment : il ne bouge plus
        temps += Time.deltaTime;
        if (temps < 0f)
            return; // il attend encore son tour
        pivot.gameObject.SetActive(true);
        Placer(Mathf.Clamp01(temps / duree));
    }

    // Le place à l'instant "t" de la chute (0 = en haut, 1 = posé), et plie ses ailes.
    private void Placer(float t)
    {
        float reste = 1f - t;                   // ce qui reste de la chute : les mouvements s'éteignent en approchant de la table
        float vague = temps * Mathf.PI * 1.2f;  // le rythme du balancement (un aller-retour toutes les ~1,7 s)

        // La descente : douce, elle ralentit en arrivant. Et le balancement de gauche à droite, comme une feuille.
        float descente = 1f - Mathf.SmoothStep(0f, 1f, t);
        Vector3 ici = transform.position + Vector3.up * (hauteur * descente) + cote * (Mathf.Sin(vague) * balancement * reste);

        // Il penche du côté où il glisse (comme une feuille qui se balance), et tourne lentement sur lui-même ; à la fin, à plat.
        Vector3 axePenche = Vector3.Cross(Vector3.up, cote);
        Quaternion penche = Quaternion.AngleAxis(Mathf.Cos(vague) * 35f * reste, axePenche);
        Quaternion tourne = Quaternion.AngleAxis(Mathf.Sin(temps * 0.7f) * 40f * reste, Vector3.up);
        pivot.SetPositionAndRotation(ici, penche * tourne * transform.rotation);

        // Le pliage : les ailes se relèvent et se rabaissent doucement (le bout bouge plus que le corps), puis s'aplatissent.
        float pli = Mathf.Sin(temps * Mathf.PI * 0.9f) * pliage * reste;
        for (int i = 0; i < sommets.Length; i++)
            sommets[i] = sommetsDeBase[i] + versLeHaut * (envergure[i] * envergure[i] * pli * demiEnvergure);
        filtre.mesh.vertices = sommets;
        filtre.mesh.RecalculateNormals();
    }

    // Une copie du maillage visible des deux côtés : on ajoute une copie de chaque sommet et chaque triangle à l'envers.
    // (Un maillage ne se voit que d'un côté ; avec les triangles à l'envers, l'autre côté se voit et est bien éclairé.)
    private static Mesh DeuxFaces(Mesh original)
    {
        Mesh copie = Instantiate(original);
        int n = original.vertexCount;
        Vector2[] uv = original.uv;
        copie.vertices = Doubler(original.vertices);
        if (uv.Length == n)
            copie.uv = Doubler(uv);
        for (int s = 0; s < original.subMeshCount; s++)
        {
            int[] tri = original.GetTriangles(s);
            int[] deux = new int[tri.Length * 2];
            for (int i = 0; i < tri.Length; i += 3)
            {
                deux[i] = tri[i]; deux[i + 1] = tri[i + 1]; deux[i + 2] = tri[i + 2];       // la face d'origine
                int j = tri.Length + i;
                deux[j] = tri[i] + n; deux[j + 1] = tri[i + 2] + n; deux[j + 2] = tri[i + 1] + n; // la même, à l'envers (sur les copies)
            }
            copie.SetTriangles(deux, s);
        }
        copie.RecalculateNormals();
        if (uv.Length == n)
            copie.RecalculateTangents(); // pour que la texture soit bien éclairée sur les deux faces
        copie.RecalculateBounds();
        return copie;
    }

    // Un tableau suivi de sa copie (les sommets de l'autre face sont aux mêmes places).
    private static T[] Doubler<T>(T[] tableau)
    {
        T[] resultat = new T[tableau.Length * 2];
        tableau.CopyTo(resultat, 0);
        tableau.CopyTo(resultat, tableau.Length);
        return resultat;
    }

    // Le centre de la surface du modèle sur un axe : chaque triangle compte selon sa taille (pas selon son nombre de points).
    private static float CentreDeSurface(int[] triangles, Vector3[] p, int axe)
    {
        float surface = 0f, somme = 0f;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = p[triangles[i]], b = p[triangles[i + 1]], c = p[triangles[i + 2]];
            float aire = Vector3.Cross(b - a, c - a).magnitude / 2f;
            surface += aire;
            somme += aire * (a[axe] + b[axe] + c[axe]) / 3f;
        }
        return somme / surface;
    }

    // De combien le centre de la surface de la forme plate est au-dessus (+) ou au-dessous (-) du milieu de la forme.
    private static float DecalageForme(FormeCerfVolant f)
    {
        Vector2[] c = f.Coins;
        float aire = 0f, centreY = 0f, minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < c.Length; i++)
        {
            Vector2 a = c[i], b = c[(i + 1) % c.Length];
            float k = a.x * b.y - b.x * a.y; // (formule du centre d'un polygone)
            aire += k;
            centreY += (a.y + b.y) * k;
            minY = Mathf.Min(minY, a.y);
            maxY = Mathf.Max(maxY, a.y);
        }
        return centreY / (3f * aire) - (minY + maxY) / 2f;
    }

    // L'envergure de la forme plate (de son coin le plus à gauche à son coin le plus à droite).
    private static float Largeur(FormeCerfVolant f)
    {
        float min = float.MaxValue, max = float.MinValue;
        foreach (Vector2 coin in f.Coins)
        {
            min = Mathf.Min(min, coin.x);
            max = Mathf.Max(max, coin.x);
        }
        return max - min;
    }
}
