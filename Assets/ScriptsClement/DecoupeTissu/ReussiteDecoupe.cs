using UnityEngine;

// FONCTIONNALITÉ : la réussite de la découpe.
// Quand la pièce découpée apparaît (la découpe est finie), elle saute hors du tissu en grossissant un peu
// et en se tortillant, puis retombe à sa place, avec un petit son de victoire.
// À mettre sur l'objet "PieceDecoupee". Il est caché au départ : tout se joue la première fois qu'il apparaît.
public class ReussiteDecoupe : MonoBehaviour
{
    [SerializeField] private AudioClip sonReussite;       // la petite mélodie de victoire (SonReussite.wav)
    [SerializeField] private float volume = 0.6f;
    [SerializeField] private float hauteurDuSaut = 0.08f; // la pièce monte de 8 cm
    [SerializeField] private float duree = 0.8f;          // le saut dure 0,8 seconde

    private Vector3 positionDepart;  // où la pièce retombe à la fin
    private Quaternion rotationDepart;
    private Vector3 tailleDepart;
    private float temps;             // depuis combien de temps elle saute

    // Start n'est appelé qu'une fois, la première fois que la pièce apparaît : le saut ne se joue donc qu'une fois.
    private void Start()
    {
        positionDepart = transform.position;
        rotationDepart = transform.rotation;
        tailleDepart = transform.localScale;

        // Le son reste là où la pièce est apparue, même si elle part ensuite voler.
        AudioSource.PlayClipAtPoint(sonReussite, transform.position, volume);
    }

    private void Update()
    {
        if (temps >= duree)
            return; // le saut est fini : on ne touche plus à la pièce (le vol pourra la déplacer)

        temps += Time.deltaTime;
        float t = Mathf.Min(temps / duree, 1f); // de 0 (début) à 1 (fin)

        // "saut" monte de 0 à 1 puis redescend à 0 : la forme d'une bosse.
        float saut = Mathf.Sin(t * Mathf.PI);

        // Elle monte et redescend...
        transform.position = positionDepart + Vector3.up * hauteurDuSaut * saut;

        // ... grossit de 10 % en l'air...
        transform.localScale = tailleDepart * (1f + 0.1f * saut);

        // ... et se tortille de gauche à droite (3 allers-retours), seulement en l'air.
        float tortille = Mathf.Sin(t * Mathf.PI * 6f) * 12f * saut; // en degrés
        transform.rotation = Quaternion.AngleAxis(tortille, Vector3.up) * rotationDepart;
    }
}
