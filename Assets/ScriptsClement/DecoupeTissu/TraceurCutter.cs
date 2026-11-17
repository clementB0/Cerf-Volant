using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// FONCTIONNALITÉ : découper avec le cutter.
// Quand on TIENT le cutter et que sa pointe touche le tissu, les pointillés sous la pointe se relient.
// La main seule ne découpe pas.
// À mettre sur l'objet "Tissu", avec PatronPointilles (un Quad couché, de 1 m x 1 m quand son échelle vaut 1).
public class TraceurCutter : MonoBehaviour
{
    [SerializeField] private Transform pointeDuCutter;      // le bout de la lame (l'objet "Pointe" dans le Cutter)
    [SerializeField] private float distanceContact = 0.02f; // à moins de 2 cm du tissu, la pointe "touche"

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
        bool assezPres = Mathf.Abs(local.z) < distanceContact;

        if (auDessusDuTissu && assezPres)
        {
            // On "écrase" la pointe sur le tissu (z = 0) pour comparer avec les traits, qui sont à plat.
            patron.RelierTraitsPres(transform.TransformPoint(new Vector3(local.x, local.y, 0f)));
        }
    }
}
