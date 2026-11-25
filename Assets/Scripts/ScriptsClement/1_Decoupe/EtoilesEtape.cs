using UnityEngine;

// FONCTIONNALITÉ : à chaque étape de découpe finie, des petites étoiles jaillissent de l'endroit du dernier trait découpé,
// avec un petit son de clochettes.
// À mettre sur l'objet "Tissu", avec PatronPointilles (qui prévient quand une étape est finie).
[RequireComponent(typeof(PatronPointilles))]
public class EtoilesEtape : MonoBehaviour
{
    [SerializeField] private AudioClip sonEtoiles;              // SonEtoiles.wav
    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;
    [SerializeField] private int nombre = 16;                   // combien d'étoiles à chaque fois
    [SerializeField] private float vitesse = 0.5f;              // à quelle vitesse elles s'envolent (mètres par seconde)
    [SerializeField] private float taille = 0.03f;              // leur taille (3 cm)
    [SerializeField] private float hauteur = 0.05f;             // elles jaillissent 5 cm au-dessus du dernier trait
    [SerializeField] private Color couleur1 = new Color(1f, 0.85f, 0.2f); // jaune doré
    [SerializeField] private Color couleur2 = Color.white;      // chaque étoile a une couleur entre les deux

    private ParticleSystem etoiles;
    private AudioSource hautParleur;

    private void Start()
    {
        // Rangé à côté du tissu (pas dedans) : quand le tissu disparaît à la fin, les étoiles et le son continuent.
        GameObject objet = new GameObject("EtoilesEtape");
        objet.transform.SetParent(transform.parent, false);

        hautParleur = objet.AddComponent<AudioSource>();
        hautParleur.playOnAwake = false;
        hautParleur.spatialBlend = 1f; // le son vient de l'endroit des étoiles

        etoiles = CreerEtoiles(objet, vitesse, taille, couleur1, couleur2);
        GetComponent<PatronPointilles>().EtapeFinie += Lancer;
    }

    // Fabrique les étoiles sur "objet" et les renvoie : on les fait jaillir ensuite avec "Emit(combien)".
    // "public static" : un autre script peut s'en servir sans avoir d'EtoilesEtape (FigureCerfVolant, pour la figure en vol).
    public static ParticleSystem CreerEtoiles(GameObject objet, float vitesse, float taille, Color couleur1, Color couleur2)
    {
        ParticleSystem etoiles = objet.AddComponent<ParticleSystem>();
        etoiles.Stop(); // on les lance nous-mêmes (Emit)
        ParticleSystem.MainModule reglages = etoiles.main;
        reglages.simulationSpace = ParticleSystemSimulationSpace.World;
        reglages.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
        reglages.startSpeed = new ParticleSystem.MinMaxCurve(vitesse * 0.5f, vitesse);
        reglages.startSize = new ParticleSystem.MinMaxCurve(taille * 0.6f, taille);
        reglages.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        reglages.startColor = new ParticleSystem.MinMaxGradient(couleur1, couleur2);
        reglages.gravityModifier = 0.15f; // elles retombent un tout petit peu
        reglages.playOnAwake = false;

        ParticleSystem.EmissionModule flot = etoiles.emission;
        flot.enabled = false;

        // Elles partent dans toutes les directions.
        ParticleSystem.ShapeModule forme = etoiles.shape;
        forme.shapeType = ParticleSystemShapeType.Sphere;
        forme.radius = 0.01f;

        // Elles ralentissent (comme dans l'air)...
        ParticleSystem.LimitVelocityOverLifetimeModule frein = etoiles.limitVelocityOverLifetime;
        frein.enabled = true;
        frein.dampen = 0.08f;
        frein.limit = 0f;

        // ... tournent sur elles-mêmes...
        ParticleSystem.RotationOverLifetimeModule tourne = etoiles.rotationOverLifetime;
        tourne.enabled = true;
        tourne.z = new ParticleSystem.MinMaxCurve(-4f, 4f);

        // ... et grossissent vite puis rapetissent jusqu'à disparaître.
        ParticleSystem.SizeOverLifetimeModule grandit = etoiles.sizeOverLifetime;
        grandit.enabled = true;
        grandit.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0f)));

        // Leur image : une étoile blanche (la couleur de chaque particule la teinte).
        Material matiere = new Material(Shader.Find("Sprites/Default")); // gère la transparence et la couleur des particules
        matiere.mainTexture = ImageEtoile(64);
        objet.GetComponent<ParticleSystemRenderer>().sharedMaterial = matiere;
        return etoiles;
    }

    private void OnDestroy()
    {
        PatronPointilles patron = GetComponent<PatronPointilles>();
        if (patron != null)
            patron.EtapeFinie -= Lancer;
    }

    // Une étape est finie : les étoiles jaillissent de cet endroit, avec le son.
    private void Lancer(Vector3 endroit)
    {
        etoiles.transform.position = endroit + Vector3.up * hauteur;
        etoiles.Emit(nombre);
        if (sonEtoiles != null)
            hautParleur.PlayOneShot(sonEtoiles, volume);
    }

    // Dessine une étoile à 5 branches, blanche sur fond transparent, avec un bord doux.
    private static Texture2D ImageEtoile(int cote)
    {
        Texture2D image = new Texture2D(cote, cote, TextureFormat.RGBA32, false);
        image.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[cote * cote];
        float rayonPointes = cote * 0.48f, rayonCreux = rayonPointes * 0.45f; // le bout des branches, et le creux entre deux
        for (int y = 0; y < cote; y++)
        {
            for (int x = 0; x < cote; x++)
            {
                float dx = x + 0.5f - cote / 2f, dy = y + 0.5f - cote / 2f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                // L'angle, ramené à une branche : 0 = sur une pointe, 0.5 = au milieu du creux.
                float angle = Mathf.Atan2(dx, dy) / (2f * Mathf.PI) * 5f;
                float dansBranche = Mathf.Abs(angle - Mathf.Round(angle)) * 2f; // de 0 (pointe) à 1 (creux)
                float bord = Mathf.Lerp(rayonPointes, rayonCreux, dansBranche);
                byte alpha = (byte)(Mathf.Clamp01(bord - distance + 0.5f) * 255f); // 1 pixel de bord doux
                pixels[y * cote + x] = new Color32(255, 255, 255, alpha);
            }
        }
        image.SetPixels32(pixels);
        image.Apply();
        return image;
    }
}
