using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(ReservoirSocket))]
public class ReservoirFeedback : MonoBehaviour
{
    [SerializeField] private AudioClip lockClick;
    [SerializeField, Range(0f, 1f)] private float volume = 0.55f;
    [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.23f;
    [SerializeField, Min(0f)] private float hapticDuration = 0.06f;

    private ReservoirSocket socket;

    private void OnEnable()
    {
        socket = GetComponent<ReservoirSocket>();
        socket.ReservoirLocked += PlayFeedback;
    }

    private void OnDisable()
    {
        socket.ReservoirLocked -= PlayFeedback;
    }

    private void PlayFeedback(IXRSelectInteractor hand)
    {
        if (lockClick != null)
            AudioSource.PlayClipAtPoint(lockClick, transform.position, volume);

        if (hand is XRBaseInputInteractor controller)
            controller.SendHapticImpulse(hapticAmplitude, hapticDuration);
    }
}
