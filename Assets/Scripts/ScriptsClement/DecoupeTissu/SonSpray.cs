using UnityEngine;

// FONCTIONNALITÉ : le "pschhh" des bombes de peinture, en boucle tant qu'on appuie sur la gâchette.
// Plus la bombe se vide (script ReservoirBombes), plus le son devient faible ; vide, plus de son.
// À mettre sur l'objet "PieceDecoupee", avec ReservoirBombes.
// Il trouve tout seul les bombes de la scène (celles qui ont le script SprayInput).
public class SonSpray : MonoBehaviour
{
    [SerializeField] private AudioClip sonSpray;   // le "pschhh" qui tourne en boucle (SprayPaint.mp3)
    [SerializeField] private float volume = 0.4f;  // le volume quand la bombe est pleine

    private SprayInput[] bombes;        // les bombes de peinture
    private AudioSource[] hautParleurs; // un haut-parleur par bombe : chaque bombe fait son propre bruit
    private ReservoirBombes reservoir;  // sait combien de peinture il reste dans chaque bombe

    private void Start()
    {
        reservoir = GetComponent<ReservoirBombes>();

        // "Include" : on trouve aussi les bombes encore cachées (elles apparaissent juste après la découpe).
        bombes = FindObjectsByType<SprayInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        hautParleurs = new AudioSource[bombes.Length];
        for (int i = 0; i < bombes.Length; i++)
        {
            // Le haut-parleur est posé sur la bombe : le son la suit quand tu la bouges.
            AudioSource hautParleur = bombes[i].gameObject.AddComponent<AudioSource>();
            hautParleur.clip = sonSpray;
            hautParleur.loop = true;           // le son recommence tant qu'on peint
            hautParleur.playOnAwake = false;
            hautParleur.spatialBlend = 1f;     // en 3D : on l'entend venir de la bombe
            hautParleur.volume = 0f;           // il démarre en silence, puis monte doucement quand on peint
            hautParleurs[i] = hautParleur;
        }
    }

    private void Update()
    {
        for (int i = 0; i < bombes.Length; i++)
        {
            // La bombe a pu disparaître (fin de la peinture) : son haut-parleur avec elle.
            if (bombes[i] == null || hautParleurs[i] == null)
                continue;

            // Le volume voulu : rien si on n'appuie pas ; sinon, d'autant plus faible que la bombe est vide.
            float charge = reservoir != null ? reservoir.Charge(bombes[i]) : 1f;
            float voulu = bombes[i].IsSpraying ? volume * charge : 0f;

            // Le son monte et descend en un dixième de seconde : pas de "clac" quand on appuie ou qu'on relâche.
            AudioSource hautParleur = hautParleurs[i];
            hautParleur.volume = Mathf.MoveTowards(hautParleur.volume, voulu, volume * 10f * Time.deltaTime);

            if (hautParleur.volume > 0f && !hautParleur.isPlaying)
                hautParleur.Play();   // on commence à peindre : pschhh
            else if (hautParleur.volume == 0f && hautParleur.isPlaying)
                hautParleur.Stop();   // le son est retombé à zéro : on l'arrête
        }
    }
}
