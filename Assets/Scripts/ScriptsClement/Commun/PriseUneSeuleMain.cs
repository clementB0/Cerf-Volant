using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// FONCTIONNALITÉ : un objet tenu dans une main ne peut pas être attrapé par l'autre main (on ne se le "vole" pas).
// Il faut le lâcher d'abord. (Par défaut, Unity le fait passer dans la nouvelle main.)
// À mettre sur l'objet qui a le XRGrabInteractable (ex. le pistolet de peinture).
[RequireComponent(typeof(XRGrabInteractable))]
public class PriseUneSeuleMain : MonoBehaviour, IXRSelectFilter
{
    private XRGrabInteractable prise;

    public bool canProcess => isActiveAndEnabled;

    private void Awake()
    {
        prise = GetComponent<XRGrabInteractable>();
        prise.selectFilters.Add(this);
    }

    private void OnDestroy()
    {
        if (prise != null)
            prise.selectFilters.Remove(this);
    }

    // Une main peut le prendre s'il n'est tenu par personne, ou si c'est elle qui le tient déjà.
    public bool Process(IXRSelectInteractor main, IXRSelectInteractable objet)
    {
        return !objet.isSelected || objet.interactorsSelecting.Contains(main);
    }
}
