using UnityEngine;

// FONCTIONNALITÉ : un "pouf !" quand le cutter disparaît dans la fumée, à la fin de la découpe.
// À mettre sur l'objet "PieceDecoupee" : il apparaît pile à la fin de la découpe, donc son Start = "découpe finie".
public class SonFumee : MonoBehaviour
{
    [SerializeField] private AudioClip sonFumee;   // le "pouf !" (SonFumee.wav)
    [SerializeField] private Transform cutter;     // là où part la fumée de fin de découpe
    [SerializeField] private float volume = 0.6f;

    private void Start()
    {
        if (cutter != null && sonFumee != null)
            AudioSource.PlayClipAtPoint(sonFumee, cutter.position, volume);
    }
}
