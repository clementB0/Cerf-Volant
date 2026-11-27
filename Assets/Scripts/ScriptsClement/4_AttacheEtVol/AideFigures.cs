using TMPro;
using UnityEngine;

// FONCTIONNALITÉ : l'aide des figures. Au début du vol, un petit panneau devant les yeux explique
// comment faire les figures : B + joystick gauche à droite = salto, en bas = toupie, à gauche = rebond (voir FigureCerfVolant).
// Il disparaît dès la première figure (on a compris), ou au bout de 20 secondes de vol.
// À mettre sur "PieceDecoupee", avec FigureCerfVolant et VolCerfVolant.
[RequireComponent(typeof(FigureCerfVolant), typeof(VolCerfVolant))]
public class AideFigures : MonoBehaviour
{
    [SerializeField] private Color couleur = new Color(1f, 0.85f, 0.2f); // jaune vif, comme les autres indications
    [SerializeField] private float distance = 0.7f;                      // à 70 cm devant les yeux
    [SerializeField] private float dureeMax = 20f;                       // au plus 20 secondes de vol
    [SerializeField] private TMP_FontAsset police;                       // la police (North Kids SDF) ; vide = la police par défaut

    private FigureCerfVolant figures;
    private VolCerfVolant vol;
    private Transform panneau;
    private float tempsVol;   // combien de temps de vol avec l'aide affichée
    private bool fini;        // vrai quand l'aide ne doit plus revenir

    private void Start()
    {
        figures = GetComponent<FigureCerfVolant>();
        vol = GetComponent<VolCerfVolant>();
    }

    private void Update()
    {
        if (!fini && (figures.FiguresFaites > 0 || tempsVol > dureeMax))
            fini = true;

        // Pendant le vol, pas pendant une chute ni quand "Releve vite !" est affiché.
        bool montrer = !fini && vol.EnVol && !vol.Tombe && !vol.AttendRelance && Camera.main != null;
        if (montrer)
            tempsVol += Time.deltaTime;
        if (panneau == null)
        {
            if (!montrer)
                return;
            Creer(Camera.main.transform);
        }
        panneau.gameObject.SetActive(montrer);
    }

    // Accroché à la tête, un peu sous le regard (pour ne pas cacher le cerf-volant).
    private void Creer(Transform tete)
    {
        TextMeshPro texte = new GameObject("AideFigures").AddComponent<TextMeshPro>();
        panneau = texte.transform;
        panneau.gameObject.layer = 5; // calque "UI" : l'appareil photo ne le filme pas
        panneau.SetParent(tete, false);
        panneau.localPosition = new Vector3(0f, -0.2f, distance);
        texte.rectTransform.sizeDelta = new Vector2(0.5f, 0.25f);
        texte.text = "Fais des figures !\n<size=70%>B + joystick a droite : salto\nB + joystick en bas : toupie\nB + joystick a gauche : rebond</size>";
        texte.fontSize = 0.4f; // environ 4 cm de haut pour le titre
        if (police != null)
            texte.font = police;
        texte.fontStyle = FontStyles.Bold;
        texte.alignment = TextAlignmentOptions.Center;
        texte.color = couleur;
    }

    private void OnDisable()
    {
        if (panneau != null)
            panneau.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (panneau != null)
            Destroy(panneau.gameObject);
    }
}
