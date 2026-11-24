using UnityEngine;

// FONCTIONNALITÉ : le "pschhh" de la bombe de peinture, en boucle tant qu'on appuie sur la gâchette.
// Plus la bombe se vide (script ReservoirBombe), plus le son devient faible ; vide, plus de son.
// À mettre sur une bombe de peinture (dans la scène), à côté de SprayInput et ReservoirBombe : le son sort de la bombe.
public class SonSpray : MonoBehaviour
{
    [SerializeField] private AudioClip sonSpray;   // le "pschhh" qui tourne en boucle (SprayPaint.mp3)
    [SerializeField, Range(0f, 1f)] private float volume = 0.4f; // le volume quand la bombe est pleine

    private SprayInput spray;           // sait si on appuie sur la gâchette en la tenant
    private ReservoirBombe reservoir;   // sait combien de peinture il reste (facultatif)
    private AudioSource hautParleur;

    private void Start()
    {
        spray = GetComponent<SprayInput>();
        reservoir = GetComponent<ReservoirBombe>();

        hautParleur = gameObject.AddComponent<AudioSource>();
        hautParleur.clip = sonSpray;
        hautParleur.loop = true;           // le son recommence tant qu'on peint
        hautParleur.playOnAwake = false;
        hautParleur.spatialBlend = 1f;     // en 3D : on l'entend venir de la bombe
        hautParleur.volume = 0f;           // il démarre en silence, puis monte doucement quand on peint
    }

    private void Update()
    {
        // Le volume voulu : rien si on n'appuie pas ; sinon, d'autant plus faible que la bombe est vide.
        float charge = reservoir != null ? reservoir.Charge : 1f;
        float voulu = spray.IsSpraying ? volume * charge : 0f;

        // Le son monte et descend en un dixième de seconde : pas de "clac" quand on appuie ou qu'on relâche.
        hautParleur.volume = Mathf.MoveTowards(hautParleur.volume, voulu, volume * 10f * Time.deltaTime);

        if (hautParleur.volume > 0f && !hautParleur.isPlaying)
            hautParleur.Play();   // on commence à peindre : pschhh
        else if (hautParleur.volume == 0f && hautParleur.isPlaying)
            hautParleur.Stop();   // le son est retombé à zéro : on l'arrête
    }
}
