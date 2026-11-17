using UnityEngine;

// FONCTIONNALITÉ : dessiner le fil entre le bâton et le cerf-volant.
// À mettre sur l'objet "Bout" (le haut du bâton) : le fil part de lui.
// (Pas sur le bâton lui-même : un objet ne peut avoir qu'un seul "Renderer", et le bâton a déjà le sien pour être visible.)
public class FilCerfVolant : MonoBehaviour
{
    [SerializeField] private Transform cerfVolant;          // où arrive le fil (PieceDecoupee)
    [SerializeField] private Material materiauFil;          // couleur du fil
    [SerializeField] private float epaisseurFil = 0.003f;   // 3 mm

    private LineRenderer fil;

    private void Start()
    {
        // Le fil est une ligne à 2 points, créée ici pour n'avoir rien à régler dans la scène.
        fil = gameObject.AddComponent<LineRenderer>();
        fil.sharedMaterial = materiauFil;
        fil.widthMultiplier = epaisseurFil;
        fil.positionCount = 2;
    }

    // LateUpdate passe après tous les Update : le bâton et le cerf-volant ont déjà bougé, le fil n'a pas une image de retard.
    // shortcut: le fil est tout droit, sans courbe ; ajouter des points intermédiaires si on veut qu'il pende.
    private void LateUpdate()
    {
        fil.SetPosition(0, transform.position);
        fil.SetPosition(1, cerfVolant.position);
    }
}
