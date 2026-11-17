using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// FONCTIONNALITÉ : dessiner la forme du cerf-volant en traits blancs (pointillés) sur le tissu.
// Quand la pointe du cutter passe sur un trait, il s'allonge et rejoint ses voisins : les pointillés deviennent une ligne.
// À mettre sur l'objet "Tissu" (un Quad couché, de 1 m x 1 m quand son échelle vaut 1).
public class PatronPointilles : MonoBehaviour
{
    // La forme à découper (un fichier "Forme" : Papillon, Raie, Dragon... dans ScenesClement/Formes).
    // Pour changer de cerf-volant, glisser une autre forme ici dans l'Inspector.
    [SerializeField] private FormeCerfVolant forme;

    [SerializeField] private float ecartEntreTraits = 0.03f; // un trait tous les 3 cm (le trait fait la moitié : 1,5 cm)
    [SerializeField] private float epaisseurTrait = 0.004f;  // 4 mm
    [SerializeField] private Material materiauTrait;         // couleur des traits (blanc)
    [SerializeField] private float rayonContact = 0.015f;    // un trait se relie si la pointe du cutter passe à moins de 1,5 cm

    // Ce qui se passe quand tous les traits sont reliés. Se règle dans l'Inspector (liste "Quand Tout Est Relie").
    // Dans la scène : affiche la PieceDecoupee et cache le Tissu.
    [SerializeField] private UnityEvent quandToutEstRelie;

    private List<Transform> traitsPasRelies = new List<Transform>(); // les traits que le cutter n'a pas encore touchés
    private bool toutEstRelie;                                       // devient vrai une seule fois, à la fin

    // La forme, en lecture seule, pour que PieceDecoupee ait exactement la même.
    public FormeCerfVolant Forme => forme;

    private void Start()
    {
        Vector2[] coins = forme.Coins;

        // On relie chaque coin au suivant. Le "% coins.Length" fait revenir le dernier coin au premier : la forme est fermée.
        for (int i = 0; i < coins.Length; i++)
        {
            Vector2 depart = coins[i];
            Vector2 arrivee = coins[(i + 1) % coins.Length];

            // Combien de traits tiennent sur ce côté, et la place exacte de chacun.
            int nombreDeTraits = Mathf.CeilToInt(Vector2.Distance(depart, arrivee) / ecartEntreTraits);
            Vector2 pas = (arrivee - depart) / nombreDeTraits;

            for (int j = 0; j < nombreDeTraits; j++)
            {
                // Le trait est centré au milieu de sa case : relié (longueur x 2), il touche pile ses deux voisins.
                Vector2 centre = depart + pas * (j + 0.5f);
                CreerTrait(centre, pas);
            }
        }
    }

    // Crée un petit trait blanc (un cube aplati) posé sur le tissu, dans la direction du côté.
    private void CreerTrait(Vector2 centre, Vector2 pas)
    {
        GameObject trait = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trait.name = "Trait";
        Destroy(trait.GetComponent<Collider>()); // pas de collision : le trait ne doit pas bloquer les mains ni les rayons
        trait.GetComponent<Renderer>().sharedMaterial = materiauTrait;

        // On le range dans le tissu : il bouge avec lui. z = 0 = à la surface du tissu.
        trait.transform.SetParent(transform, false);
        trait.transform.localPosition = new Vector3(centre.x, centre.y, 0f);

        // On le tourne dans la direction du côté (Atan2 donne l'angle de la direction).
        float angle = Mathf.Atan2(pas.y, pas.x) * Mathf.Rad2Deg;
        trait.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        // x = longueur (la moitié de la case, pour laisser un trou), y et z = épaisseur.
        trait.transform.localScale = new Vector3(pas.magnitude / 2f, epaisseurTrait, epaisseurTrait);

        traitsPasRelies.Add(trait.transform);
    }

    // Appelé par TraceurCutter avec la position de la pointe du cutter : relie les traits tout près d'elle.
    public void RelierTraitsPres(Vector3 positionPointe)
    {
        // On parcourt la liste à l'envers : retirer un élément ne fait alors sauter aucune case.
        for (int i = traitsPasRelies.Count - 1; i >= 0; i--)
        {
            Transform trait = traitsPasRelies[i];
            if (Vector3.Distance(trait.position, positionPointe) < rayonContact)
            {
                // Le trait double de longueur : il bouche les deux demi-trous et touche ses voisins.
                Vector3 taille = trait.localScale;
                trait.localScale = new Vector3(taille.x * 2f, taille.y, taille.z);
                traitsPasRelies.RemoveAt(i);
            }
        }

        // Plus aucun trait à relier : la découpe est finie, on prévient la scène (une seule fois).
        if (traitsPasRelies.Count == 0 && !toutEstRelie)
        {
            toutEstRelie = true;
            quandToutEstRelie.Invoke();
        }
    }
}
