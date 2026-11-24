using UnityEngine;

// FONCTIONNALITÉ : le cutter disparaît quand la découpe est finie (on n'en a plus besoin).
// À mettre sur l'objet "PieceDecoupee" : il apparaît pile à la fin de la découpe, donc son Start = "découpe finie".
public class CacherCutter : MonoBehaviour
{
    [SerializeField] private GameObject cutter;
    [SerializeField] private float delai = 0.2f; // petite attente, le temps que le nuage de fumée cache le cutter

    private void Start()
    {
        // "Invoke" appelle la méthode Cacher après "delai" secondes.
        Invoke(nameof(Cacher), delai);
    }

    private void Cacher()
    {
        // Cacher le cutter le lâche aussi de la main toute seule (une main ne peut pas tenir un objet caché).
        if (cutter != null)
            cutter.SetActive(false);
    }
}
