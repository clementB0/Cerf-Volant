using System.Collections.Generic;
using UnityEngine;

// FONCTIONNALITÉ : pendant le vol, des oiseaux (des hirondelles) traversent le ciel.
// Ils ne touchent jamais le cerf-volant : un oiseau qui arrive tout près l'évite et vient tourner doucement autour de lui (OiseauEnOrbite).
// Au 5e oiseau autour, ils s'envolent tous dans le ciel (et le cerf-volant fête ça avec la figure de son animal : voir FigureAnimal),
// puis on peut en rassembler de nouveaux.
// À mettre sur l'objet "PieceDecoupee", à côté de VolCerfVolant.
public class ObstaclesOiseaux : MonoBehaviour
{
    [SerializeField] private float tempsEntreOiseaux = 2.5f;  // un nouvel oiseau toutes les 2,5 secondes environ
    [SerializeField] private float vitesseOiseau = 1.5f;      // en mètres par seconde (lent : ils arrivent de derrière, on les voit tard)
    [SerializeField] private float distanceDepart = 6f;       // ils partent 6 m derrière le cerf-volant (donc derrière toi)
    [SerializeField] private float precision = 0.4f;          // ils visent jusqu'à 40 cm à gauche ou à droite du cerf-volant (petit = ils viennent presque tous tourner autour)
    [SerializeField] private GameObject modeleOiseau;          // le modèle 3D de l'oiseau (Asset_Hirondelle)
    [SerializeField] private float rayonFrole = 0.8f;         // arrivé à moins de 80 cm du cerf-volant, il l'évite et vient tourner autour
    [SerializeField] private int oiseauxAutourMax = 5;        // au 5e oiseau autour du cerf-volant, ils s'envolent tous
    [SerializeField] private AudioClip sonPassage;            // le "swoosh" quand un oiseau passe au-dessus de ta tête (swoosh.mp3)

    // Prévient quand le cercle est complet et que les oiseaux s'envolent (FigureAnimal s'en sert pour lancer sa figure).
    public event System.Action CercleComplet;

    private VolCerfVolant vol;
    private float prochainOiseau;   // le moment où le prochain oiseau part

    // Les oiseaux en train de voler : chacun avec sa vitesse et le moment où il disparaît.
    private List<Transform> oiseaux = new List<Transform>();
    private List<Vector3> vitesses = new List<Vector3>();
    private List<float> fins = new List<float>();

    // Les oiseaux qui tournent autour du cerf-volant (après s'en être approchés).
    private List<OiseauEnOrbite> autour = new List<OiseauEnOrbite>();

    private void Start()
    {
        vol = GetComponent<VolCerfVolant>();
    }

    private void Update()
    {
        // 1) Un nouvel oiseau de temps en temps, seulement pendant le vol (et pas pendant qu'il tombe).
        if (vol.EnVol && !vol.Tombe && Time.time >= prochainOiseau)
        {
            LancerOiseau();
            // Un peu au hasard, pour qu'on ne puisse pas deviner le rythme.
            prochainOiseau = Time.time + tempsEntreOiseaux * Random.Range(0.7f, 1.3f);
        }

        // 2) Chaque oiseau avance. On parcourt la liste à l'envers pour pouvoir en retirer sans se tromper de case.
        for (int i = oiseaux.Count - 1; i >= 0; i--)
        {
            oiseaux[i].position += vitesses[i] * Time.deltaTime;

            // Il arrive tout près du cerf-volant (seulement s'il vole) : il ne le touche pas, il l'évite et vient tourner autour
            // (OiseauEnOrbite s'occupe de lui maintenant, on l'enlève de la liste sans le détruire).
            float distance = Vector3.Distance(oiseaux[i].position, transform.position);
            if (vol.EnVol && !vol.Tombe && distance < rayonFrole)
            {
                OiseauEnOrbite oiseau = oiseaux[i].gameObject.AddComponent<OiseauEnOrbite>();
                oiseau.Rejoindre(transform, autour.Count, oiseauxAutourMax); // la place suivante sur le cercle
                autour.Add(oiseau);
                Oublier(i);

                // Le cercle est complet : ils s'envolent tous, et on recommence à zéro.
                if (autour.Count >= oiseauxAutourMax)
                {
                    EnvolerTous();
                    CercleComplet?.Invoke(); // "?." : seulement si quelqu'un écoute
                }
                continue;
            }

            // L'oiseau disparaît quand il a fini de traverser le ciel (il est passé trop loin du cerf-volant pour le rejoindre).
            if (Time.time > fins[i])
                Retirer(i);
        }
    }

