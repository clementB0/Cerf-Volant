using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// FONCTIONNALITÉ : une aide pour accrocher les cordes au cerf-volant (l'emboîtement).
//  - Après 8 secondes, chaque pièce à assembler (le bout libre de chaque corde) s'entoure d'un halo lumineux,
//    puis le point d'emboîtement correspondant s'allume aussi.
//  - Quand le joueur saisit une pièce, seul l'emplacement de destination de cette pièce reste éclairé.
//  - Une pièce accrochée (son bout libre disparaît, script Fusion) : ses halos disparaissent.
// À mettre sur l'objet "PieceDecoupee" (le cerf-volant : c'est là que les cordes s'accrochent).
public class AideEmboitement : MonoBehaviour
{
    [SerializeField] private Transform[] pieces;                 // les bouts libres des cordes (gauche, puis droite)
    [SerializeField] private float attente = 8f;                 // l'aide apparaît après 8 secondes
    [SerializeField] private float decalageDestinations = 0.6f;  // les points d'emboîtement s'allument 0,6 seconde après les pièces
    [SerializeField] private float tailleHaloPiece = 0.05f;      // le halo autour d'une pièce (5 cm)
    [SerializeField] private float tailleHaloDestination = 0.07f; // le halo sur un point d'emboîtement (7 cm)
    [SerializeField] private Color couleur = new Color(1f, 0.95f, 0.6f); // un jaune très clair, lumineux

    private GameObject[] halosPieces;
    private GameObject[] halosDestinations;
    private Material lumiere;     // transparent, pour que les halos laissent voir ce qu'ils entourent
    private float attenteEcoulee; // depuis combien de temps la phase a commencé

    private void Start()
    {
        lumiere = new Material(Shader.Find("Sprites/Default"));
        halosPieces = new GameObject[pieces.Length];
        halosDestinations = new GameObject[pieces.Length];
        for (int i = 0; i < pieces.Length; i++)
        {
            halosPieces[i] = Halo("AideEmboitement_Piece", tailleHaloPiece);
            halosDestinations[i] = Halo("AideEmboitement_Destination", tailleHaloDestination);
        }
    }

    // Une boule lumineuse transparente, sans collision.
    private GameObject Halo(string nom, float taille)
    {
        GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        halo.name = nom;
        Destroy(halo.GetComponent<Collider>());
        halo.GetComponent<Renderer>().sharedMaterial = lumiere;
        halo.transform.localScale = Vector3.one * taille;
        halo.SetActive(false);
        return halo;
    }

    private void Update()
    {
        // La phase d'emboîtement a lieu quand au moins une pièce est là, pas encore accrochée.
        bool phaseEnCours = false;
        int tenue = -1; // la pièce que le joueur tient (-1 = aucune)
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] == null || !pieces[i].gameObject.activeInHierarchy)
                continue;
            phaseEnCours = true;
            XRGrabInteractable prise = pieces[i].GetComponent<XRGrabInteractable>();
            if (prise != null && prise.isSelected)
                tenue = i;
        }
        attenteEcoulee = phaseEnCours ? attenteEcoulee + Time.deltaTime : 0f;

        // Les halos pulsent doucement.
        lumiere.color = new Color(couleur.r, couleur.g, couleur.b, 0.25f + 0.2f * Mathf.Sin(Time.time * 4f));

        bool piecesAllumees = attenteEcoulee >= attente;
        bool destinationsAllumees = attenteEcoulee >= attente + decalageDestinations;
        for (int i = 0; i < pieces.Length; i++)
        {
            bool presente = pieces[i] != null && pieces[i].gameObject.activeInHierarchy;

            // La pièce s'illumine (sauf quand on en tient une : alors seule la destination reste).
            bool haloPiece = presente && piecesAllumees && tenue < 0;
            halosPieces[i].SetActive(haloPiece);
            if (haloPiece)
                halosPieces[i].transform.position = pieces[i].position;

            // Son point d'emboîtement : allumé après les pièces ; si on tient une pièce, seulement le sien.
            bool haloDestination = presente && (tenue == i || (tenue < 0 && destinationsAllumees));
            halosDestinations[i].SetActive(haloDestination);
            if (haloDestination)
                halosDestinations[i].transform.position = transform.TransformPoint(PointAccroche.PositionLocale(i));
        }
    }

    private void OnDisable()
    {
        if (halosPieces == null)
            return;
        foreach (GameObject halo in halosPieces) if (halo != null) halo.SetActive(false);
        foreach (GameObject halo in halosDestinations) if (halo != null) halo.SetActive(false);
    }

    private void OnDestroy()
    {
        if (halosPieces == null)
            return;
        foreach (GameObject halo in halosPieces) if (halo != null) Destroy(halo);
        foreach (GameObject halo in halosDestinations) if (halo != null) Destroy(halo);
    }
}
