using UnityEngine;

// FONCTIONNALITÉ : à chaque étape de découpe finie, le morceau de tissu en trop (derrière les pointillés) tombe.
// Au départ, le tissu (un carré) est redécoupé en morceaux : la forme du cerf-volant au milieu,
// et autour, une "chute" par étape : la part du carré entre ces pointillés et le bord du tissu.
// C'est PatronPointilles qui prépare les chutes (Preparer) puis les fait tomber (FaireTomber).
// À mettre sur l'objet "Tissu", avec PatronPointilles.
public class ChutesTissu : MonoBehaviour
{
    [SerializeField] private int finesse = 40;            // nombre de petites tranches par chute (plus = bord plus lisse)
    [SerializeField] private float dureeAvantDisparition = 2.5f; // une chute tombée disparaît après 2,5 secondes
    [SerializeField, Range(2f, 30f)] private float arrondiCoins = 14f; // coins du tissu : 14 = à peine arrondis, plus petit = plus rond (2 = un rond), 30 = presque carré

    private GameObject[] chutes; // une chute par étape

    // "coins" = le contour de la forme ; "debutsEtapes" = le point du contour où commence chaque étape.
    // Tout est en coordonnées du tissu : le carré va de -0.5 à 0.5, le milieu est en (0, 0).
    public void Preparer(Vector2[] coins, Vector2[] debutsEtapes)
    {
        Material matiere = GetComponent<Renderer>().sharedMaterial;

        // 1) Le tissu lui-même ne garde que la forme du cerf-volant : le reste, ce sont les chutes.
        GetComponent<MeshFilter>().mesh = MaillageForme(coins);

        // 2) Dans quel sens tourne le contour ? (+1 = sens inverse des aiguilles d'une montre, -1 = sens des aiguilles)
        //    Les chutes doivent tourner dans le même sens que les étapes.
        float sens = Aire(coins) > 0f ? 1f : -1f;

        int nombre = debutsEtapes.Length;
        chutes = new GameObject[nombre];
        for (int e = 0; e < nombre; e++)
        {
            // La chute va de l'angle du début de cette étape à l'angle du début de l'étape suivante.
            float angleDebut = Mathf.Atan2(debutsEtapes[e].y, debutsEtapes[e].x);
            float angleFin = Mathf.Atan2(debutsEtapes[(e + 1) % nombre].y, debutsEtapes[(e + 1) % nombre].x);
            float ouverture = Mathf.Repeat((angleFin - angleDebut) * sens, Mathf.PI * 2f) * sens;
            if (nombre == 1)
                ouverture = Mathf.PI * 2f * sens; // une seule étape : la chute fait tout le tour

            GameObject chute = new GameObject("Chute");
            chute.transform.SetParent(transform, false); // rangée dans le tissu : elle bouge et tourne avec lui
            chute.AddComponent<MeshFilter>().mesh = MaillageChute(coins, angleDebut, ouverture);
            chute.AddComponent<MeshRenderer>().sharedMaterial = matiere;
            chutes[e] = chute;
        }
    }

    // Appelé par PatronPointilles quand l'étape "e" est découpée : sa chute se détache et tombe.
    public void FaireTomber(int e)
    {
        GameObject chute = chutes[e];
        if (chute == null)
            return;

        // Elle quitte le tissu ("true" = elle garde sa place et sa taille dans le monde) : il peut sauter sans elle.
        chute.transform.SetParent(null, true);

        // Une boîte très fine pour qu'elle se pose sur la table au lieu de passer à travers.
        BoxCollider boite = chute.AddComponent<BoxCollider>();
        boite.size = new Vector3(boite.size.x, boite.size.y, 0.005f);

        // La gravité la fait tomber ; on la pousse un peu vers l'extérieur et on la fait tournoyer, comme un vrai bout de tissu.
        Rigidbody corps = chute.AddComponent<Rigidbody>();
        Vector3 versLExterieur = Vector3.ProjectOnPlane(boite.bounds.center - transform.position, Vector3.up).normalized;
        corps.linearVelocity = versLExterieur * 0.4f + Vector3.up * 0.3f;
        corps.angularVelocity = Random.insideUnitSphere * 3f;

        Destroy(chute, dureeAvantDisparition);
        chutes[e] = null;
    }

