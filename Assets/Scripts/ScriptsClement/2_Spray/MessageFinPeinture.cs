using TMPro;
using UnityEngine;

// FONCTIONNALITÉ : quand le cerf-volant est peint à 80 %, "Appuie quand tu as fini !" s'affiche au-dessus du bouton rouge,
// avec une flèche dessous qui monte et descend en montrant le bouton. Le message disparaît quand la peinture est finie.
// À mettre sur l'objet "GestionTransitionCutToTag" (toujours actif, à côté de BoutonAuSpray).
public class MessageFinPeinture : MonoBehaviour
{
    [SerializeField] private Transform bouton;                            // le bouton rouge "Passer au montage"
    [SerializeField, Range(0f, 100f)] private float seuil = 80f;          // à partir de 80 % peint
    [SerializeField] private Color couleur = new Color(1f, 0.85f, 0.2f);  // jaune vif, comme les autres indications
    [SerializeField] private TMP_FontAsset police;                        // la police (North Kids SDF) ; vide = la police par défaut

    private PaintableSurface peinture;  // le cerf-volant qu'on peint (celui de l'atelier choisi)
    private Transform message;          // le texte + la flèche, au-dessus du bouton
    private Transform fleche;

    private void Update()
    {
        // Le cerf-volant peint : le seul PaintableSurface actif (les autres ateliers sont cachés).
        if (peinture == null || !peinture.isActiveAndEnabled)
            peinture = FindFirstObjectByType<PaintableSurface>();

        bool montrer = peinture != null && !peinture.Terminee && peinture.Progression >= seuil
                       && bouton.gameObject.activeInHierarchy && Camera.main != null;
        if (message == null)
        {
            if (!montrer)
                return;
            Creer();
        }

        message.gameObject.SetActive(montrer);
        if (!montrer)
            return;

        // Au-dessus du bouton, tourné vers le joueur (seulement autour de l'axe vertical, pour rester droit).
        message.position = bouton.position + Vector3.up * 0.12f;
        Vector3 versJoueur = Vector3.ProjectOnPlane(message.position - Camera.main.transform.position, Vector3.up);
        if (versJoueur != Vector3.zero)
            message.rotation = Quaternion.LookRotation(versJoueur);

        // La flèche monte et descend de 3 cm, un peu plus d'une fois par seconde.
        fleche.localPosition = new Vector3(0f, (Mathf.Sin(Time.time * 7f) + 1f) * 0.015f, 0f);
    }

    private void Creer()
    {
        message = new GameObject("MessageFinPeinture").transform;
        message.gameObject.layer = 5; // calque "UI" : l'appareil photo ne le filme pas

        TextMeshPro texte = new GameObject("Texte").AddComponent<TextMeshPro>();
        texte.gameObject.layer = 5;
        texte.transform.SetParent(message, false);
        texte.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        texte.rectTransform.sizeDelta = new Vector2(0.6f, 0.1f);
        texte.text = "Appuie quand tu as fini !";
        texte.fontSize = 0.4f; // environ 4 cm de haut
        if (police != null)
            texte.font = police;
        texte.fontStyle = FontStyles.Bold;
        texte.alignment = TextAlignmentOptions.Center;
        texte.color = couleur;

        // La flèche vers le bas (celle des bombes, retournée et un peu plus grande).
        GameObject objet = new GameObject("Fleche");
        objet.layer = 5;
        fleche = objet.transform;
        fleche.SetParent(message, false);
        fleche.localRotation = Quaternion.Euler(0f, 0f, 180f);
        fleche.localScale = Vector3.one * 1.5f;
        objet.AddComponent<MeshFilter>().sharedMesh = ReservoirBombe.MaillageFleche();
        objet.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = couleur };
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
