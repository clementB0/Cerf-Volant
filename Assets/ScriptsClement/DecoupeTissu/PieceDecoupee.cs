using UnityEngine;

// FONCTIONNALITÉ : la pièce de tissu découpée, qui a la forme du patron.
// À mettre sur l'objet "PieceDecoupee" (désactivé au départ).
// PatronPointilles l'active quand tous les traits sont reliés : à ce moment-là elle se construit
// et prend la place du Tissu.
// Pour l'attraper, on n'écrit rien : c'est le composant "XR Grab Interactable" du template (sur le même objet).
[RequireComponent(typeof(MeshFilter), typeof(BoxCollider))]
public class PieceDecoupee : MonoBehaviour
{
    [SerializeField] private PatronPointilles patron; // on lui prend la forme et la position

    private void Start()
    {
        // On se met exactement à la place du tissu, pour que la pièce apparaisse sous la ligne tracée.
        transform.SetPositionAndRotation(patron.transform.position, patron.transform.rotation);

        Vector2[] coins = patron.Coins;

        // Les sommets du maillage : le centre du tissu (n° 0), puis les coins de la forme (n° 1, 2, 3...), à plat (z = 0).
        Vector3[] sommets = new Vector3[coins.Length + 1];
        sommets[0] = Vector3.zero;
        for (int i = 0; i < coins.Length; i++)
            sommets[i + 1] = new Vector3(coins[i].x, coins[i].y, 0f);

        // Les triangles, en "éventail" depuis le centre : (centre, coin 1, coin 2), (centre, coin 2, coin 3)...
        // Le "%" fait revenir le dernier coin au premier pour fermer la forme.
        // shortcut: marche seulement si on "voit" tout le contour depuis le centre (losange, papillon) ; une forme en U ou en C demandera une vraie découpe en triangles.
        int[] triangles = new int[coins.Length * 3];
        for (int i = 0; i < coins.Length; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % coins.Length + 1;
        }

        Mesh maillage = new Mesh();
        maillage.name = "Forme cerf-volant";
        maillage.vertices = sommets;
        maillage.triangles = triangles;
        maillage.RecalculateNormals(); // pour que la lumière sache de quel côté est le dessus
        GetComponent<MeshFilter>().mesh = maillage;

        // La zone où la main peut attraper la pièce (utilisée par le XR Grab Interactable du template).
        // On la cale sur la taille de la forme, avec 1 cm d'épaisseur sinon elle est trop fine pour être attrapée.
        // shortcut: c'est un rectangle autour de la forme, donc on attrape aussi dans les coins vides ; passer à un MeshCollider si ça gêne.
        BoxCollider zoneDePrise = GetComponent<BoxCollider>();
        zoneDePrise.center = maillage.bounds.center;
        zoneDePrise.size = new Vector3(maillage.bounds.size.x, maillage.bounds.size.y, 0.01f);
    }
}
