using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// FONCTIONNALITÉ : dessiner la forme du cerf-volant en traits blancs (pointillés) sur le tissu.
// Quand la pointe du cutter passe sur un trait, il s'allonge et rejoint ses voisins : les pointillés deviennent une ligne.
// À mettre sur l'objet "Tissu" (un Quad couché, de 1 m x 1 m quand son échelle vaut 1).
public class PatronPointilles : MonoBehaviour
{
    // Les coins de la forme, en mètres, mesurés depuis le centre du tissu.
    // x = gauche (-) / droite (+), y = vers soi (-) / loin de soi (+).
    // Par défaut : un papillon de 80 cm de large (relevé sur le dessin, sans les antennes). Modifiable dans l'Inspector.
    // Attention :
    //  - donner les coins dans le sens des aiguilles d'une montre (vu du dessus), sinon la PieceDecoupee est invisible ;
    //  - depuis le centre du tissu (0, 0), on doit "voir" tout le contour (voir PieceDecoupee).
    [SerializeField] private Vector2[] coins =
    {
        new Vector2(0f, 0.13f),         // haut du corps (entre les antennes)

        // Aile droite du haut
        new Vector2(0.022f, 0.12f),
        new Vector2(0.082f, 0.18f),
        new Vector2(0.162f, 0.225f),
        new Vector2(0.262f, 0.252f),    // haut de l'aile
        new Vector2(0.342f, 0.235f),
        new Vector2(0.392f, 0.19f),
        new Vector2(0.4f, 0.14f),       // bout de l'aile
        new Vector2(0.377f, 0.08f),
        new Vector2(0.322f, 0.025f),
        new Vector2(0.247f, 0f),        // creux entre les deux ailes droites

        // Aile droite du bas
        new Vector2(0.312f, -0.05f),
        new Vector2(0.344f, -0.095f),
        new Vector2(0.337f, -0.145f),
        new Vector2(0.292f, -0.185f),
        new Vector2(0.202f, -0.202f),   // bas de l'aile
        new Vector2(0.102f, -0.195f),
        new Vector2(0.037f, -0.15f),

        new Vector2(0f, -0.14f),        // bas du corps

        // Aile gauche du bas (le miroir de la droite : x devient -x)
        new Vector2(-0.037f, -0.15f),
        new Vector2(-0.102f, -0.195f),
        new Vector2(-0.202f, -0.202f),
        new Vector2(-0.292f, -0.185f),
        new Vector2(-0.337f, -0.145f),
        new Vector2(-0.344f, -0.095f),
        new Vector2(-0.312f, -0.05f),
        new Vector2(-0.247f, 0f),       // creux entre les deux ailes gauches

        // Aile gauche du haut
        new Vector2(-0.322f, 0.025f),
        new Vector2(-0.377f, 0.08f),
        new Vector2(-0.4f, 0.14f),
        new Vector2(-0.392f, 0.19f),
        new Vector2(-0.342f, 0.235f),
        new Vector2(-0.262f, 0.252f),
        new Vector2(-0.162f, 0.225f),
        new Vector2(-0.082f, 0.18f),
        new Vector2(-0.022f, 0.12f),
    };

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
    public Vector2[] Coins => coins;

    private void Start()
    {
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
