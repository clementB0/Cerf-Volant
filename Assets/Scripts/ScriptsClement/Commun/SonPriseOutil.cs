using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable))]
public class SonPriseOutil : MonoBehaviour
{
    [SerializeField] private AudioClip sonPrise;
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

    private XRGrabInteractable prise;
    private AudioSource source;

    private void Awake()
    {
        prise = GetComponent<XRGrabInteractable>();
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0.4f;
        source.dopplerLevel = 0f;
    }

    private void OnEnable() => prise.selectEntered.AddListener(Jouer);
    private void OnDisable() => prise.selectEntered.RemoveListener(Jouer);

    private void Jouer(SelectEnterEventArgs args)
    {
        if (sonPrise != null)
            source.PlayOneShot(sonPrise, volume);
    }
}
