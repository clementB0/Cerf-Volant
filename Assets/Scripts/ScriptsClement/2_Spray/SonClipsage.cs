using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// FONCTIONNALITÉ : quand un pot de peinture est bien clipsé sur le pistolet, un petit son de réussite confirme
// (en plus du "clic" de ReservoirFeedback, qu'on n'entendait pas assez).
// À mettre sur le pistolet, à côté de ReservoirSocket.
[RequireComponent(typeof(ReservoirSocket))]
public class SonClipsage : MonoBehaviour
{
    [SerializeField] private AudioClip sonConfirmation;          // le son "c'est bon !" (SonChoix.wav)
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;

    private ReservoirSocket socket;

    private void OnEnable()
    {
        socket = GetComponent<ReservoirSocket>();
        socket.ReservoirLocked += Confirmer;
    }

    private void OnDisable()
    {
        socket.ReservoirLocked -= Confirmer;
    }

    private void Confirmer(IXRSelectInteractor main)
    {
        if (sonConfirmation != null)
            AudioSource.PlayClipAtPoint(sonConfirmation, transform.position, volume);
    }
}
