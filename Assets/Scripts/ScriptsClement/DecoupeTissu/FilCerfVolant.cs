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

    [SerializeField] private float poids = 0.06f;           // le fil pend sous son poids : 0.06 = il descend de 6 % de sa longueur au milieu
    [SerializeField] private float pousseeDuVent = 0.03f;   // en vol, le vent pousse le milieu du fil (0 = le vent ne le courbe pas)

    // Une ligne courbe, c'est plein de petits bouts droits : plus il y a de points, plus elle est lisse.
    private const int Points = 12;

    private LineRenderer fil;
    private VolCerfVolant vol; // le script du vol : il nous donne le vent

    private void Start()
    {
        // Le fil est une ligne créée ici pour n'avoir rien à régler dans la scène.
        fil = gameObject.AddComponent<LineRenderer>();
        fil.sharedMaterial = materiauFil;
        fil.widthMultiplier = epaisseurFil;
        fil.positionCount = Points;

        vol = cerfVolant.GetComponent<VolCerfVolant>();
    }

    // LateUpdate passe après tous les Update : le bâton et le cerf-volant ont déjà bougé, le fil n'a pas une image de retard.
    private void LateUpdate()
    {
        Vector3 depart = transform.position;
        // "!= null" : le bout libre existe encore (pas encore détruit par Fusion), donc la corde n'est pas accrochée.
        Vector3 arrivee = boutDeCorde != null ? boutDeCorde.position : cerfVolant.position;

        // De combien le milieu du fil s'écarte de la ligne droite :
        // il pend vers le bas (plus le fil est long, plus il pend), et en vol le vent le pousse en plus.
        Vector3 creux = Vector3.down * poids * Vector3.Distance(depart, arrivee);
        if (vol != null && vol.EnVol)
            creux += vol.Vent * pousseeDuVent;

        for (int i = 0; i < Points; i++)
        {
            float t = i / (Points - 1f); // de 0 (le bâton) à 1 (le cerf-volant)

            // "4 * t * (1 - t)" vaut 0 aux deux bouts et 1 au milieu : le fil reste attaché aux bouts et se creuse entre.
            fil.SetPosition(i, Vector3.Lerp(depart, arrivee, t) + creux * (4f * t * (1f - t)));
        }
    }
}
