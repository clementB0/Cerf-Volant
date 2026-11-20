using System.Collections.Generic;
using UnityEngine;

// FONCTIONNALITÉ : pendant le vol, des oiseaux (pour l'instant de petits cubes) traversent le ciel.
// Il faut les esquiver : si un oiseau touche le cerf-volant, il tombe (puis le vent le relance).
// À mettre sur l'objet "PieceDecoupee", à côté de VolCerfVolant.
public class ObstaclesOiseaux : MonoBehaviour
{
    [SerializeField] private float tempsEntreOiseaux = 2.5f;  // un nouvel oiseau toutes les 2,5 secondes environ
    [SerializeField] private float vitesseOiseau = 1.5f;      // en mètres par seconde (lent : ils arrivent de derrière, on les voit tard)
    [SerializeField] private float distanceDepart = 6f;       // ils partent 6 m derrière le cerf-volant (donc derrière toi)
    [SerializeField] private float precision = 0.4f;          // ils visent jusqu'à 40 cm à gauche ou à droite du cerf-volant (petit = plus dur)
    [SerializeField] private float tailleOiseau = 0.15f;      // un cube de 15 cm
    [SerializeField] private float rayonTouche = 0.3f;        // plus près que ça du cerf-volant = touché

    private VolCerfVolant vol;
    private float prochainOiseau;   // le moment où le prochain oiseau part

    // Les oiseaux en train de voler : chacun avec sa vitesse et le moment où il disparaît.
    private List<Transform> oiseaux = new List<Transform>();
    private List<Vector3> vitesses = new List<Vector3>();
    private List<float> fins = new List<float>();

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

            // Touché ? (seulement s'il vole encore : un cerf-volant déjà au sol ne retombe pas)
            bool touche = vol.EnVol && !vol.Tombe && Vector3.Distance(oiseaux[i].position, transform.position) < rayonTouche;
            if (touche)
                vol.Tomber();

            // L'oiseau disparaît s'il a touché, ou quand il a fini de traverser le ciel.
            if (touche || Time.time > fins[i])
                Retirer(i);
        }
    }

    private void LancerOiseau()
    {
        // Il vole avec le vent : il arrive de derrière toi, passe au-dessus de ta tête et file vers le cerf-volant.
        Vector3 devant = Vector3.ProjectOnPlane(vol.Vent, Vector3.up).normalized;
        Vector3 cote = Vector3.Cross(Vector3.up, devant); // la droite, quand on regarde dans le vent

        // Le point qu'il vise : à la hauteur du cerf-volant, un peu à gauche ou à droite au hasard.
        // Il arrive pile à sa hauteur : pour l'esquiver, il faut emmener le cerf-volant sur le côté.
        Vector3 cible = transform.position + cote * Random.Range(-precision, precision);

        // Pour l'instant, un oiseau est un petit cube.
        GameObject oiseau = GameObject.CreatePrimitive(PrimitiveType.Cube);
        oiseau.name = "Oiseau";
        oiseau.transform.localScale = Vector3.one * tailleOiseau;
        oiseau.transform.position = cible - devant * distanceDepart; // il part loin derrière...
        oiseau.transform.rotation = Quaternion.LookRotation(devant);  // ... regarde là où il va...
        Destroy(oiseau.GetComponent<Collider>()); // ... et ne gêne pas les mains, le spray ni le cutter (le contact est calculé à la main)

        oiseaux.Add(oiseau.transform);
        vitesses.Add(devant * vitesseOiseau);
        fins.Add(Time.time + 2f * distanceDepart / vitesseOiseau); // le temps d'aller d'un côté à l'autre
    }

    private void Retirer(int i)
    {
        Destroy(oiseaux[i].gameObject);
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
    }
}
