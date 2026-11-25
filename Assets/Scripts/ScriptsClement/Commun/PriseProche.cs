using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

// FONCTIONNALITÉ : on n'attrape plus les objets de loin (plus de rayon), seulement de près...
// ... mais "de près" va plus loin qu'avant : dès que la main passe à 20 cm d'un objet, il vient directement dans la main.
// Quand plusieurs objets sont proches, c'est celui qui touche presque la main qui est pris (pas un voisin).
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
            // Parmi les objets proches, prendre celui dont la surface est la plus près de la main
            // (avant : celui dont le point de prise était le plus près, souvent décalé : ex. un crayon voisin dans la boîte).
            main.nearCasterSortingStrategy = NearFarInteractor.NearCasterSortingStrategy.ClosestPointOnCollider;
            if (main.nearInteractionCaster is SphereInteractionCaster sphere)
                sphere.castRadius = porteeProche;
            // Avec une manette : chercher et attraper depuis la poignée (la main), pas depuis le bout de la manette.
            if (!main.TryGetComponent(out PointDePriseMain _))
                main.gameObject.AddComponent<PointDePriseMain>();
        }
    }
}
