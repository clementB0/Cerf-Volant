using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
public class ReservoirSocket : MonoBehaviour
{
    [SerializeField] private Transform insertionPoint;
    [SerializeField] private Collider gunCollider;
    [SerializeField] private PaintReservoir[] compatibleReservoirs;
    [SerializeField, Min(0.01f)] private float captureRadius = 0.065f;
    [SerializeField, Min(0.01f)] private float slideDistance = 0.045f;

    private PaintReservoir attached;
    private PaintReservoir lastRemoved;
    private IXRSelectInteractor slidingHand;
    private bool waitForRelease;
    private float handStart;
    private float railPosition;
    private bool locked;

    public PaintReservoir LockedReservoir =>
        locked && railPosition >= slideDistance - 0.002f && attached != null &&
        attached.isActiveAndEnabled ? attached : null;

    public event Action<IXRSelectInteractor> ReservoirLocked;

    private void LateUpdate()
    {
        if (attached == null)
        {
            FindCandidate();
            return;
        }

        if (!attached.isActiveAndEnabled)
        {
            Detach();
            return;
        }

        var hand = attached.Grab.firstInteractorSelecting;
        if (hand == null)
        {
            if (!locked)
            {
                Detach();
                return;
            }

            // Releasing partway through removal returns the reservoir to its seat.
            slidingHand = null;
            waitForRelease = false;
            railPosition = slideDistance;
        }
        else if (!waitForRelease && hand != slidingHand)
        {
            BeginSlide(hand);
        }
        else if (!waitForRelease)
        {
            float travel = HandPosition(hand) - handStart;
            railPosition = Mathf.Clamp((locked ? slideDistance : 0f) + travel, 0f, slideDistance);
            if (!locked && railPosition >= slideDistance)
            {
                locked = true;
                // Keep the current grip seated until it is released and grabbed again.
                waitForRelease = true;
                ReservoirLocked?.Invoke(hand);
            }
            else if (locked && railPosition <= 0f)
            {
                PlaceOnRail();
                lastRemoved = attached;
                Detach();
                return;
            }
        }

        PlaceOnRail();
    }

    private void FindCandidate()
    {
        float bestDistance = captureRadius * captureRadius;
        if (lastRemoved != null && (!lastRemoved.Grab.isSelected ||
            (lastRemoved.transform.position - insertionPoint.position).sqrMagnitude >= bestDistance))
        {
            lastRemoved = null;
        }

        PaintReservoir closest = null;
        foreach (var reservoir in compatibleReservoirs)
        {
            if (reservoir == null || reservoir == lastRemoved ||
                !reservoir.isActiveAndEnabled || reservoir.OnRail ||
                !reservoir.Grab.isSelected)
                continue;

            float distance = (reservoir.transform.position - insertionPoint.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                closest = reservoir;
                bestDistance = distance;
            }
        }

        if (closest == null)
            return;

        attached = closest;
        attached.SetOnRail(true);
        Physics.IgnoreCollision(gunCollider, attached.GrabCollider, true);
        BeginSlide(attached.Grab.firstInteractorSelecting);
        PlaceOnRail();
    }

    private void BeginSlide(IXRSelectInteractor hand)
    {
        slidingHand = hand;
        handStart = HandPosition(hand);
    }

    private float HandPosition(IXRSelectInteractor hand)
    {
        Vector3 grip = hand.GetAttachTransform(attached.Grab).position;
        return Vector3.Dot(grip - insertionPoint.position, insertionPoint.right);
    }

    private void PlaceOnRail()
    {
        attached.transform.SetPositionAndRotation(
            insertionPoint.position + insertionPoint.right * railPosition, insertionPoint.rotation);
    }

    private void Detach()
    {
        if (attached != null)
        {
            Physics.IgnoreCollision(gunCollider, attached.GrabCollider, false);
            attached.SetOnRail(false);
        }

        attached = null;
        slidingHand = null;
        waitForRelease = false;
        railPosition = 0f;
        locked = false;
    }

    private void OnDisable()
    {
        Detach();
    }
}
