using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// FONCTIONNALITÉ : une aide pour découper. Dès qu'on prend le cutter dans la main :
//  - un petit segment lumineux avance lentement le long des pointillés qui restent à découper ;
//  - un cutter "fantôme" (transparent) le suit, la lame posée sur le tissu, pour montrer le geste.
// Tout disparaît dès que le joueur découpe un trait, et revient après 5 secondes sans découper.
// À mettre sur l'objet "Tissu", avec PatronPointilles et TraceurCutter.
[RequireComponent(typeof(PatronPointilles), typeof(TraceurCutter))]
public class AideDecoupe : MonoBehaviour
{
    [SerializeField] private float attente = 5f;             // l'aide revient après 5 secondes sans découper
    [SerializeField] private float vitesse = 0.05f;          // le segment avance de 5 cm par seconde (lentement)
    [SerializeField] private float longueurSegment = 0.04f;  // il fait 4 cm de long
    [SerializeField] private Color couleurSegment = new Color(1f, 0.95f, 0.6f); // un jaune très clair, lumineux
    [SerializeField, Range(0f, 1f)] private float opaciteFantome = 0.35f;     // le fantôme est transparent

    private PatronPointilles patron;
    private Transform pointe;         // le bout de la lame du vrai cutter
    private Transform cutter;         // le vrai cutter (l'objet qu'on attrape)
    private XRGrabInteractable prise; // sait si on tient le cutter
    private Vector3 pointeDansLeCutter; // où est la pointe, vue depuis le cutter (pour placer le fantôme pareil)
    private GameObject segment;
    private GameObject fantome;
    private float avance;             // la distance déjà parcourue par le segment le long des pointillés

    private void Start()
    {
        patron = GetComponent<PatronPointilles>();
        pointe = GetComponent<TraceurCutter>().Pointe;
        prise = pointe.GetComponentInParent<XRGrabInteractable>();
        cutter = prise.transform;
        pointeDansLeCutter = cutter.InverseTransformPoint(pointe.position);

        // Le segment lumineux : un petit bâton allongé, couleur unie et vive.
        segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = "AideDecoupe_Segment";
        Destroy(segment.GetComponent<Collider>());
        Material lumiere = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        lumiere.color = couleurSegment;
        segment.GetComponent<Renderer>().material = lumiere;
        segment.SetActive(false);

        // Le fantôme : une copie des formes du cutter (sans ses scripts), en blanc transparent.
        fantome = new GameObject("AideDecoupe_CutterFantome");
        Material transparent = new Material(Shader.Find("Sprites/Default")); // un matériau simple qui gère la transparence
        transparent.color = new Color(1f, 1f, 1f, opaciteFantome);
        foreach (MeshFilter forme in cutter.GetComponentsInChildren<MeshFilter>(true))
        {
            GameObject morceau = new GameObject(forme.name);
            morceau.transform.SetParent(fantome.transform, false);
            // Même place, même orientation et même taille que dans le vrai cutter (vues depuis le cutter).
            morceau.transform.localPosition = cutter.InverseTransformPoint(forme.transform.position);
            morceau.transform.localRotation = Quaternion.Inverse(cutter.rotation) * forme.transform.rotation;
            Vector3 taille = forme.transform.lossyScale, tailleCutter = cutter.lossyScale;
            morceau.transform.localScale = new Vector3(taille.x / tailleCutter.x, taille.y / tailleCutter.y, taille.z / tailleCutter.z);
            morceau.AddComponent<MeshFilter>().sharedMesh = forme.sharedMesh;
            morceau.AddComponent<MeshRenderer>().sharedMaterial = transparent;
        }
        fantome.SetActive(false);
    }

    private void Update()
    {
        List<Transform> traits = patron.TraitsEnCours;
        // Cutter en main, et pas de découpe depuis 5 secondes (ou jamais découpé : tout de suite).
        bool montrer = traits != null && traits.Count > 0 && cutter.gameObject.activeInHierarchy && prise.isSelected
                       && Time.time - patron.DerniereDecoupe > attente;
        segment.SetActive(montrer);
        fantome.SetActive(montrer);
        if (!montrer)
        {
            avance = 0f; // la prochaine fois, il repartira du début des pointillés
            return;
        }

        // La longueur du chemin : d'un trait restant au suivant, dans l'ordre du contour.
        float longueur = 0f;
        for (int i = 1; i < traits.Count; i++)
            longueur += Vector3.Distance(traits[i - 1].position, traits[i].position);

        // Le segment avance lentement, et repart du début une fois arrivé au bout.
        avance += vitesse * Time.deltaTime;
        if (avance > longueur)
            avance = 0f;

        // Où est-il sur le chemin, et dans quelle direction va-t-il ?
        Vector3 ici = traits[0].position, direction = traits.Count > 1 ? traits[1].position - traits[0].position : transform.right;
        float reste = avance;
        for (int i = 1; i < traits.Count; i++)
        {
            Vector3 a = traits[i - 1].position, b = traits[i].position;
            float morceau = Vector3.Distance(a, b);
            if (reste <= morceau || i == traits.Count - 1)
            {
                ici = Vector3.Lerp(a, b, morceau > 0f ? Mathf.Clamp01(reste / morceau) : 0f);
                direction = b - a;
                break;
            }
            reste -= morceau;
        }
        if (direction.sqrMagnitude < 1e-8f)
            direction = transform.right;
        direction.Normalize();

        // Le dessus du tissu (son côté visible) : à l'opposé de son axe z.
        Vector3 dessus = -transform.forward;

        // Le segment, couché sur les pointillés, juste au-dessus du tissu.
        segment.transform.SetPositionAndRotation(ici + dessus * 0.003f, Quaternion.LookRotation(direction, dessus));
        segment.transform.localScale = new Vector3(0.008f, 0.004f, longueurSegment);

        // Le fantôme : la lame plantée dans le tissu, penchée dans le sens du mouvement, la pointe sur le segment.
        Quaternion pose = Quaternion.LookRotation((-dessus + direction * 0.7f).normalized, dessus);
        fantome.transform.localScale = cutter.lossyScale;
        fantome.transform.rotation = pose;
        fantome.transform.position = ici - pose * Vector3.Scale(pointeDansLeCutter, cutter.lossyScale);
    }

    // Le tissu est caché (fin de la découpe) : l'aide disparaît avec lui.
    private void OnDisable()
    {
        if (segment != null) segment.SetActive(false);
        if (fantome != null) fantome.SetActive(false);
    }

    private void OnDestroy()
    {
        // Ce sont des objets à part : on les supprime avec le tissu.
        if (segment != null) Destroy(segment);
        if (fantome != null) Destroy(fantome);
    }
}
