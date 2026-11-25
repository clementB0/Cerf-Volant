using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class SprayInput : MonoBehaviour
{
    private XRGrabInteractable grab;
    [SerializeField] private ReservoirSocket reservoirSocket;
    [SerializeField, Range(0f, 1f)] private float vibrationDeclenchement = 0.16f;
    private bool triggerHeld;

    private bool TriggerAvailable =>
        isActiveAndEnabled &&
        grab != null &&
        grab.isSelected;

    private bool HasPaint => reservoirSocket == null || reservoirSocket.LockedReservoir != null;

    public bool IsSpraying => TriggerAvailable && triggerHeld && HasPaint;

    public float TriggerPressure
    {
        get
        {
            if (!TriggerAvailable)
                return 0f;

            if (grab.firstInteractorSelecting is XRBaseInputInteractor hand &&
                hand.activateInput.TryReadValue(out float pressure))
                return Mathf.Clamp01(pressure);

            return triggerHeld ? 1f : 0f;
        }
    }

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
        if (HasPaint && grab.firstInteractorSelecting is XRBaseInputInteractor manette)
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
