using UnityEngine;

public class PointAccroche : MonoBehaviour
{
    [SerializeField] private Transform[] boutsDesCordes; // Gauche, puis droite.
    [SerializeField] private float rayon = 0.03f;
    [SerializeField] private Color couleur = new Color(1f, 0.85f, 0.2f, 0.8f);

    private GameObject[] points;
    private Material matiere;

    // Les bouts des cordes (gauche, puis droite) : lus par IndicationsCordes pour placer "Gauche" et "Droite".
    internal Transform[] BoutsDesCordes => boutsDesCordes;

    private MeshFilter forme; // la forme du cerf-volant (pour trouver son centre)

    // Les deux ronds, symétriques autour du centre du cerf-volant (pas sur les ailes), assez écartés pour ne pas se toucher.
    // Le centre = le milieu de la forme, pas forcément son point (0, 0) : la raie, par exemple, est plus large d'un côté.
    // (Aussi utilisé par AideEmboitement et IndicationsCordes.)
    internal Vector3 PositionLocale(int indice)
    {
        Vector3 centre = forme != null && forme.sharedMesh != null ? forme.sharedMesh.bounds.center : Vector3.zero;
        return new Vector3(centre.x + (indice == 0 ? -0.07f : 0.07f), centre.y, 0f);
    }

    private void Awake()
    {
        forme = GetComponent<MeshFilter>();
    }

    private void Start()
    {
        matiere = new Material(Shader.Find("Sprites/Default")) { color = couleur };
        points = new GameObject[boutsDesCordes.Length];
        for (int i = 0; i < points.Length; i++)
        {
            GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            point.SetActive(false);
            point.name = i == 0 ? "Accroche gauche" : "Accroche droite";
            point.transform.SetParent(transform, false);
            point.GetComponent<Collider>().isTrigger = true;
            point.GetComponent<Renderer>().sharedMaterial = matiere;
            point.AddComponent<CerfVolant>().CordeAttendue = boutsDesCordes[i];
            points[i] = point;
        }
    }

    private void LateUpdate()
    {
        Vector3 echelle = transform.lossyScale;
        for (int i = 0; i < points.Length; i++)
        {
            bool disponible = boutsDesCordes[i] != null && boutsDesCordes[i].gameObject.activeInHierarchy;
            points[i].SetActive(disponible);
            if (!disponible)
                continue;
            points[i].transform.localPosition = PositionLocale(i); // à chaque image : la forme peut être construite après nous
            points[i].transform.localScale = new Vector3(2f * rayon / echelle.x, 2f * rayon / echelle.y, 2f * rayon / echelle.z);
        }
    }

    private void OnDestroy()
    {
        if (matiere != null)
            Destroy(matiere);
    }
}
