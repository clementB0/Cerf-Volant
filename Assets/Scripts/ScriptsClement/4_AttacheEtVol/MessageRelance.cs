using TMPro;
using UnityEngine;

// FONCTIONNALITÉ : après une chute (un oiseau a touché le cerf-volant), "Monte" s'affiche devant les yeux,
// avec une flèche de chaque côté qui monte et descend : il faut lever les deux poignées d'un coup pour le faire repartir.
// Le message disparaît dès que le cerf-volant repart (voir VolCerfVolant : AttendRelance).
// À mettre sur "PieceDecoupee", avec VolCerfVolant.
[RequireComponent(typeof(VolCerfVolant))]
public class MessageRelance : MonoBehaviour
{
    [SerializeField] private Color couleur = new Color(1f, 0.85f, 0.2f); // jaune vif, comme les autres indications
    [SerializeField] private float distance = 0.7f;                      // le message flotte à 70 cm devant les yeux

    private VolCerfVolant vol;
    private Transform message;                              // "Monte" + les deux flèches (accroché à la tête)
    private readonly Transform[] fleches = new Transform[2]; // à gauche, puis à droite du mot

    private void Start()
    {
        vol = GetComponent<VolCerfVolant>();
    }

    private void Update()
    {
        // On le montre quand le cerf-volant attend d'être relancé et qu'on tient les deux poignées.
        bool montrer = vol.AttendRelance && vol.EnVol;
        if (message == null)
        {
            if (!montrer || Camera.main == null)
                return;
            Creer(Camera.main.transform);
        }

        message.gameObject.SetActive(montrer);
        if (!montrer)
            return;

        // Les flèches montent et descendent de 4 cm, un peu plus d'une fois par seconde : le geste à faire.
        float bouge = (Mathf.Sin(Time.time * 7f) + 1f) * 0.02f;
        for (int i = 0; i < 2; i++)
            fleches[i].localPosition = new Vector3(i == 0 ? -0.14f : 0.14f, -0.02f + bouge, 0f);
    }

    // Crée le message, accroché à la tête : il suit le regard, toujours au même endroit dans la vue.
    private void Creer(Transform tete)
    {
        message = new GameObject("MessageRelance").transform;
        message.gameObject.layer = 5; // calque "UI" : l'appareil photo ne le filme pas
        message.SetParent(tete, false);
        message.localPosition = new Vector3(0f, -0.05f, distance);

        TextMeshPro texte = new GameObject("Texte").AddComponent<TextMeshPro>();
        texte.gameObject.layer = 5;
        texte.transform.SetParent(message, false);
        texte.rectTransform.sizeDelta = new Vector2(0.3f, 0.1f);
        texte.text = "Monte";
        texte.fontSize = 0.6f; // environ 6 cm de haut
        texte.fontStyle = FontStyles.Bold;
        texte.alignment = TextAlignmentOptions.Center;
        texte.color = couleur;

        // Deux flèches vers le haut (la même que pour les bombes, un peu plus grande).
        Mesh forme = ReservoirBombe.MaillageFleche();
        Material matiere = new Material(Shader.Find("Sprites/Default")) { color = couleur };
        for (int i = 0; i < 2; i++)
        {
            GameObject fleche = new GameObject(i == 0 ? "FlecheGauche" : "FlecheDroite");
            fleche.layer = 5;
            fleche.transform.SetParent(message, false);
            fleche.transform.localScale = Vector3.one * 1.5f;
            fleche.AddComponent<MeshFilter>().sharedMesh = forme;
            fleche.AddComponent<MeshRenderer>().sharedMaterial = matiere;
            fleches[i] = fleche.transform;
        }
    }

    private void OnDisable()
    {
        if (message != null)
            message.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (message != null)
            Destroy(message.gameObject);
    }
}
