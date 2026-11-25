using UnityEngine;
using UnityEngine.SceneManagement;

// FONCTIONNALITÉ : dans la boîte de crayons, les crayons sont retournés : c'est la pointe (et sa couleur) qui dépasse en haut.
// Rien à poser dans la scène : à chaque chargement de scène, les crayons de la boîte ("Boite_Crayons...") qui ont
// la pointe en bas sont retournés, sur place. (Seulement leur position de départ : les scripts des crayons, de Lukas, ne changent pas.)
public static class CrayonsPointeEnHaut
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AuLancement()
    {
        Retourner();
        SceneManager.sceneLoaded += (scene, mode) => Retourner(); // les scènes chargées ensuite aussi
    }

    private static void Retourner()
    {
        // "Include" : la boîte est cachée au début de la partie (elle apparaît pour le dessin).
        foreach (Pen crayon in Object.FindObjectsByType<Pen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!crayon.transform.root.name.StartsWith("Boite_Crayons"))
                continue; // seulement les crayons rangés dans la boîte

            Transform pointe = crayon.transform.Find("Pointe");
            MeshFilter forme = crayon.GetComponent<MeshFilter>();
            if (pointe == null || forme == null || forme.sharedMesh == null)
                continue;

            // Le milieu du crayon (son pivot est près de la pointe) : on le retourne autour de ce point, il reste à sa place dans la boîte.
            Vector3 milieu = crayon.transform.TransformPoint(forme.sharedMesh.bounds.center);
            if (pointe.position.y >= milieu.y)
                continue; // déjà la pointe en haut
            crayon.transform.RotateAround(milieu, crayon.transform.right, 180f);
        }
    }
}
