using UnityEngine;

// FONCTIONNALITÉ : le bruit du tissu qui claque dans le vent pendant que le cerf-volant vole.
// Plus le vent qu'il sent est fort (rafale, ou quand tu tires sur le fil), plus le bruit est fort et aigu.
// À mettre sur l'objet "PieceDecoupee", à côté de VolCerfVolant (c'est lui qui connaît le vent).
public class SonVol : MonoBehaviour
{
    [SerializeField] private AudioClip sonVol;                  // le tissu qui claque, en boucle (SonVol.wav)
    [SerializeField] private float volumeMax = 0.6f;
    [SerializeField] private float ventPourVolumeMax = 8f;      // à cette vitesse de vent (m/s), le son est au plus fort

    private VolCerfVolant vol;
    private AudioSource hautParleur;

    private void Start()
    {
        vol = GetComponent<VolCerfVolant>();

        // Le haut-parleur est sur le cerf-volant : on l'entend venir de là-haut.
        hautParleur = gameObject.AddComponent<AudioSource>();
        hautParleur.clip = sonVol;
        hautParleur.loop = true;
        hautParleur.playOnAwake = false;
        hautParleur.spatialBlend = 1f;  // en 3D
        hautParleur.volume = 0f;        // il démarre en silence, puis monte doucement
    }

    private void Update()
    {
        // Le volume voulu : rien au sol, sinon selon la force du vent qu'il sent (de 0 à 1).
        float force = vol.EnVol ? Mathf.Clamp01(vol.VentApparent.magnitude / ventPourVolumeMax) : 0f;

        // Le son monte et descend en douceur (sur environ un quart de seconde), sans à-coups.
        hautParleur.volume = Mathf.MoveTowards(hautParleur.volume, force * volumeMax, 4f * Time.deltaTime);

        // Un vent fort fait claquer le tissu plus vite : le son devient un peu plus aigu.
        hautParleur.pitch = 0.85f + force * 0.35f;

        // On ne fait tourner le son que quand on l'entend (ça économise le casque).
        if (hautParleur.volume > 0f && !hautParleur.isPlaying)
            hautParleur.Play();
        else if (hautParleur.volume == 0f && hautParleur.isPlaying)
            hautParleur.Stop();
    }
}
