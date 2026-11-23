using UnityEngine;

public class PointAccroche : MonoBehaviour
{
    [SerializeField] private Transform[] boutsDesCordes; // Gauche, puis droite.
    [SerializeField] private float rayon = 0.03f;
    [SerializeField] private Color couleur = new Color(1f, 0.85f, 0.2f, 0.8f);

    private GameObject[] points;
    private Material matiere;

    internal static Vector3 PositionLocale(int indice)
    {
        return Vector3.right * (indice == 0 ? -0.16f : 0.16f);
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
            point.transform.localPosition = PositionLocale(i);
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
            if (disponible)
                points[i].transform.localScale = new Vector3(2f * rayon / echelle.x, 2f * rayon / echelle.y, 2f * rayon / echelle.z);
        }
    }

    private void OnDestroy()
    {
        if (matiere != null)
            Destroy(matiere);
    }
}
