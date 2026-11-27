using UnityEngine;

// FONCTIONNALITÉ : une petite traînée blanche, très légère, derrière le cerf-volant pendant qu'il vole.
// Elle part fine et presque transparente, et s'efface en un peu plus d'une demi-seconde.
// À mettre sur l'objet "PieceDecoupee", à côté de VolCerfVolant (c'est lui qui sait s'il vole).
public class TraineeCerfVolant : MonoBehaviour
{
    [SerializeField] private float duree = 0.6f;     // la traînée s'efface en 0,6 s (plus grand = plus longue)
    [SerializeField] private float largeur = 0.05f;  // 5 cm au départ, puis elle s'affine jusqu'à 0
    [SerializeField] private Color couleur = new Color(1f, 1f, 1f, 0.15f); // blanc presque transparent (0,15)

    private VolCerfVolant vol;
    private TrailRenderer trainee;

    private void Start()
    {
        vol = GetComponent<VolCerfVolant>();

        // Un objet ne peut avoir qu'un seul "Renderer" (le cerf-volant a déjà le sien) : la traînée est sur un enfant.
        GameObject porteur = new GameObject("Trainee");
        porteur.transform.SetParent(transform, false);
        trainee = porteur.AddComponent<TrailRenderer>();

        trainee.time = duree;
        trainee.widthMultiplier = largeur;
        trainee.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f); // large au cerf-volant, pointue au bout
        trainee.minVertexDistance = 0.02f;                           // un point tous les 2 cm : une courbe bien lisse
        trainee.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trainee.receiveShadows = false;

        // Elle s'efface vers le bout (transparente au bout).
        Gradient fondu = new Gradient();
        fondu.SetKeys(
            new[] { new GradientColorKey(couleur, 0f), new GradientColorKey(couleur, 1f) },
            new[] { new GradientAlphaKey(couleur.a, 0f), new GradientAlphaKey(0f, 1f) });
        trainee.colorGradient = fondu;

        // Le même matériau tout simple que TraitsDeVent (il gère la transparence).
        trainee.material = new Material(Shader.Find("Sprites/Default"));
        trainee.emitting = false;
    }

    // LateUpdate : après que le vol a déplacé le cerf-volant.
    private void LateUpdate()
    {
        bool enVol = vol.EnVol && !vol.Tombe;

        // Au décollage, on efface l'ancienne traînée (sinon un trait relierait l'ancienne place à la nouvelle).
        if (enVol && !trainee.emitting)
            trainee.Clear();
        trainee.emitting = enVol;
    }
}
