using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// FONCTIONNALITÉ : un petit "pop" quand on attrape un objet.
// Ça marche pour tous les objets attrapables de la scène (cutter, poignées, cordes, bombes de peinture, cerf-volant...).
// À mettre sur l'objet "CerfVolantADecouper" (le parent de tout).
public class SonPrise : MonoBehaviour
{
    [SerializeField] private AudioClip sonPrise;   // quand on attrape (SonPrise.wav)
    [SerializeField] private float volume = 0.5f;

    // Il peut y avoir plusieurs cerfs-volants dans la scène, chacun avec ce script :
    // seul le premier qui démarre s'occupe des sons, sinon chaque pop serait joué plusieurs fois.
    private static SonPrise chef;

    private void Start()
    {
        if (chef != null)
            return; // un autre s'en occupe déjà
        chef = this;

        // On cherche tous les objets attrapables de la scène, même ceux encore cachés (le cerf-volant découpé, les bombes...).
        foreach (XRGrabInteractable objet in FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // "selectEntered" = une main vient de l'attraper.
            objet.selectEntered.AddListener(QuandOnAttrape);
        }
    }

    private void QuandOnAttrape(SelectEnterEventArgs prise)
    {
        // Le pop sort de l'objet attrapé (donc de ta main). Sans son réglé dans l'Inspector, on ne joue rien.
        if (sonPrise != null)
            AudioSource.PlayClipAtPoint(sonPrise, prise.interactableObject.transform.position, volume);
    }
}
