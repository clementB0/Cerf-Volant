using UnityEngine;

// FONCTIONNALITÉ : au début, le vrai cerf-volant en 3D (ex. la raie) arrive du ciel en battant des ailes,
// puis se pose à la place de sa miniature, à côté des autres choix. Il remplace alors la miniature plate.
// Les ailes battent comme une vague qui part du corps vers le bout des ailes ; le battement ralentit avant de se poser.
// À mettre sur une miniature du menu (ex. "Mini Raie"), avec MiniatureCerfVolant :
// on peut toujours la choisir comme avant (la main touche la zone de la miniature).
public class ArriveeEnVol : MonoBehaviour
{
    [SerializeField] private GameObject modele;              // le modèle 3D (ex. cerf_volant_3)
    [SerializeField] private FormeCerfVolant forme;          // sa forme plate (pour lui donner la même taille que la miniature)
    [SerializeField] private float duree = 5f;               // le vol dure 5 secondes
    [SerializeField] private Vector3 depart = new Vector3(-2.5f, 2.5f, 4.5f); // d'où il part, par rapport à sa place (en mètres)
    [SerializeField] private float battementsParSeconde = 2f;
    [SerializeField] private float amplitude = 0.35f;        // de combien le bout des ailes monte et descend (0.35 = 35 % de la demi-envergure)

    private Transform pivot;          // porte le modèle : ses axes sont ceux de la miniature (x = envergure, y = tête, z = dessous)
    private MeshFilter filtre;        // le maillage du modèle, qu'on déforme pour battre des ailes
    private Vector3[] sommetsDeBase;  // le maillage au repos
    private Vector3[] sommets;        // le maillage déformé (recalculé à chaque image)
    private float[] envergure;        // pour chaque sommet : où il est sur l'aile (0 = corps, 1 = bout de l'aile)
    private Vector3 versLeHaut;       // la direction "monte" du battement, dans le repère du maillage
    private float demiEnvergure;      // la moitié de l'envergure, dans le repère du maillage (pour l'amplitude)
    private float temps;              // depuis combien de temps il vole

    private void Start()
    {
        if (modele == null || forme == null)
            return;

        // 1) Le pivot, rangé dans la miniature (ainsi, s'il est encore en vol quand on choisit, il disparaît avec elle).
        pivot = new GameObject(modele.name + " (en vol)").transform;
        pivot.SetParent(transform, false);
        pivot.localScale = Vector3.one / transform.lossyScale.x; // taille 1 dans le monde : on règle la vraie taille sur le modèle

        GameObject objet = Instantiate(modele, pivot);
        filtre = objet.GetComponentInChildren<MeshFilter>();
        Transform tMaillage = filtre.transform;
        sommetsDeBase = filtre.sharedMesh.vertices; // (le modèle doit être importé avec "Read/Write" coché)

        // 2) On trouve ses axes : l'envergure = la plus grande longueur, le dessous = la plus petite (son épaisseur),
        //    et la tête = l'axe du milieu, du côté où il y a le plus de matière (la queue fine part de l'autre côté).
        Vector3[] p = new Vector3[sommetsDeBase.Length];
        Bounds boite = new Bounds(pivot.InverseTransformPoint(tMaillage.TransformPoint(sommetsDeBase[0])), Vector3.zero);
        Vector3 moyenne = Vector3.zero;
        for (int i = 0; i < p.Length; i++)
        {
            p[i] = pivot.InverseTransformPoint(tMaillage.TransformPoint(sommetsDeBase[i]));
            boite.Encapsulate(p[i]);
            moyenne += p[i] / p.Length;
        }
        int[] axes = { 0, 1, 2 };
        System.Array.Sort(axes, (a, b) => boite.size[a].CompareTo(boite.size[b])); // de la plus petite à la plus grande
        Vector3 dessous = Vector3.zero, tete = Vector3.zero;
        dessous[axes[0]] = -1f; // le signe choisit quelle face du modèle regarde le ciel (+1 la mettait sur le dos)
        tete[axes[1]] = Mathf.Sign(moyenne[axes[1]] - boite.center[axes[1]]);
        if (tete[axes[1]] == 0f) tete[axes[1]] = 1f;

        // 3) On tourne le modèle pour que : tête → y du pivot, dessous → z du pivot (comme la miniature), centré, à la bonne taille.
        Quaternion redresser = Quaternion.Inverse(Quaternion.LookRotation(dessous, tete));
        float largeurVoulue = Largeur(forme) * transform.lossyScale.x;  // l'envergure de la miniature, en mètres
        float echelle = largeurVoulue / boite.size[axes[2]];
        objet.transform.localRotation = redresser * objet.transform.localRotation;
        objet.transform.localScale *= echelle;
        objet.transform.localPosition = -(redresser * boite.center) * echelle;

        // 4) Préparation du battement : une copie du maillage qu'on pourra déformer, visible des deux côtés.
        filtre.mesh = DeuxFaces(filtre.sharedMesh);
        int n = sommetsDeBase.Length;
        sommetsDeBase = filtre.mesh.vertices; // les sommets, puis leurs copies pour l'autre face
        sommets = (Vector3[])sommetsDeBase.Clone();
        envergure = new float[sommets.Length];
        Vector3 axeEnvergure = Vector3.zero; axeEnvergure[axes[2]] = 1f;
        for (int i = 0; i < sommets.Length; i++)
            envergure[i] = Mathf.Abs(Vector3.Dot(p[i % n] - boite.center, axeEnvergure)) / (boite.size[axes[2]] / 2f);
        // "monte" = l'opposé du dessous, exprimé dans le repère du maillage ; et la demi-envergure dans ce même repère.
        versLeHaut = tMaillage.InverseTransformVector(pivot.TransformVector(-dessous)).normalized;
        demiEnvergure = tMaillage.InverseTransformVector(pivot.TransformVector(axeEnvergure)).magnitude * boite.size[axes[2]] / 2f;

        // La miniature plate est cachée : c'est le modèle 3D qui la remplace (sa zone de toucher reste la même).
        GetComponent<Renderer>().enabled = false;
        Placer(0f);
    }

