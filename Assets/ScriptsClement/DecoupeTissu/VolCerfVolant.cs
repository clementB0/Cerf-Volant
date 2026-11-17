using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// FONCTIONNALITÉ : faire voler le cerf-volant au bout de son fil quand on tient le bâton À DEUX MAINS.
// À mettre sur l'objet "PieceDecoupee" (le cerf-volant découpé).
//
// On pilote comme une vraie barre de cerf-volant :
//  - le cerf-volant suit le bâton (tes mains), à longueurFil devant toi, dans le vent ;
//  - main droite plus basse que la gauche → il part à droite (et inversement).
public class VolCerfVolant : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable baton;                  // le bâton : son composant "attrapable" sait quelles mains le tiennent
    [SerializeField] private float longueurFil = 3f;                    // distance bâton → cerf-volant, en mètres
    [SerializeField] private Vector3 directionDuVent = Vector3.forward; // le vent pousse le cerf-volant par là (devant toi)
    [SerializeField] private float hauteurDuVol = 50f;                  // angle au-dessus de l'horizon, en degrés
    [SerializeField] private float angleDeVirageMax = 60f;              // jusqu'où il part sur le côté quand tu penches le bâton, en degrés
    [SerializeField] private float reactivite = 6f;                     // plus c'est grand, plus il colle à tes mains (petit = il traîne)
    [SerializeField] private float balancement = 0.15f;                 // de combien de mètres il se balance dans le vent

    private XRGrabInteractable prise; // le composant "attrapable" du cerf-volant lui-même

    private void Start()
    {
        prise = GetComponent<XRGrabInteractable>();
        // (les deux mains sur la barre, chacune à sa place : c'est BarreDeuxMains, sur le bâton)
    }

    private void Update()
    {
        // Il faut deux mains sur le bâton pour que le cerf-volant vole.
        bool deuxMains = baton.interactorsSelecting.Count >= 2;

        // Pendant le vol, on ne peut pas attraper le cerf-volant lui-même (sinon la main et le vent se battent).
        prise.enabled = !deuxMains;

        if (!deuxMains)
            return; // moins de deux mains : le cerf-volant reste où il est

        // 1) Les deux mains qui tiennent le bâton, et laquelle est à droite.
        Vector3 main1 = baton.interactorsSelecting[0].transform.position;
        Vector3 main2 = baton.interactorsSelecting[1].transform.position;
        Vector3 droite = Vector3.Cross(Vector3.up, directionDuVent).normalized; // la droite, quand on regarde dans le vent
        Vector3 deGaucheADroite = Vector3.Dot(main2 - main1, droite) >= 0f ? main2 - main1 : main1 - main2;

        // 2) Le virage : de combien le bâton penche (-1 = main droite tout en haut, +1 = main droite tout en bas).
        float penche = -deGaucheADroite.normalized.y;
        float virage = penche * angleDeVirageMax;

        // 3) Où il veut être : dans le vent (tourné du virage), en hauteur, au bout du fil.
        Vector3 vent = Quaternion.AngleAxis(virage, Vector3.up) * directionDuVent.normalized;
        float angle = hauteurDuVol * Mathf.Deg2Rad;
        Vector3 direction = vent * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle);
        Vector3 cible = baton.transform.position + direction * longueurFil;

        // 4) Le vent n'est jamais régulier : petit balancement gauche-droite et haut-bas.
        cible += droite * Mathf.Sin(Time.time * 1.3f) * balancement;
        cible += Vector3.up * Mathf.Sin(Time.time * 0.7f) * balancement * 0.5f;

        // 5) Il rejoint ce point rapidement, avec juste un peu de retard pour garder l'effet "vrai cerf-volant".
        transform.position = Vector3.Lerp(transform.position, cible, reactivite * Time.deltaTime);

        // 6) Il se tourne vers le bâton (le dessus du tissu face à toi), la tête vers le ciel, penché dans le virage.
        Quaternion orientation = Quaternion.LookRotation(transform.position - baton.transform.position, Vector3.up);
        orientation *= Quaternion.Euler(0f, 0f, virage * 0.5f);
        transform.rotation = Quaternion.Slerp(transform.rotation, orientation, reactivite * Time.deltaTime);
    }
}
