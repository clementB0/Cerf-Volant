using UnityEngine;

// FONCTIONNALITÉ : l'ambiance de la scène, du début à la fin : une petite musique, le vent et les oiseaux.
// Les trois sons tournent en boucle, partout pareil (pas en 3D : ils ne viennent pas d'un endroit précis).
// À mettre sur l'objet "ChoixCerfVolant" (il est là dès le début de la scène).
public class SonAmbiance : MonoBehaviour
{
    [SerializeField] private AudioClip musique;      // AmbianceMusique.mp3
    [SerializeField] private float volumeMusique = 0.2f;
    [SerializeField] private AudioClip vent;         // AmbianceVent.wav
    [SerializeField] private float volumeVent = 0.25f;
    [SerializeField] private AudioClip oiseaux;      // AmbianceOiseaux.wav
    [SerializeField] private float volumeOiseaux = 0.5f;

    private void Start()
    {
        Boucle(musique, volumeMusique);
        Boucle(vent, volumeVent);
        Boucle(oiseaux, volumeOiseaux);
    }

    // Crée un haut-parleur qui joue ce son en boucle. Sans son réglé dans l'Inspector, on ne crée rien.
    private void Boucle(AudioClip son, float volume)
    {
        if (son == null)
            return;

        AudioSource hautParleur = gameObject.AddComponent<AudioSource>();
        hautParleur.clip = son;
        hautParleur.volume = volume;
        hautParleur.loop = true;
        hautParleur.spatialBlend = 0f; // 0 = pas en 3D : on l'entend pareil partout
        hautParleur.Play();
    }
}
