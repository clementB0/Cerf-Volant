using UnityEngine;

// FONCTIONNALITÉ : pendant la découpe, le tissu fait un petit saut et tourne (ou se retourne)
// pour amener devant le joueur le morceau de pointillés à découper. À la fin, il se remet à l'endroit,
// le cerf-volant face au joueur (sa tête pointée loin de lui, comme un dessin posé devant soi).
// C'est PatronPointilles qui lui demande de bouger, et qui attend qu'il ait fini pour continuer.
// À mettre sur l'objet "Tissu", avec PatronPointilles.
public class TourneTissu : MonoBehaviour
{
    [SerializeField] private float duree = 1f;          // le mouvement dure 1 seconde
    [SerializeField] private float hauteurSaut = 0.25f; // il monte de 25 cm (assez pour se retourner sans toucher la table)

    public bool EnMouvement => temps < duree; // vrai pendant le saut : on ne peut pas découper

    private float temps = 999f;          // depuis combien de temps il bouge (très grand = il ne bouge pas)
    private bool retourne;               // vrai quand il est à l'envers
    private Quaternion rotationDeBase;   // sa rotation de départ, à l'endroit (dans son parent)
    private Quaternion rotationDepart;
    private Quaternion rotationArrivee;
    private Vector3 positionDepart;      // sa place (dans son parent), où il retombe après le saut

    private void Awake()
    {
        rotationDeBase = transform.localRotation;
    }

    // Tourne pour que "pointLocal" (un point du tissu, ex. le milieu des pointillés à découper) soit du côté du joueur.
    // "seRetourner" = il se retourne aussi, comme une crêpe.
    public void Tourner(Vector3 pointLocal, bool seRetourner)
    {
        if (seRetourner)
            retourne = !retourne;
        Partir(Orientation(pointLocal, VersLeJoueur()));
    }

    // Le mouvement final : à l'endroit, la tête du cerf-volant (le haut de la forme, y) pointée loin du joueur.
    // Il doit finir à l'endroit : la pièce découpée sera posée comme lui, et elle doit être à l'endroit pour la peinture.
    public void MettreFaceAuJoueur()
    {
        retourne = false;
        Partir(Orientation(Vector3.up, -VersLeJoueur()));
    }

    // La direction du joueur vue du tissu, à plat (sans monter ni descendre).
    private Vector3 VersLeJoueur()
    {
        // shortcut: sans caméra principale on suppose le joueur côté -z du monde ; sinon on suit la tête (Camera.main).
        Vector3 joueur = Camera.main != null ? Camera.main.transform.position : transform.position + Vector3.back;
        return Vector3.ProjectOnPlane(joueur - transform.position, Vector3.up);
    }

    // Calcule la rotation (dans le parent) qui amène "pointLocal" dans la direction "direction", en tournant à plat.
    private Quaternion Orientation(Vector3 pointLocal, Vector3 direction)
    {
        Quaternion parent = transform.parent.rotation;

        // Sa rotation dans le monde, à l'endroit ou retourné (un demi-tour autour de son axe y).
        Quaternion monde = parent * rotationDeBase;
        if (retourne)
            monde *= Quaternion.Euler(0f, 180f, 0f);

        // Où se trouverait le point avec cette rotation, et de combien il faut tourner (autour de la verticale) pour l'amener dans la direction voulue.
        Vector3 actuel = Vector3.ProjectOnPlane(monde * pointLocal, Vector3.up);
        float angle = Vector3.SignedAngle(actuel, direction, Vector3.up);

        // On repasse dans le repère du parent (c'est là que Unity range la rotation du tissu).
        return Quaternion.Inverse(parent) * Quaternion.AngleAxis(angle, Vector3.up) * monde;
    }

    private void Partir(Quaternion arrivee)
    {
        rotationDepart = transform.localRotation;
        rotationArrivee = arrivee;
        positionDepart = transform.localPosition;
        temps = 0f; // c'est parti
    }

    private void Update()
    {
        if (!EnMouvement)
            return;

        temps += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, temps / duree); // de 0 à 1, en démarrant et finissant doucement

        // Il tourne petit à petit de sa position de départ à celle d'arrivée...
        transform.localRotation = Quaternion.Slerp(rotationDepart, rotationArrivee, t);

        // ... en sautant : il monte puis redescend pile à sa place (Sin vaut 0 au début et à la fin, 1 au milieu).
        // "InverseTransformVector" convertit les 25 cm du monde dans le repère du parent (qui peut être plus petit ou plus grand).
        Vector3 saut = Vector3.up * Mathf.Sin(t * Mathf.PI) * hauteurSaut;
        transform.localPosition = positionDepart + transform.parent.InverseTransformVector(saut);
    }
}
