using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// FONCTIONNALITÉ : quand le bouton rouge est enfoncé à fond, on le sent et on l'entend :
// un son de réussite, une vibration dans la manette, et le bouton "rebondit" (il grossit un instant puis revient).
// À mettre sur le bouton rouge "Passer au montage", à côté de son XRSimpleInteractable.
[RequireComponent(typeof(XRSimpleInteractable))]
public class BoutonAppuye : MonoBehaviour
{
    [SerializeField] private AudioClip son;                       // le son "c'est validé !" (SonReussite.wav)
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;
    [SerializeField] private Transform visuel;                    // ce qui rebondit (l'objet "Visuals" du bouton)
    [SerializeField] private float rebond = 0.15f;                // il grossit de 15 %
    [SerializeField] private float dureeRebond = 0.25f;

    private XRSimpleInteractable bouton;
    private Vector3 tailleDepart;

    private void Awake()
    {
        bouton = GetComponent<XRSimpleInteractable>();
        if (visuel != null)
            tailleDepart = visuel.localScale;
    }

    private void OnEnable() => bouton.selectEntered.AddListener(Appuye);
    private void OnDisable()
    {
        bouton.selectEntered.RemoveListener(Appuye);
        if (visuel != null)
            visuel.localScale = tailleDepart; // (caché en plein rebond : on le remet droit)
    }

    private void Appuye(SelectEnterEventArgs args)
    {
        if (son != null)
            AudioSource.PlayClipAtPoint(son, transform.position, volume);
        if (args.interactorObject is XRBaseInputInteractor main)
            main.SendHapticImpulse(0.5f, 0.12f);
        if (visuel != null && isActiveAndEnabled)
            StartCoroutine(Rebondir());
    }

    private IEnumerator Rebondir()
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / dureeRebond)
        {
            visuel.localScale = tailleDepart * (1f + rebond * Mathf.Sin(t * Mathf.PI)); // monte puis redescend
            yield return null;
        }
        visuel.localScale = tailleDepart;
    }
}
