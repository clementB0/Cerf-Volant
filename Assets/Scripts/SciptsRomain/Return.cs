using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;
public class Return : MonoBehaviour
{
    
    private XRGrabInteractable grab;
    private Rigidbody rb;

    private Transform initialParent;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;

    private bool initialKinematic;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        // On mémorise la position de départ
        initialParent = transform.parent;
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        initialScale = transform.localScale;

        initialKinematic = rb.isKinematic;

    }

    void OnEnable()
    {
        grab.selectExited.AddListener(OnReleased);
    }

    void OnDisable()
    {
        grab.selectExited.RemoveListener(OnReleased);
    }

    void OnReleased(SelectExitEventArgs args)
    {
        StartCoroutine(ReturnToStart());
    }


    IEnumerator ReturnToStart()
    {
        // On attend la prochaine étape physique
        yield return new WaitForFixedUpdate();

        // On bloque la physique
        rb.isKinematic = true;

        // On arrête les mouvements
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // On restaure la hiérarchie et la position
        transform.SetParent(initialParent, false);
        transform.localPosition = initialPosition;
        transform.localRotation = initialRotation;
        transform.localScale = initialScale;

        // On synchronise avec le Rigidbody
        rb.position = transform.position;
        rb.rotation = transform.rotation;

        Physics.SyncTransforms();

        // On laisse Unity actualiser les collisions
        yield return new WaitForFixedUpdate();

        // On réactive la physique
        rb.isKinematic = initialKinematic;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

}

