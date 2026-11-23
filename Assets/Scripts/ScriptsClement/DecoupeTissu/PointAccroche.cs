using UnityEngine;

// FONCTIONNALITÉ : un seul point d'accroche, au milieu du cerf-volant, où il faut amener les deux cordes.
// Le point est une petite boule visible : c'est exactement la zone qui marche.
// Toucher le cerf-volant ailleurs ne fait rien (le script CerfVolant de Romain n'est plus sur toute la pièce, seulement sur ce point).
// Le point n'existe que pendant l'emboîtement : quand les deux cordes sont accrochées, il disparaît.
// À mettre sur l'objet "PieceDecoupee". (Le milieu = le centre de la pièce, là où les fils arrivent déjà.)
public class PointAccroche : MonoBehaviour
{
    [SerializeField] private Transform[] boutsDesCordes;      // les bouts libres des cordes (CordeGauche, CordeDroite)
    [SerializeField] private float rayon = 0.03f;             // la zone d'accroche : une boule de 3 cm de rayon (en vrais mètres)
    [SerializeField] private Color couleur = new Color(1f, 0.85f, 0.2f, 0.8f); // jaune, un peu transparent

    private GameObject point;

    private void Start()
    {
        // Une boule : sa forme sert à la fois à la voir et à détecter les cordes.
        point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        point.name = "PointAccroche";
        point.transform.SetParent(transform, false);
        point.transform.localPosition = Vector3.zero; // le milieu du cerf-volant
        point.GetComponent<Collider>().isTrigger = true; // ne bloque rien : il détecte seulement le bout de la corde
        point.AddComponent<CerfVolant>();                // le script Fusion (sur les cordes) cherche ce script pour s'accrocher

        Material matiere = new Material(Shader.Find("Sprites/Default")); // gère la transparence, visible des deux côtés
        matiere.color = couleur;
        point.GetComponent<Renderer>().sharedMaterial = matiere;
        point.SetActive(false);
    }

    private void LateUpdate()
    {
        // Le point est là tant qu'au moins une corde reste à accrocher.
        bool emboitement = false;
        foreach (Transform bout in boutsDesCordes)
            if (bout != null && bout.gameObject.activeInHierarchy)
                emboitement = true;
        point.SetActive(emboitement);
        if (!emboitement)
            return;

        // Garder une vraie taille de 2 × rayon, même si la pièce est agrandie ou réduite.
        Vector3 echelle = transform.lossyScale;
        point.transform.localScale = new Vector3(2f * rayon / echelle.x, 2f * rayon / echelle.y, 2f * rayon / echelle.z);
    }
}
