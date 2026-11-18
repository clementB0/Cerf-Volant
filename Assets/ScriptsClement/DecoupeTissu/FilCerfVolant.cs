using UnityEngine;

// FONCTIONNALITÉ : dessiner le fil du bâton.
// Tant que la corde n'est pas accrochée, le fil va du bâton à son bout libre (une petite boule qu'on attrape).
// Quand on touche le cerf-volant avec ce bout, le script Fusion (de Romain) détruit le bout libre :
// le fil va alors du bâton au cerf-volant. La vraie longueur de corde est réglée par le SpringJoint du bout libre.
// À mettre sur l'objet "Bout" (le haut du bâton) : le fil part de lui.
// (Pas sur le bâton lui-même : un objet ne peut avoir qu'un seul "Renderer", et le bâton a déjà le sien pour être visible.)
public class FilCerfVolant : MonoBehaviour
{
    [SerializeField] private Transform cerfVolant;          // où arrive le fil une fois accroché (PieceDecoupee)
    [SerializeField] private Transform boutDeCorde;         // le bout libre de la corde (CordeGauche ou CordeDroite)
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
        // "!= null" : le bout libre existe encore (pas encore détruit par Fusion), donc la corde n'est pas accrochée.
        fil.SetPosition(1, boutDeCorde != null ? boutDeCorde.position : cerfVolant.position);
    }
}
