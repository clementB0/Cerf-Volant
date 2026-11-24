using UnityEngine;

// FONCTIONNALITÉ : un "ta-da !" quand la peinture est finie.
// À mettre sur l'objet "PieceDecoupee", à côté de VolCerfVolant.
// Comment on sait que la peinture est finie : dans la scène, la fin de la peinture allume le script VolCerfVolant
// (il est éteint pendant la peinture). Dès qu'il s'allume, on joue le son.
public class SonFinPeinture : MonoBehaviour
{
    [SerializeField] private AudioClip sonFinPeinture;   // le "ta-da !" (SonFinPeinture.wav)
    [SerializeField] private float volume = 0.7f;

    private VolCerfVolant vol;
    private bool volAllume;   // était-il allumé à l'image d'avant ?

    private void Start()
    {
        vol = GetComponent<VolCerfVolant>();
        volAllume = vol.enabled; // s'il est déjà allumé au départ (scène sans peinture), on ne jouera pas le son
    }

    private void Update()
    {
        // Il vient juste de s'allumer : la peinture est finie.
        if (vol.enabled && !volAllume && sonFinPeinture != null)
            AudioSource.PlayClipAtPoint(sonFinPeinture, transform.position, volume);

        volAllume = vol.enabled;
    }
}
