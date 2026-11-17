using UnityEngine;

// FONCTIONNALITÉ : la pièce de tissu découpée, qui a la forme du patron.
// À mettre sur l'objet "PieceDecoupee" (désactivé au départ).
// PatronPointilles l'active quand tous les traits sont reliés : à ce moment-là elle se construit
// et prend la place du Tissu.
// Pour l'attraper, on n'écrit rien : c'est le composant "XR Grab Interactable" du template (sur le même objet).
[RequireComponent(typeof(MeshFilter), typeof(BoxCollider))]
public class PieceDecoupee : MonoBehaviour
{
    [SerializeField] private PatronPointilles patron; // on lui prend la forme et la position

    private void Start()
    {
        // On se met exactement à la place du tissu, pour que la pièce apparaisse sous la ligne tracée.
        transform.SetPositionAndRotation(patron.transform.position, patron.transform.rotation);

        // La forme en 3D, fabriquée par le fichier Forme du patron (même forme que les pointillés).
        Mesh maillage = patron.Forme.CreerMaillage();
        GetComponent<MeshFilter>().mesh = maillage;

        // La zone où la main peut attraper la pièce (utilisée par le XR Grab Interactable du template).
        // On la cale sur la taille de la forme, avec 1 cm d'épaisseur sinon elle est trop fine pour être attrapée.
        // shortcut: c'est un rectangle autour de la forme, donc on attrape aussi dans les coins vides ; passer à un MeshCollider si ça gêne.
        BoxCollider zoneDePrise = GetComponent<BoxCollider>();
        zoneDePrise.center = maillage.bounds.center;
        zoneDePrise.size = new Vector3(maillage.bounds.size.x, maillage.bounds.size.y, 0.01f);
    }
}
