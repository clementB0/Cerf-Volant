using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class SprayInput : MonoBehaviour
{
    private XRGrabInteractable grab;
    [SerializeField, Range(0f, 1f)] private float vibrationDeclenchement = 0.16f;
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
        if (triggerHeld || !grab.isSelected || args.interactorObject != grab.firstInteractorSelecting)
            return;

        triggerHeld = true;
        if (grab.firstInteractorSelecting is XRBaseInputInteractor manette)
            manette.SendHapticImpulse(vibrationDeclenchement, 0.055f);
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
