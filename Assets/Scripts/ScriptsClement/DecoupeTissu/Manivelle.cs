using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// FONCTIONNALITÉ : une manivelle sur le côté droit du bâton, pour dérouler ou enrouler le fil du cerf-volant.
// Avec l'autre main, on attrape sa poignée (bouton Grip) et on la fait tourner :
//  - vers l'avant (le haut de la manivelle part vers le cerf-volant) → on déroule du fil : il s'éloigne ;
//  - vers soi → on enroule le fil : il se rapproche.
// À mettre sur le bâton (l'objet "PoigneeGauche").
public class Manivelle : MonoBehaviour
{
    [SerializeField] private VolCerfVolant vol;          // le vol du cerf-volant (c'est lui qui garde la longueur du fil)
    [SerializeField] private float metresParTour = 1f;   // un tour complet de manivelle = 1 mètre de fil
    [SerializeField] private float rayon = 0.06f;        // longueur du bras de la manivelle (6 cm)
    [SerializeField] private float ecart = 0.1f;         // la manivelle est à 10 cm sur la droite du bâton
    [SerializeField] private Material materiau;          // la couleur de la manivelle (Baton.mat : noir)

    private Transform manivelle;          // la manivelle entière (elle suit le bâton)
    private Transform rotor;              // la partie qui tourne : le bras et la poignée
    private XRSimpleInteractable poignee; // la poignée qu'on attrape (elle ne se détache pas : on la fait juste tourner)
    private float angle;                  // l'angle de la manivelle, en degrés (0 = bras vers le haut)
    private bool tenait;                  // la poignée était-elle tenue à l'image d'avant ?

    private void Start()
    {
        // La manivelle est un objet à part (pas rangée dans le bâton) : le bâton a une taille étirée qui la déformerait,
        // et ses colliders seraient pris pour ceux du bâton. Elle suit donc le bâton à chaque image (LateUpdate).
        manivelle = new GameObject("Manivelle").transform;

        // L'axe (un petit cylindre couché sur le côté) qui relie la manivelle au bâton.
        Morceau(PrimitiveType.Cylinder, manivelle, new Vector3(0.025f, 0.012f, 0.025f), Vector3.zero);

        // Ce qui tourne autour de l'axe : le bras, et la poignée au bout du bras.
        rotor = new GameObject("Rotor").transform;
        rotor.SetParent(manivelle, false);
        GameObject bras = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Ranger(bras, rotor, new Vector3(0.01f, rayon, 0.014f), new Vector3(0.014f, rayon / 2f, 0f), Quaternion.identity);
        Destroy(bras.GetComponent<Collider>());
        GameObject bouton = Morceau(PrimitiveType.Cylinder, rotor, new Vector3(0.022f, 0.025f, 0.022f), new Vector3(0.04f, rayon, 0f));

        // La poignée garde son collider : c'est elle qu'on attrape. "Simple" = on la sélectionne sans l'arracher.
        poignee = bouton.AddComponent<XRSimpleInteractable>();
    }

    // Un cylindre couché le long de l'axe de la manivelle (son axe x), d'une couleur.
    private GameObject Morceau(PrimitiveType forme, Transform parent, Vector3 taille, Vector3 place)
    {
        GameObject morceau = GameObject.CreatePrimitive(forme);
        Ranger(morceau, parent, taille, place, Quaternion.Euler(0f, 0f, 90f)); // un cylindre est debout : on le couche sur le côté
        return morceau;
    }

    private void Ranger(GameObject objet, Transform parent, Vector3 taille, Vector3 place, Quaternion rotation)
    {
        objet.transform.SetParent(parent, false);
        objet.transform.localPosition = place;
        objet.transform.localRotation = rotation;
        objet.transform.localScale = taille;
        if (materiau != null)
            objet.GetComponent<Renderer>().sharedMaterial = materiau;
    }

    // LateUpdate : après le déplacement du bâton par la main, sinon la manivelle aurait une image de retard.
    private void LateUpdate()
    {
        // Elle suit le bâton : collée à son côté droit, tournée comme lui.
        manivelle.SetPositionAndRotation(transform.position + transform.right * ecart, transform.rotation);

        // Où est la main qui tient la poignée, vue depuis l'axe de la manivelle ?
        Vector3 local = Vector3.zero;
        if (poignee.isSelected)
            local = manivelle.InverseTransformPoint(poignee.interactorsSelecting[0].GetAttachTransform(poignee).position);

        // Si la main passe tout près de l'axe (moins de 2 cm), son angle n'a pas de sens (il saute dans tous les sens) : on l'ignore.
        bool angleFiable = new Vector2(local.y, local.z).magnitude > 0.02f;
        if (poignee.isSelected && angleFiable)
        {
            // Son angle autour de l'axe : 0 = en haut, positif = vers l'avant (vers le cerf-volant).
            float nouvelAngle = Mathf.Atan2(local.z, local.y) * Mathf.Rad2Deg;

            // De combien elle a tourné depuis l'image d'avant ("DeltaAngle" gère le passage de 180 à -180).
            // Juste quand on l'attrape, la main n'est pas pile au même angle : la manivelle se cale sur la main, sans changer le fil.
            // Et jamais plus de 45 degrés d'un coup : une vraie main ne tourne pas si vite en une image.
            float tour = tenait ? Mathf.Clamp(Mathf.DeltaAngle(angle, nouvelAngle), -45f, 45f) : 0f;
            angle = nouvelAngle;

            // Vers l'avant = on déroule (plus de fil), vers soi = on enroule.
            vol.ChangerLongueur(tour / 360f * metresParTour);
        }
        tenait = poignee.isSelected && (tenait || angleFiable); // on reste "en train de tourner" même si la main frôle l'axe un instant

        // Le bras tourne autour de l'axe x (un angle positif fait passer le haut vers l'avant).
        rotor.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }

    // Le bâton disparaît ou réapparaît : la manivelle aussi.
    private void OnEnable()
    {
        if (manivelle != null)
            manivelle.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (manivelle != null)
            manivelle.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (manivelle != null)
            Destroy(manivelle.gameObject);
    }
}