    // La forme du cerf-volant : un éventail de triangles depuis le milieu (comme dans PieceDecoupee).
    private Mesh MaillageForme(Vector2[] coins)
    {
        Vector3[] sommets = new Vector3[coins.Length + 1];
        int[] triangles = new int[coins.Length * 3];
        sommets[0] = Vector3.zero;
        for (int i = 0; i < coins.Length; i++)
        {
            sommets[i + 1] = coins[i];
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % coins.Length + 1;
        }
        return Construire("Tissu_Forme", sommets, triangles);
    }

    // Une chute : une bande de petites tranches, chacune allant du contour de la forme jusqu'au bord du carré.
    private Mesh MaillageChute(Vector2[] coins, float angleDebut, float ouverture)
    {
        Vector3[] sommets = new Vector3[(finesse + 1) * 2];
        int[] triangles = new int[finesse * 6];
        for (int i = 0; i <= finesse; i++)
        {
            float angle = angleDebut + ouverture * i / finesse;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            // Point intérieur : sur le contour de la forme. Point extérieur : sur le bord du carré aux coins arrondis.
            sommets[i * 2] = direction * DistanceContour(coins, direction);
            sommets[i * 2 + 1] = direction * DistanceBord(direction);

            if (i < finesse)
            {
                // Chaque tranche est un petit quadrilatère, coupé en deux triangles.
                int a = i * 2;
                triangles[i * 6] = a;
                triangles[i * 6 + 1] = a + 1;
                triangles[i * 6 + 2] = a + 3;
                triangles[i * 6 + 3] = a;
                triangles[i * 6 + 4] = a + 3;
                triangles[i * 6 + 5] = a + 2;
            }
        }
        return Construire("Tissu_Chute", sommets, triangles);
    }

    // À quelle distance du milieu le contour est-il, dans cette direction ? (on cherche le côté que la direction traverse)
    private float DistanceContour(Vector2[] coins, Vector2 direction)
    {
        float plusProche = 0.5f; // au cas où (ne devrait pas arriver) : le bord du tissu
        for (int i = 0; i < coins.Length; i++)
        {
            Vector2 a = coins[i];
            Vector2 cote = coins[(i + 1) % coins.Length] - a;
            float diviseur = Croix(direction, cote);
            if (Mathf.Abs(diviseur) < 1e-6f)
                continue; // la direction est parallèle à ce côté : elle ne le traverse pas

            float distance = Croix(a, cote) / diviseur;  // jusqu'où aller dans la direction
            float position = Croix(a, direction) / diviseur; // où on tombe sur le côté (de 0 = a, à 1 = coin suivant)
            if (distance > 0f && position >= 0f && position <= 1f && distance < plusProche)
                plusProche = distance;
        }
        return plusProche;
    }

    // À quelle distance du milieu est le bord du tissu, dans cette direction ?
    // Le bord est un "carré arrondi" : |x|^n + |y|^n = 0,5^n (n = arrondiCoins). Sur les côtés c'est presque droit, dans les coins c'est rond.
    private float DistanceBord(Vector2 direction)
    {
        float n = arrondiCoins;
        return 0.5f / Mathf.Pow(Mathf.Pow(Mathf.Abs(direction.x), n) + Mathf.Pow(Mathf.Abs(direction.y), n), 1f / n);
    }

    private static float Croix(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;

    // L'aire "signée" du contour : positive s'il tourne dans le sens inverse des aiguilles d'une montre.
    private static float Aire(Vector2[] coins)
    {
        float somme = 0f;
        for (int i = 0; i < coins.Length; i++)
            somme += Croix(coins[i], coins[(i + 1) % coins.Length]);
        return somme / 2f;
    }

    // Fabrique le maillage. Les "uv" (où chaque point tombe sur l'image du tissu) suivent ceux du carré d'origine.
    private Mesh Construire(string nom, Vector3[] sommets, int[] triangles)
    {
        Vector2[] uv = new Vector2[sommets.Length];
        for (int i = 0; i < sommets.Length; i++)
            uv[i] = new Vector2(sommets[i].x + 0.5f, sommets[i].y + 0.5f);

        Mesh maillage = new Mesh();
        maillage.name = nom;
        maillage.vertices = sommets;
        maillage.uv = uv;
        maillage.triangles = triangles;
        maillage.RecalculateNormals();
        maillage.RecalculateBounds();
        return maillage;
    }
}
