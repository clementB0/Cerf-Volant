using UnityEngine;

// FONCTIONNALITÉ : le menu de choix du cerf-volant, au début de la scène.
// Quand une miniature est choisie : toutes les miniatures disparaissent,
// et l'atelier de découpe choisi apparaît devant toi en grandissant.
// À mettre sur l'objet "ChoixCerfVolant". Les ateliers sont désactivés au départ dans la scène.
public class ChoixCerfVolant : MonoBehaviour
{
    [SerializeField] private GameObject miniatures;            // le groupe des petits cerfs-volants (on le cache d'un coup)
    [SerializeField] private Vector3 positionDuChoisi;          // où se met l'atelier choisi (0, 0, 0 = juste devant toi au départ)
    [SerializeField] private float dureeAgrandissement = 0.6f;  // en secondes

    private Transform choisi;  // l'atelier choisi (vide tant qu'on n'a pas choisi)
    private float temps;       // depuis combien de temps il grandit

    // Appelé par MiniatureCerfVolant quand on choisit un cerf-volant.
    public void Choisir(GameObject atelier)
    {
        if (choisi != null)
            return; // déjà choisi : on ignore (ex. les deux mains qui appuient en même temps)

        miniatures.SetActive(false); // les miniatures disparaissent

        choisi = atelier.transform;
        choisi.position = positionDuChoisi;
        choisi.localScale = Vector3.one * 0.1f; // il part tout petit...
        atelier.SetActive(true);
    }

    private void Update()
    {
        if (choisi == null || temps >= dureeAgrandissement)
            return; // rien à faire : pas encore choisi, ou il a fini de grandir

        // ... et grandit en douceur jusqu'à sa vraie taille (SmoothStep = démarre et finit doucement).
        temps += Time.deltaTime;
        choisi.localScale = Vector3.one * Mathf.SmoothStep(0.1f, 1f, temps / dureeAgrandissement);
    }
}
