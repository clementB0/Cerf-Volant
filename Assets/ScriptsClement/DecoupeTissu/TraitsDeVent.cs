using UnityEngine;

// FONCTIONNALITÉ : montrer le vent. De longs traits blancs arrivent de loin et rentrent dans le cerf-volant.
// Ils suivent le vrai vent du vol : même direction, même vitesse (ils accélèrent pendant les rafales).
// À mettre sur l'objet "PieceDecoupee", à côté de VolCerfVolant (c'est lui qui connaît le vent).
public class TraitsDeVent : MonoBehaviour
{
    [SerializeField] private float distance = 2.5f;         // les traits naissent à cette distance avant le cerf-volant, en mètres
    [SerializeField] private float largeurZone = 0.8f;      // largeur du carré dans lequel ils naissent, en mètres
    [SerializeField] private float traitsParSeconde = 8f;
    [SerializeField] private float longueur = 0.6f;         // longueur d'un trait, en mètres
    [SerializeField] private float epaisseur = 0.008f;      // 8 mm
    [SerializeField] private Color couleur = new Color(1f, 1f, 1f, 0.5f); // blanc à moitié transparent

    private VolCerfVolant vol;          // le script du vol : il nous donne le vent
    private ParticleSystem particules;  // le "lanceur de traits" (chaque trait est une particule très étirée)

    private void Start()
    {
        vol = GetComponent<VolCerfVolant>();

        // Le lanceur est un objet à part, créé ici pour n'avoir rien à régler dans la scène.
        particules = new GameObject("TraitsDeVent").AddComponent<ParticleSystem>();

        // Réglages de base. "World" = un trait déjà parti continue tout droit, même si le lanceur bouge.
        ParticleSystem.MainModule reglages = particules.main;
        reglages.simulationSpace = ParticleSystemSimulationSpace.World;
        reglages.startSize = epaisseur;
        reglages.startColor = couleur;

        ParticleSystem.EmissionModule emission = particules.emission;
        emission.rateOverTime = traitsParSeconde;

        // Les traits naissent n'importe où dans un carré plat, et partent droit devant le lanceur.
        ParticleSystem.ShapeModule zone = particules.shape;
        zone.shapeType = ParticleSystemShapeType.Box;
        zone.scale = new Vector3(largeurZone, largeurZone, 0f);

        // Chaque trait apparaît et disparaît en douceur (transparent → visible → transparent).
        Gradient fondu = new Gradient();
        fondu.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
        ParticleSystem.ColorOverLifetimeModule couleurDansLeTemps = particules.colorOverLifetime;
        couleurDansLeTemps.enabled = true;
        couleurDansLeTemps.color = fondu;

        // "Stretch" étire la particule dans le sens où elle avance : un petit point devient un long trait.
        ParticleSystemRenderer rendu = particules.GetComponent<ParticleSystemRenderer>();
        rendu.renderMode = ParticleSystemRenderMode.Stretch;
        rendu.velocityScale = 0f;
        rendu.lengthScale = longueur / epaisseur;
        // "Sprites/Default" est un matériau tout simple, toujours présent dans Unity, qui gère la transparence.
        rendu.material = new Material(Shader.Find("Sprites/Default"));
    }

    // LateUpdate passe après tous les Update : le vol a déjà calculé le vent et déplacé le cerf-volant.
    private void LateUpdate()
    {
        // Pas de vol, pas de nouveaux traits (ceux déjà partis finissent leur course).
        ParticleSystem.EmissionModule emission = particules.emission;
        emission.enabled = vol.EnVol;
        if (!vol.EnVol)
            return;

        // "Max" évite une division par zéro si on règle un vent nul.
        float vitesse = Mathf.Max(vol.Vent.magnitude, 0.1f);
        Vector3 sens = vol.Vent / vitesse; // la direction du vent

        // Le lanceur se place avant le cerf-volant, du côté d'où vient le vent, et regarde vers lui.
        particules.transform.SetPositionAndRotation(transform.position - sens * distance, Quaternion.LookRotation(sens));

        // Les traits vont à la vitesse du vent, et vivent juste le temps d'arriver au cerf-volant : ils "rentrent" dedans.
        ParticleSystem.MainModule reglages = particules.main;
        reglages.startSpeed = vitesse;
        reglages.startLifetime = distance / vitesse;
    }

    private void OnDestroy()
    {
        // Le lanceur est un objet à part : on le supprime avec le cerf-volant.
        if (particules != null)
            Destroy(particules.gameObject);
    }
}