    private void Update()
    {
        if (pivot == null || temps > duree + 1f)
            return; // posé depuis un moment : il ne bouge plus
        temps += Time.deltaTime;
        Placer(Mathf.Clamp01(temps / duree));
    }

    // Le place à l'instant "t" du vol (0 = départ, 1 = posé), et fait battre ses ailes.
    private void Placer(float t)
    {
        // Le trajet : une courbe qui part du ciel et descend en douceur vers sa place (elle ralentit à la fin).
        float doux = 1f - (1f - t) * (1f - t);
        Vector3 arrivee = transform.position;
        Vector3 a = arrivee + depart, b = arrivee + new Vector3(depart.x * 0.2f, depart.y, depart.z * 0.3f), c = arrivee + Vector3.up * 0.6f;
        Vector3 ici = Courbe(a, b, c, arrivee, doux);
        Vector3 devant = Courbe(a, b, c, arrivee, Mathf.Min(1f, doux + 0.02f)) - ici;

        // Il vole à plat, la tête dans le sens du vol, en penchant un peu dans les virages ; à la fin, il prend la pose de la miniature.
        Quaternion enVol = transform.rotation;
        if (devant.sqrMagnitude > 1e-8f)
            enVol = Quaternion.LookRotation(Vector3.down, devant) * Quaternion.Euler(0f, 0f, Mathf.Sin(temps * 1.3f) * 12f);
        float pose = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.75f, 1f, t));
        pivot.SetPositionAndRotation(ici, Quaternion.Slerp(enVol, transform.rotation, pose));

        // Le battement : une vague du corps vers le bout des ailes, qui s'arrête en douceur avant de se poser.
        float force = amplitude * (1f - Mathf.InverseLerp(0.7f, 0.95f, t));
        for (int i = 0; i < sommets.Length; i++)
        {
            float bout = envergure[i] * envergure[i]; // le corps bouge à peine, le bout des ailes beaucoup
            float vague = Mathf.Sin(temps * battementsParSeconde * Mathf.PI * 2f - envergure[i] * 2f);
            sommets[i] = sommetsDeBase[i] + versLeHaut * (vague * bout * force * demiEnvergure);
        }
        filtre.mesh.vertices = sommets;
        filtre.mesh.RecalculateNormals();
    }

    // Une copie du maillage visible des deux côtés : on ajoute une copie de chaque sommet et chaque triangle à l'envers.
    // (Un maillage ne se voit que d'un côté ; avec les triangles à l'envers, l'autre côté se voit et est bien éclairé.)
    private static Mesh DeuxFaces(Mesh original)
    {
        Mesh copie = Instantiate(original);
        int n = original.vertexCount;
        Vector3[] v = original.vertices;
        Vector2[] uv = original.uv;
        copie.vertices = Doubler(v);
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
        T[] double_ = new T[tableau.Length * 2];
        tableau.CopyTo(double_, 0);
        tableau.CopyTo(double_, tableau.Length);
        return double_;
    }

    // Une courbe douce qui passe par a (départ) et d (arrivée), tirée vers b et c (courbe de Bézier).
    private static Vector3 Courbe(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float u = 1f - t;
        return u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d;
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
