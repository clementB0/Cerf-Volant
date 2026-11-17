using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// FONCTIONNALITÉ : faire voler le cerf-volant quand on tient ses DEUX poignées (une par main).
// À mettre sur l'objet "PieceDecoupee" (le cerf-volant découpé).
//
// On pilote comme un vrai cerf-volant à deux poignées :
//  - le cerf-volant suit tes mains (le milieu des deux poignées), à longueurFil devant toi, dans le vent ;
//  - poignée droite plus basse que la gauche → il part à droite (et inversement).
public class VolCerfVolant : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable poigneeGauche;          // la poignée de gauche (son composant "attrapable" sait si on la tient)
    [SerializeField] private XRGrabInteractable poigneeDroite;          // la poignée de droite
    [SerializeField] private float longueurFil = 3f;                    // distance mains → cerf-volant, en mètres
    [SerializeField] private Vector3 directionDuVent = Vector3.forward; // le vent pousse le cerf-volant par là (devant toi)
    [SerializeField] private float hauteurDuVol = 50f;                  // angle au-dessus de l'horizon, en degrés
    [SerializeField] private float angleDeVirageMax = 60f;              // jusqu'où il part sur le côté quand tu baisses une main, en degrés
    [SerializeField] private float reactivite = 6f;                     // plus c'est grand, plus il colle à tes mains (petit = il traîne)
    [SerializeField] private float balancement = 0.15f;                 // de combien de mètres il se balance dans le vent

    private XRGrabInteractable prise; // le composant "attrapable" du cerf-volant lui-même

    private void Start()
    {
        prise = GetComponent<XRGrabInteractable>();
    }

    private void Update()
    {
        // Il faut tenir les deux poignées pour que le cerf-volant vole ("isSelected" = attrapée).
        bool deuxMains = poigneeGauche.isSelected && poigneeDroite.isSelected;

        // Pendant le vol, on ne peut pas attraper le cerf-volant lui-même (sinon la main et le vent se battent).
        prise.enabled = !deuxMains;

        if (!deuxMains)
            return; // il manque une main : le cerf-volant reste où il est

        // 1) Les deux poignées, et le milieu entre elles (c'est de là que part le vol).
        Vector3 gauche = poigneeGauche.transform.position;
        Vector3 droite = poigneeDroite.transform.position;
        Vector3 milieu = (gauche + droite) / 2f;

        // 2) Le virage : de combien la ligne entre les poignées penche (-1 = droite tout en haut, +1 = droite tout en bas).
        float penche = -(droite - gauche).normalized.y;
        float virage = penche * angleDeVirageMax;

        // 3) Où il veut être : dans le vent (tourné du virage), en hauteur, au bout du fil.
        Vector3 vent = Quaternion.AngleAxis(virage, Vector3.up) * directionDuVent.normalized;
        float angle = hauteurDuVol * Mathf.Deg2Rad;
        Vector3 direction = vent * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle);
        Vector3 cible = milieu + direction * longueurFil;

        // 4) Le vent n'est jamais régulier : petit balancement gauche-droite et haut-bas.
        Vector3 cote = Vector3.Cross(Vector3.up, directionDuVent).normalized; // la droite, quand on regarde dans le vent
        cible += cote * Mathf.Sin(Time.time * 1.3f) * balancement;
        cible += Vector3.up * Mathf.Sin(Time.time * 0.7f) * balancement * 0.5f;

        // 5) Il rejoint ce point rapidement, avec juste un peu de retard pour garder l'effet "vrai cerf-volant".
        transform.position = Vector3.Lerp(transform.position, cible, reactivite * Time.deltaTime);

        // 6) Il se tourne vers tes mains (le dessus du tissu face à toi), la tête vers le ciel, penché dans le virage.
        Quaternion orientation = Quaternion.LookRotation(transform.position - milieu, Vector3.up);
        orientation *= Quaternion.Euler(0f, 0f, virage * 0.5f);
        transform.rotation = Quaternion.Slerp(transform.rotation, orientation, reactivite * Time.deltaTime);
    }
}
