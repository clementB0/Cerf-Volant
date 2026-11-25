using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

// FONCTIONNALITÉ : avec les manettes, la main attrape depuis la POIGNÉE de la manette (là où est vraiment la main),
// pas depuis le bout avant de la manette (le "pointeur", plusieurs cm devant : ça prenait le crayon d'à côté).
//  - Les objets proches sont cherchés et triés autour de ce point ;
//  - un objet attrapé vient se mettre dans la main, à ce point.
// Avec les mains suivies (sans manette), rien ne change.
// Rien à poser dans la scène : PriseProche l'ajoute tout seul sur les deux mains.
[DefaultExecutionOrder(-200)] // avant XRI : la main cherche les objets depuis la bonne place dès cette image
public class PointDePriseMain : MonoBehaviour
{
    private NearFarInteractor main;
    private Transform pointDePrise;
    private bool surUneManette; // la main est rangée dans "Left Controller" / "Right Controller" (pas une main suivie)

    private void Start()
    {
        main = GetComponent<NearFarInteractor>();
        surUneManette = transform.parent != null && transform.parent.name.Contains("Controller");

        pointDePrise = new GameObject("Point de prise (poignee)").transform;
        pointDePrise.SetParent(transform, false);

        if (main.nearInteractionCaster is InteractionCasterBase recherche)
            recherche.castOrigin = pointDePrise;                              // les objets proches sont cherchés autour de la poignée
        if (main.interactionAttachController != null)
            main.interactionAttachController.transformToFollow = pointDePrise; // et ils viennent se mettre là
    }

    private void Update()
    {
        pointDePrise.localPosition = surUneManette ? PoigneeVueDuPointeur() : Vector3.zero;
    }

    // Où est la poignée, vue depuis le bout de la manette (l'endroit où est rangée cette main).
    // Les deux poses sont lues dans la même image sur la manette : le décalage est juste, même quand la main bouge vite.
    private Vector3 PoigneeVueDuPointeur()
    {
        XRController manette = main.handedness == InteractorHandedness.Left ? XRController.leftHand : XRController.rightHand;
        Vector3Control poignee = manette?.TryGetChildControl<Vector3Control>("devicePosition");
        Vector3Control pointeur = manette?.TryGetChildControl<Vector3Control>("pointerPosition");
        QuaternionControl rotationPointeur = manette?.TryGetChildControl<QuaternionControl>("pointerRotation");
        if (poignee == null || pointeur == null || rotationPointeur == null)
            return Vector3.zero;

        Vector3 decalage = Quaternion.Inverse(rotationPointeur.ReadValue()) * (poignee.ReadValue() - pointeur.ReadValue());
        return decalage.magnitude < 0.2f ? decalage : Vector3.zero; // plus de 20 cm : valeurs pas fiables (manette perdue), on n'y touche pas
    }
}
