using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// FONCTIONNALITÉ : découper avec le cutter.
// Quand on TIENT le cutter et que sa pointe touche le tissu, les pointillés sous la pointe se relient,
// et la manette qui tient le cutter vibre légèrement.
// La main seule ne découpe pas.
// À mettre sur l'objet "Tissu", avec PatronPointilles (un Quad couché, de 1 m x 1 m quand son échelle vaut 1).
//
// POUR CHANGER L'ENDROIT QUI DÉCOUPE : déplace l'objet "Pointe" (enfant du Cutter).
// Dans la vue Scene, une sphère jaune montre ce point, et sa taille montre la zone de contact (distanceContact).
public class TraceurCutter : MonoBehaviour
{
    [SerializeField] private Transform pointeDuCutter;      // l'endroit qui découpe (l'objet "Pointe" dans le Cutter)

    // En lecture seule : AideDecoupe s'en sert pour placer le cutter fantôme.
    public Transform Pointe => pointeDuCutter;
    [SerializeField] private float distanceContact = 0.08f; // la pointe "touche" jusqu'à 8 cm au-dessus ou en dessous du tissu (au cas où on rate)
    [SerializeField] private float forceVibration = 0.15f;  // de 0 (rien) à 1 (très fort) : légère

    private XRGrabInteractable cutter; // le composant du template qui permet d'attraper le cutter : il sait si on le tient
    private PatronPointilles patron;   // les pointillés du même tissu, à relier

    private void Start()
    {
        patron = GetComponent<PatronPointilles>();                            // obligatoire : sans pointillés, rien à découper
        cutter = pointeDuCutter.GetComponentInParent<XRGrabInteractable>();   // la pointe est rangée dans le cutter
    }

    private void Update()
    {
        // Personne ne tient le cutter ("isSelected" = attrapé) : on ne découpe pas.
        if (!cutter.isSelected)
            return;

        // Position de la pointe vue "depuis le tissu" : x et y = où on est sur le tissu, z = hauteur au-dessus.
        Vector3 local = transform.InverseTransformPoint(pointeDuCutter.position);

        bool auDessusDuTissu = Mathf.Abs(local.x) < 0.5f && Mathf.Abs(local.y) < 0.5f; // le Quad va de -0.5 à 0.5
        // La hauteur en vrais mètres (le tissu est réduit avec l'atelier : "local.z" ne serait pas en mètres).
        float hauteur = Vector3.Dot(pointeDuCutter.position - transform.position, transform.forward);
        bool assezPres = Mathf.Abs(hauteur) < distanceContact;

        if (auDessusDuTissu && assezPres)
        {
            // On "écrase" la pointe sur le tissu (z = 0) pour comparer avec les traits, qui sont à plat.
            patron.RelierTraitsPres(transform.TransformPoint(new Vector3(local.x, local.y, 0f)));

            // Légère vibration dans la manette qui tient le cutter, tant que la lame touche le tissu.
            // Chaque vibration dure 2 images : elles se chevauchent, on sent donc une vibration continue.
            // (Une main suivie sans manette ne peut pas vibrer : dans ce cas, il ne se passe rien.)
            if (cutter.interactorsSelecting[0] is XRBaseInputInteractor manette)
                manette.SendHapticImpulse(forceVibration, Time.deltaTime * 2f);
        }
    }

    // Dessin d'aide dans la vue Scene (jamais visible en jeu) : la zone qui découpe, autour de la pointe.
    // Une boîte jaune, à plat comme le tissu : haute de distanceContact au-dessus et en dessous,
    // large comme la distance à laquelle un pointillé se découpe (rayonContact, dans PatronPointilles).
    // Si le tissu passe dans la boîte, ça découpe.
    private void OnDrawGizmos()
    {
        if (pointeDuCutter == null)
            return;
        PatronPointilles pointilles = GetComponent<PatronPointilles>();
        float largeur = pointilles != null ? pointilles.RayonContact : 0.02f;
        Gizmos.color = Color.yellow;
        Gizmos.matrix = Matrix4x4.TRS(pointeDuCutter.position, transform.rotation, Vector3.one); // tournée comme le tissu
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(largeur * 2f, largeur * 2f, distanceContact * 2f));
        Gizmos.DrawSphere(Vector3.zero, 0.005f); // le point exact, au centre
    }
}
