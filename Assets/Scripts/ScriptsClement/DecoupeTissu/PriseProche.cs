using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

// FONCTIONNALITÉ : on n'attrape plus les objets de loin (plus de rayon), seulement de près...
// ... mais "de près" va plus loin qu'avant : dès que la main passe à 20 cm d'un objet, il vient directement dans la main.
// Rien à poser dans la scène : ça se règle tout seul à chaque chargement de scène, sur les deux mains (manettes et mains suivies).
// (Le rayon de téléportation n'est pas concerné : c'est une autre "main".)
public static class PriseProche
{
    private const float porteeProche = 0.20f; // avant : 0,10 (10 cm)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AuLancement()
    {
        Regler();
        SceneManager.sceneLoaded += (scene, mode) => Regler(); // les scènes chargées ensuite aussi
    }

    private static void Regler()
    {
        // "Include" : aussi les mains éteintes pour l'instant (ex. une manette pas encore suivie par le casque).
        foreach (NearFarInteractor main in Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            main.enableFarCasting = false; // plus de prise de loin
            if (main.nearInteractionCaster is SphereInteractionCaster sphere)
                sphere.castRadius = porteeProche;
        }
    }
}
