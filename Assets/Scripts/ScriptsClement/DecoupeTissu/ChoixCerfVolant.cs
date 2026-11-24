using UnityEngine;

// FONCTIONNALITÉ : le menu de choix du cerf-volant, au début de la scène.
// Quand une miniature est choisie : petit son, toutes les miniatures disparaissent,
// et l'atelier de découpe choisi part de la miniature et vient se placer sur la table en grandissant, avec la forme choisie.
// Il arrive là où il est placé dans la scène, à la taille réglée dans la scène : pour le déplacer, déplace l'atelier lui-même.
// Plusieurs miniatures peuvent viser le même atelier : c'est la forme qui change (ex. la scène de Louis, qui n'a qu'un atelier).
// À mettre sur l'objet "ChoixCerfVolant". Les ateliers sont désactivés au départ dans la scène.
public class ChoixCerfVolant : MonoBehaviour
{
    [SerializeField] private GameObject miniatures;            // le groupe des petits cerfs-volants (on le cache d'un coup)
    [SerializeField] private float dureeAgrandissement = 0.6f;  // en secondes
    [SerializeField] private AudioClip sonChoix;                // petit son quand on choisit (SonChoix.wav)
    [SerializeField] private GameObject cutter;

    private Transform choisi;  // l'atelier choisi (vide tant qu'on n'a pas choisi)
    private float temps;       // depuis combien de temps il grandit
    private Vector3 tailleFinale; // sa taille réglée dans la scène, qu'il retrouve en fin de croissance
    private Vector3 depart;       // d'où il part : la miniature choisie
    private Vector3 arrivee;      // où il arrive : sa place réglée dans la scène

    // Appelé par MiniatureCerfVolant quand on choisit un cerf-volant.
    public void Choisir(GameObject atelier, FormeCerfVolant forme, Vector3 positionMiniature)
    {
        if (choisi != null)
            return; // déjà choisi : on ignore (ex. les deux mains qui appuient en même temps)

        miniatures.SetActive(false); // les miniatures disparaissent

        // La forme à découper, donnée AVANT d'activer l'atelier : les pointillés et la pièce sont construits à partir d'elle.
        // ("true" : on cherche aussi dans les objets désactivés, l'atelier l'est encore.)
        atelier.GetComponentInChildren<PatronPointilles>(true).ChangerForme(forme);

        // Petit son, là où était la miniature (là où est la main).
        // Sans son réglé dans l'Inspector, on choisit quand même (sinon l'erreur bloquerait tout le choix).
        if (sonChoix != null)
            AudioSource.PlayClipAtPoint(sonChoix, positionMiniature, 0.6f);

        choisi = atelier.transform;
        depart = positionMiniature;
        arrivee = choisi.position; // retenue AVANT de le déplacer vers la miniature
        choisi.position = depart;
        tailleFinale = choisi.localScale;
        choisi.localScale = tailleFinale * 0.1f; // il part tout petit...
        atelier.SetActive(true);
        if (cutter != null)
            cutter.SetActive(true);
    }

    private void Update()
    {
        if (choisi == null || temps >= dureeAgrandissement)
            return; // rien à faire : pas encore choisi, ou il a fini de grandir

        // ... grandit en douceur jusqu'à sa vraie taille (SmoothStep = démarre et finit doucement)...
        temps += Time.deltaTime;
        float avancement = Mathf.SmoothStep(0f, 1f, temps / dureeAgrandissement); // de 0 (début) à 1 (arrivé)
        choisi.localScale = tailleFinale * Mathf.Lerp(0.1f, 1f, avancement);

        // ... et vole de la miniature jusqu'à sa place sur la table.
        choisi.position = Vector3.Lerp(depart, arrivee, avancement);
    }
}
