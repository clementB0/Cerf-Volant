using UnityEngine;

// DONNÉE : une forme de cerf-volant à découper (le contour, en points).
// Pour en créer une nouvelle : clic droit dans le Project > Create > Cerf-Volant > Forme, puis remplir "Coins".
// Ensuite, glisser le fichier dans le champ "Forme" du PatronPointilles (sur le Tissu).
[CreateAssetMenu(menuName = "Cerf-Volant/Forme", fileName = "Forme")]
public class FormeCerfVolant : ScriptableObject
{
    // Les points du contour, en mètres, depuis le centre du tissu.
    // x = gauche (-) / droite (+), y = vers soi (-) / loin de soi (+). Le tissu fait 1 m : rester entre -0.5 et 0.5.
    // Attention :
    //  - donner les points dans le sens des aiguilles d'une montre (vu du dessus), sinon la PieceDecoupee est invisible ;
    //  - depuis le centre (0, 0), on doit "voir" tout le contour (voir PieceDecoupee).
    [SerializeField] private Vector2[] coins;

    // La largeur du corps (au milieu), en part de la demi-envergure : 0.1 = le corps fait 10 % de chaque côté du milieu.
    // Au-delà, ce sont les ailes : c'est là qu'elles pivotent quand le cerf-volant bat des ailes (BattementAiles).
    [SerializeField, Range(0f, 0.9f)] private float corps = 0.1f;

    // En lecture seule pour les autres scripts.
    public Vector2[] Coins => coins;
    public float Corps => corps;

    // Fabrique la forme en 3D, toute plate (utilisée par la PieceDecoupee et par les miniatures du menu).
    public Mesh CreerMaillage()
    {
        // Les sommets : le centre (n° 0), puis les coins de la forme (n° 1, 2, 3...), à plat (z = 0).
        Vector3[] sommets = new Vector3[coins.Length + 1];
        sommets[0] = Vector3.zero;
        for (int i = 0; i < coins.Length; i++)
            sommets[i + 1] = new Vector3(coins[i].x, coins[i].y, 0f);

        // Les triangles, en "éventail" depuis le centre : (centre, coin 1, coin 2), (centre, coin 2, coin 3)...
        // Le "%" fait revenir le dernier coin au premier pour fermer la forme.
        // shortcut: marche seulement si on "voit" tout le contour depuis le centre (papillon, raie, dragon) ; une forme en U ou en C demandera une vraie découpe en triangles.
        int[] triangles = new int[coins.Length * 3];
        for (int i = 0; i < coins.Length; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % coins.Length + 1;
        }

        Mesh maillage = new Mesh();
        maillage.name = name; // le nom du fichier : "Papillon", "Raie"...
        maillage.vertices = sommets;
        maillage.triangles = triangles;
        maillage.RecalculateNormals(); // pour que la lumière sache de quel côté est le dessus
        return maillage;
    }
}