    private void LancerOiseau()
    {
        // Il vole avec le vent : il arrive de derrière toi, passe au-dessus de ta tête et file vers le cerf-volant.
        Vector3 devant = Vector3.ProjectOnPlane(vol.Vent, Vector3.up).normalized;
        Vector3 cote = Vector3.Cross(Vector3.up, devant); // la droite, quand on regarde dans le vent

        // Le point qu'il vise : à la hauteur du cerf-volant, un peu à gauche ou à droite au hasard.
        // Il arrive pile à sa hauteur : il passe donc presque toujours assez près pour venir tourner autour.
        Vector3 cible = transform.position + cote * Random.Range(-precision, precision);

        // L'oiseau : une hirondelle (Asset_Hirondelle), à sa vraie taille (environ 30 cm d'une aile à l'autre).
        // Elle n'a pas de collider : elle ne gêne pas les mains, le spray ni le cutter (le contact est calculé à la main).
        GameObject oiseau = new GameObject("Oiseau");
        if (modeleOiseau != null)
        {
            Instantiate(modeleOiseau, oiseau.transform).transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // dans le modèle, la tête regarde vers l'arrière : demi-tour
        }
        else
        {
            // Pas de modèle réglé dans l'Inspector : un petit cube de 15 cm à la place (comme avant).
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(cube.GetComponent<Collider>());
            cube.transform.SetParent(oiseau.transform, false);
            cube.transform.localScale = Vector3.one * 0.15f;
        }
        oiseau.transform.position = cible - devant * distanceDepart; // il part loin derrière...
        oiseau.transform.rotation = Quaternion.LookRotation(devant);  // ... et regarde là où il va

        // Le "swoosh" : il part de l'oiseau (en 3D), juste avant qu'il passe au plus près de ta tête.
        // Comme il arrive de derrière, c'est ce son qui te prévient qu'un oiseau arrive.
        if (sonPassage != null && Camera.main != null)
        {
            AudioSource son = oiseau.AddComponent<AudioSource>();
            son.clip = sonPassage;
            son.spatialBlend = 1f;
            float quand = Vector3.Dot(Camera.main.transform.position - oiseau.transform.position, devant) / vitesseOiseau; // en secondes
            son.PlayDelayed(Mathf.Max(0f, quand - 0.3f));
        }

        oiseaux.Add(oiseau.transform);
        vitesses.Add(devant * vitesseOiseau);
        fins.Add(Time.time + 2f * distanceDepart / vitesseOiseau); // le temps d'aller d'un côté à l'autre
    }

    // Tous les oiseaux qui tournent autour du cerf-volant s'envolent dans le ciel, et le cercle est vide.
    private void EnvolerTous()
    {
        foreach (OiseauEnOrbite oiseau in autour)
            if (oiseau != null)
                oiseau.Envoler();
        autour.Clear();
    }

    private void Retirer(int i)
    {
        Destroy(oiseaux[i].gameObject);
        Oublier(i);
    }

    // On ne s'occupe plus de cet oiseau (sans le détruire).
    private void Oublier(int i)
    {
        oiseaux.RemoveAt(i);
        vitesses.RemoveAt(i);
        fins.RemoveAt(i);
    }

    private void OnDestroy()
    {
        // Les oiseaux sont des objets à part : on les supprime avec le cerf-volant.
        foreach (Transform oiseau in oiseaux)
        {
            if (oiseau != null)
                Destroy(oiseau.gameObject);
        }
        foreach (OiseauEnOrbite oiseau in autour)
        {
            if (oiseau != null)
                Destroy(oiseau.gameObject);
        }
    }
}
