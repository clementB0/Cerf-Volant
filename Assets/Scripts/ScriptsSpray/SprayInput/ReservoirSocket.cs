using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Le pot se pose sur le pistolet (au point d'insertion), puis on le tourne sur lui-même
// (d'un côté ou de l'autre, un petit geste suffit) pour le clipser. Pour l'enlever : on le reprend et on le tire.
[DisallowMultipleComponent]
public class ReservoirSocket : MonoBehaviour
{
    [SerializeField] private Transform insertionPoint;  // là où le pot est posé ; on tourne autour de son axe "up"
    [SerializeField] private Collider gunCollider;
    [SerializeField] private PaintReservoir[] compatibleReservoirs;
    [SerializeField, Min(0.01f)] private float captureRadius = 0.065f;
    [SerializeField, Range(5f, 180f)] private float clipAngle = 15f;    // de combien tourner pour clipser
    [SerializeField, Min(0.01f)] private float pullDistance = 0.04f;   // de combien tirer (en m) pour l'enlever

    private XRGrabInteractable gunGrab;
    private XRSelectFilterDelegate potFirst;
    private PaintReservoir attached;
    private PaintReservoir lastRemoved;
    private IXRSelectInteractor twistingHand;
    private Quaternion lastHandRotation;
    private Vector3 handStart;
    private bool waitForRelease;
    private bool locked;
    private float twist;      // degrés tournés par la main depuis qu'elle a posé le pot
    private float seatAngle;  // angle du pot une fois clipsé (+clipAngle ou -clipAngle)

    public PaintReservoir LockedReservoir =>
        locked && attached != null && attached.isActiveAndEnabled ? attached : null;

    // Pot posé mais pas encore clipsé (pour les flèches "tourne").
    public PaintReservoir PendingReservoir => locked ? null : attached;
    public Transform InsertionPoint => insertionPoint;

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

            // Lâché : le pot reste clipsé.
            twistingHand = null;
            waitForRelease = false;
        }
        else if (!waitForRelease && hand != twistingHand)
        {
            BeginTwist(hand);
        }
        else if (!waitForRelease && !locked)
        {
            twist += HandTwistDelta(hand);
            if (Mathf.Abs(twist) >= clipAngle)
            {
                locked = true;
                seatAngle = Mathf.Sign(twist) * clipAngle;
                // La main doit lâcher puis reprendre le pot pour l'enlever.
                waitForRelease = true;
                ReservoirLocked?.Invoke(hand);
            }
        }
        else if (!waitForRelease &&
                 (hand.GetAttachTransform(attached.Grab).position - handStart).sqrMagnitude >= pullDistance * pullDistance)
        {
            // Clipsé : il suffit de le tirer pour l'enlever.
            lastRemoved = attached;
            Detach();
            return;
        }

        PlaceInSeat();
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
        BeginTwist(attached.Grab.firstInteractorSelecting);
        PlaceInSeat();
    }

    private void BeginTwist(IXRSelectInteractor hand)
    {
        twistingHand = hand;
        lastHandRotation = hand.GetAttachTransform(attached.Grab).rotation;
        handStart = hand.GetAttachTransform(attached.Grab).position;
    }

    // De combien la main a tourné autour de l'axe du pot depuis la dernière image.
    private float HandTwistDelta(IXRSelectInteractor hand)
    {
        Quaternion rotation = hand.GetAttachTransform(attached.Grab).rotation;
        (rotation * Quaternion.Inverse(lastHandRotation)).ToAngleAxis(out float angle, out Vector3 axis);
        lastHandRotation = rotation;
        if (angle > 180f)
            angle -= 360f;
        return angle < 0.01f && angle > -0.01f ? 0f : angle * Vector3.Dot(axis, insertionPoint.up);
    }

    private void PlaceInSeat()
    {
        float angle = locked ? seatAngle : Mathf.Clamp(twist, -clipAngle, clipAngle);
        attached.transform.SetPositionAndRotation(
            insertionPoint.position, Quaternion.AngleAxis(angle, insertionPoint.up) * insertionPoint.rotation);
    }

    private void Detach()
    {
        if (attached != null)
        {
            Physics.IgnoreCollision(gunCollider, attached.GrabCollider, false);
            attached.SetOnRail(false);
        }

        attached = null;
        twistingHand = null;
        waitForRelease = false;
        twist = 0f;
        seatAngle = 0f;
        locked = false;
    }

    private void OnEnable()
    {
        gunGrab = GetComponent<XRGrabInteractable>();
        potFirst = new XRSelectFilterDelegate((hand, _) => !IsOnLockedPot(hand));
        gunGrab.selectFilters.Add(potFirst);
    }

    private void OnDisable()
    {
        gunGrab.selectFilters.Remove(potFirst);
        Detach();
    }

    // Une main qui touche le pot clipsé prend le pot, pas le pistolet (sauf si elle tient déjà le pistolet).
    private bool IsOnLockedPot(IXRSelectInteractor hand) =>
        LockedReservoir != null && !hand.IsSelecting(gunGrab) &&
        hand is IXRHoverInteractor hover && LockedReservoir.Grab.IsHovered(hover);
}
