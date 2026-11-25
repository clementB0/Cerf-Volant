using UnityEngine;

// FONCTIONNALITÉ : quand un pot de peinture est posé sur le pistolet mais pas encore clipsé,
// deux flèches courbes apparaissent de chaque côté du pot et se balancent d'un côté puis de l'autre : "tourne-le !".
// À mettre sur le pistolet, à côté de ReservoirSocket.
[RequireComponent(typeof(ReservoirSocket))]
public class FlechesClipsagePot : MonoBehaviour
{
    [SerializeField] private float rayon = 0.06f;          // rayon du cercle des flèches autour du pot
    [SerializeField] private float hauteur = 0f;           // décalage le long de l'axe du pot
    [SerializeField] private float epaisseur = 0.006f;     // épaisseur du trait des flèches
    [SerializeField] private Color couleur = Color.white;
    [SerializeField] private float balancement = 25f;      // de combien de degrés les flèches tournent d'un côté et de l'autre
    [SerializeField] private float vitesse = 4f;           // vitesse du balancement

    private ReservoirSocket support;
    private Transform pivot;

    private void Start()
    {
        support = GetComponent<ReservoirSocket>();
        pivot = new GameObject("Fleches clipsage").transform;
        pivot.SetParent(support.InsertionPoint, false);
        pivot.localPosition = Vector3.up * hauteur;

        Material materiau = new Material(Shader.Find("Sprites/Default"));
        CreerFleche(0f, materiau);
        CreerFleche(180f, materiau); // la deuxième, de l'autre côté du pot
        pivot.gameObject.SetActive(false);
    }

    private void Update()
    {
        bool afficher = support.PendingReservoir != null;
        pivot.gameObject.SetActive(afficher);
        if (afficher)
            pivot.localRotation = Quaternion.AngleAxis(Mathf.Sin(Time.time * vitesse) * balancement, Vector3.up);
    }

    // Un arc de cercle autour du pot, dont le bout s'élargit puis se referme en pointe (la tête de la flèche).
    private void CreerFleche(float angleDepart, Material materiau)
    {
        const int segments = 12;        // les 2 derniers font la pointe
        const float longueurArc = 120f; // en degrés

        LineRenderer ligne = new GameObject("Fleche").AddComponent<LineRenderer>();
        ligne.transform.SetParent(pivot, false);
        ligne.useWorldSpace = false;
        ligne.sharedMaterial = materiau;
        ligne.startColor = ligne.endColor = couleur;
        ligne.widthMultiplier = epaisseur;
        ligne.widthCurve = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.83f, 1f), new Keyframe(0.835f, 3.5f), new Keyframe(1f, 0f));
        ligne.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        ligne.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (angleDepart + longueurArc * i / segments) * Mathf.Deg2Rad;
            ligne.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * rayon);
        }
    }
}
