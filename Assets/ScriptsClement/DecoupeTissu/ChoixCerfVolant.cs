using UnityEngine;

// FONCTIONNALITÉ : le menu de choix du cerf-volant, au début de la scène.
// Quand une miniature est choisie : toutes les miniatures disparaissent,
// et l'atelier de découpe choisi apparaît devant toi en grandissant, avec la forme choisie.
// Plusieurs miniatures peuvent viser le même atelier : c'est la forme qui change (ex. la scène de Louis, qui n'a qu'un atelier).
// À mettre sur l'objet "ChoixCerfVolant". Les ateliers sont désactivés au départ dans la scène.
public class ChoixCerfVolant : MonoBehaviour
{
    [SerializeField] private GameObject miniatures;            // le groupe des petits cerfs-volants (on le cache d'un coup)
    [SerializeField] private Vector3 positionDuChoisi;          // où se met l'atelier choisi (0, 0, 0 = juste devant toi au départ)
    [SerializeField] private float dureeAgrandissement = 0.6f;  // en secondes

    private Transform choisi;  // l'atelier choisi (vide tant qu'on n'a pas choisi)
    private float temps;       // depuis combien de temps il grandit
    private Vector3 tailleFinale; // sa taille réglée dans la scène, qu'il retrouve en fin de croissance

    // Appelé par MiniatureCerfVolant quand on choisit un cerf-volant.
    public void Choisir(GameObject atelier, FormeCerfVolant forme)
    {
        if (choisi != null)
            return; // déjà choisi : on ignore (ex. les deux mains qui appuient en même temps)

        miniatures.SetActive(false); // les miniatures disparaissent

        // La forme à découper, donnée AVANT d'activer l'atelier : les pointillés et la pièce sont construits à partir d'elle.
        // ("true" : on cherche aussi dans les objets désactivés, l'atelier l'est encore.)
        atelier.GetComponentInChildren<PatronPointilles>(true).ChangerForme(forme);

        choisi = atelier.transform;
        choisi.position = positionDuChoisi;
        tailleFinale = choisi.localScale;
        choisi.localScale = tailleFinale * 0.1f; // il part tout petit...
        atelier.SetActive(true);
    }

    private void Update()
    {
        if (choisi == null || temps >= dureeAgrandissement)
            return; // rien à faire : pas encore choisi, ou il a fini de grandir

        // ... et grandit en douceur jusqu'à sa vraie taille (SmoothStep = démarre et finit doucement).
        temps += Time.deltaTime;
        choisi.localScale = tailleFinale * Mathf.SmoothStep(0.1f, 1f, temps / dureeAgrandissement);
    }
}
