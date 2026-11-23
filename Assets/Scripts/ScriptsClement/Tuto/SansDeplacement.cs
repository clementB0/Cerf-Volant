using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

// FONCTIONNALITÉ : le joueur ne se déplace pas avec les manettes (il bouge seulement en marchant pour de vrai).
//  - plus de déplacement avec le joystick gauche ;
//  - plus de téléportation avec le joystick droit (et plus de rayon de téléportation) ;
//  - plus de rotation de la caméra avec le joystick.
// On ne modifie pas le XR Origin : on coupe ses actions de déplacement, et on éteint ses systèmes de déplacement
// (sauf la gravité, qui garde le joueur au sol).
// À mettre sur l'objet "ChoixCerfVolant" (il est là dès le début de la scène).
public class SansDeplacement : MonoBehaviour
{
    // Les actions de manette à couper, dans les groupes "XRI Left Locomotion" et "XRI Right Locomotion".
    private static readonly HashSet<string> actionsCoupees = new HashSet<string>
        { "Move", "Turn", "Snap Turn", "Teleport Mode", "Teleport Mode Cancel", "Grab Move", "Jump" };

    private readonly List<InputAction> actionsAllumees = new List<InputAction>(); // réutilisée à chaque image

    private void Start()
    {
        // Les systèmes de déplacement du XR Origin (marche, rotation, téléportation, escalade, saut...) : éteints.
        foreach (LocomotionProvider deplacement in FindObjectsByType<LocomotionProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!(deplacement is GravityProvider)) // la gravité reste : le joueur ne flotte pas
                deplacement.enabled = false;
        }
    }

    // LateUpdate : après le gestionnaire de manettes du XR Origin, qui rallume parfois ces actions pendant son Update.
    private void LateUpdate()
    {
        actionsAllumees.Clear();
        InputSystem.ListEnabledActions(actionsAllumees);
        foreach (InputAction action in actionsAllumees)
        {
            bool deplacement = action.actionMap != null && action.actionMap.name.Contains("Locomotion") && actionsCoupees.Contains(action.name);
            if (deplacement)
                action.Disable();
        }
    }
}
