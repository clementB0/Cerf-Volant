using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Fusion : MonoBehaviour
{
    [Header("Fusion")]
    [SerializeField] private Transform newParent;
    [SerializeField] private AudioClip sonConnexion;
    [SerializeField] private FilCerfVolant fil;

    private bool fusionEffectuee;

    private void OnTriggerEnter(Collider other)
    {
        if (fusionEffectuee || !other.TryGetComponent<CerfVolant>(out var accroche) || !accroche.Accepte(transform))
            return;

        fusionEffectuee = true;
        if (fil != null)
            fil.Nouer(accroche.transform.position);

        // La destruction du bout libère aussi la sélection XR.
        if (TryGetComponent<XRGrabInteractable>(out var prise) &&
            prise.firstInteractorSelecting is XRBaseInputInteractor manette)
        {
            manette.SendHapticImpulse(0.18f, 0.06f);
        }

        if (sonConnexion != null)
            AudioSource.PlayClipAtPoint(sonConnexion, accroche.transform.position, 0.8f);

        VolCerfVolant vol = accroche.GetComponentInParent<VolCerfVolant>();
        if (vol != null)
            vol.ConfirmerConnexion(transform);

        Destroy(gameObject);

        if (newParent != null && newParent.gameObject != gameObject)
            Destroy(newParent.gameObject);
    }
}
