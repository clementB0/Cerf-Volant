using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class SprayInput : MonoBehaviour
{
    private XRGrabInteractable grab;
    private bool triggerHeld;

    // Lu par SprayPainter pour peindre et par SprayConeVFX pour afficher le jet.
    public bool IsSpraying =>
        isActiveAndEnabled &&
        grab != null &&
        grab.isSelected &&
        triggerHeld;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        grab.activated.AddListener(OnActivated);
        grab.deactivated.AddListener(OnDeactivated);
        grab.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        triggerHeld = false;

        grab.activated.RemoveListener(OnActivated);
        grab.deactivated.RemoveListener(OnDeactivated);
        grab.selectExited.RemoveListener(OnReleased);
    }

    private void OnActivated(ActivateEventArgs args)
    {
        triggerHeld = grab.isSelected;
    }

    private void OnDeactivated(DeactivateEventArgs args)
    {
        triggerHeld = false;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        triggerHeld = false;
    }
}