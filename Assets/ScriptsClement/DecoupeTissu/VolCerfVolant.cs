using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// FONCTIONNALITÉ : faire voler le cerf-volant quand ses DEUX cordes sont accrochées et qu'on tient ses DEUX poignées.
// À mettre sur l'objet "PieceDecoupee" (le cerf-volant découpé).
//
// Le vol est une vraie petite simulation de vent :
//  - le vent souffle avec des rafales (il n'est jamais régulier) ;
//  - il appuie sur le tissu : c'est cette poussée qui porte le cerf-volant ;
//  - le poids le tire vers le bas, et le fil le retient au bout de tes mains.
// On pilote comme un vrai cerf-volant à deux poignées :
//  - poignée droite plus basse que la gauche → il part à droite (et inversement).
public class VolCerfVolant : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable poigneeGauche;          // la poignée de gauche (son composant "attrapable" sait si on la tient)
    [SerializeField] private XRGrabInteractable poigneeDroite;          // la poignée de droite
    [SerializeField] private Transform boutCordeGauche;                 // bout libre de la corde de gauche : détruit (Fusion) = accrochée
    [SerializeField] private Transform boutCordeDroite;                 // bout libre de la corde de droite
    [SerializeField] private float longueurFil = 2.5f;                   // distance maximum mains → cerf-volant, en mètres
    [SerializeField] private Vector3 directionDuVent = Vector3.forward; // le vent souffle par là (devant toi)
    [SerializeField] private float vitesseDuVent = 6f;                  // vitesse moyenne du vent, en mètres par seconde
    [SerializeField] private float rafales = 0.4f;                      // 0.4 = le vent varie de 40 % en plus ou en moins
    [SerializeField] private float portance = 12f;                      // force avec laquelle le vent appuie sur le tissu (grand = il monte plus haut)
    [SerializeField] private float frottement = 1f;                     // l'air freine le cerf-volant (grand = il bouge moins vite)
    [SerializeField] private float angleDAttaque = 20f;                 // de combien le tissu est penché par rapport au fil, en degrés
    [SerializeField] private float angleDeVirageMax = 60f;              // jusqu'où il part sur le côté quand tu baisses une main, en degrés
    [SerializeField] private float hauteurDuSol = 0.3f;                 // il ne descend jamais plus bas que ça, en mètres

    // Ces trois valeurs sont lues par les autres scripts du vent (GonflementCerfVolant et TraitsDeVent).
    public bool EnVol { get; private set; }             // vrai pendant que le cerf-volant vole
    public Vector3 Vent { get; private set; }           // le vent en ce moment : sa direction, et sa longueur = sa vitesse
    public Vector3 VentApparent { get; private set; }   // le vent que "sent" le cerf-volant (le vent, moins son propre mouvement)

    private XRGrabInteractable prise; // le composant "attrapable" du cerf-volant lui-même
    private Vector3 vitesse;          // la vitesse du cerf-volant, en mètres par seconde

    private void Start()
    {
        prise = GetComponent<XRGrabInteractable>();
    }

    // Si le script est désactivé, le cerf-volant ne vole plus.
    private void OnDisable()
    {
        EnVol = false;
    }

    private void Update()
    {
        // Il faut que les deux cordes soient accrochées (leur bout libre a été détruit par Fusion : il vaut "null")
        // et tenir les deux poignées ("isSelected" = attrapée) pour que le cerf-volant vole.
        bool cordesAccrochees = boutCordeGauche == null && boutCordeDroite == null;
        EnVol = cordesAccrochees && poigneeGauche.isSelected && poigneeDroite.isSelected;

        // Pendant le vol, on ne peut pas attraper le cerf-volant lui-même (sinon la main et le vent se battent).
        prise.enabled = !EnVol;

        if (!EnVol)
        {
            vitesse = Vector3.zero; // il manque une main : le cerf-volant reste où il est
            return;
        }

        // Le temps écoulé depuis l'image d'avant. On le limite : si le jeu saccade, le calcul ne s'emballe pas.
        float dt = Mathf.Min(Time.deltaTime, 0.03f);

        // 1) Les deux poignées, et le milieu entre elles (c'est là que le fil est tenu).
        Vector3 gauche = poigneeGauche.transform.position;
        Vector3 droite = poigneeDroite.transform.position;
        Vector3 milieu = (gauche + droite) / 2f;

        // 2) Le vent du moment. "PerlinNoise" donne un nombre qui change doucement avec le temps :
        //    parfait pour des rafales (plus fort / moins fort) et un vent qui tourne un peu (15 degrés au plus).
        float rafale = Mathf.PerlinNoise(Time.time * 0.3f, 0f) * 2f - 1f;     // entre -1 et 1
        float deviation = (Mathf.PerlinNoise(0f, Time.time * 0.2f) * 2f - 1f) * 15f;
        Vent = Quaternion.AngleAxis(deviation, Vector3.up) * directionDuVent.normalized * vitesseDuVent * (1f + rafale * rafales);

        // 3) La direction du fil : des mains vers le cerf-volant.
        Vector3 fil = transform.position - milieu;
        fil = fil.magnitude > 0.01f ? fil.normalized : Vent.normalized;
        // Au décollage il part de la table, plus bas que tes mains : on fait comme si le fil montait un peu,
        // sinon le vent le plaquerait au sol au lieu de le soulever.
        fil.y = Mathf.Max(fil.y, 0.3f);
        fil.Normalize();

        // 4) La tête du cerf-volant pointe vers le ciel (en restant perpendiculaire au fil).
        Vector3 tete = Vector3.ProjectOnPlane(Vector3.up, fil).normalized;
        if (tete == Vector3.zero)
            tete = -Vent.normalized; // il est pile au-dessus de toi : la tête regarde vers d'où vient le vent

        // 5) Le virage : de combien la ligne entre les poignées penche (-1 = droite tout en haut, +1 = droite tout en bas).
        float penche = -(droite - gauche).normalized.y;
        float virage = penche * angleDeVirageMax; // où tu veux l'envoyer : angle sur le côté, en degrés

        //    "azimut" = où il est vraiment : son angle sur le côté par rapport au vent (positif = à droite).
        float azimut = Vector3.SignedAngle(Vector3.ProjectOnPlane(Vent, Vector3.up), Vector3.ProjectOnPlane(fil, Vector3.up), Vector3.up);

        //    Il penche sa tête vers là où tu veux l'envoyer : le vent le pousse alors de ce côté.
        //    On retire sa vitesse sur le côté (x 25) pour qu'il se redresse en arrivant, au lieu de se balancer sans fin.
        Vector3 cote = Vector3.Cross(tete, fil); // la droite du cerf-volant, vue depuis tes mains
        float inclinaison = Mathf.Clamp((virage - azimut) * 2f - Vector3.Dot(vitesse, cote) * 25f, -60f, 60f);
        tete = Quaternion.AngleAxis(-inclinaison, fil) * tete;

        // 6) Le tissu n'est pas pile en face du fil : il est un peu penché vers l'avant (l'angle d'attaque).
        //    C'est grâce à ça que le vent le pousse vers le haut, et pas seulement vers l'arrière.
        float attaque = angleDAttaque * Mathf.Deg2Rad;
        Vector3 dos = fil * Mathf.Cos(attaque) + tete * Mathf.Sin(attaque); // la direction que regarde le dos du tissu

        // 7) Les forces. Le vent que sent le cerf-volant dépend aussi de son mouvement
        //    (si tu tires sur le fil, il sent plus de vent et il monte : comme en vrai).
        VentApparent = Vent - vitesse;
        Vector3 pousseeDuVent = dos * (Vector3.Dot(VentApparent, dos) * portance); // le vent appuie sur le tissu
        Vector3 frein = VentApparent * frottement;                                 // l'air l'entraîne et le freine
        vitesse += (pousseeDuVent + frein + Physics.gravity) * dt;                 // "Physics.gravity" = son poids

        // 8) Il avance, mais le fil le retient : jamais plus loin que longueurFil des mains, jamais sous le sol.
        Vector3 position = transform.position + vitesse * dt;
        position = milieu + Vector3.ClampMagnitude(position - milieu, longueurFil);
        position.y = Mathf.Max(position.y, hauteurDuSol);

        //    Sa vraie vitesse est celle du déplacement qu'il vient de faire (le fil a pu l'arrêter ou le tirer).
        vitesse = (position - transform.position) / dt;
        transform.position = position;

        // 9) Il se tourne : le dos du tissu dans le vent, la tête penchée dans le virage. Un peu adouci pour ne pas trembler.
        Quaternion orientation = Quaternion.LookRotation(dos, tete);
        transform.rotation = Quaternion.Slerp(transform.rotation, orientation, 10f * Time.deltaTime);
    }
}
