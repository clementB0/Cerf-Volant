using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// FONCTIONNALITÉ : le panneau de tuto en haut à droite de la vue (dans le casque).
// Il montre le schéma de la phase en cours : 1 Choix, 2 Découpe, 3 Peinture, 4 Cordes, 5 Vol.
// Quand on passe à la phase suivante, le panneau change avec un petit "pop".
// À mettre sur l'objet "ChoixCerfVolant" (il est là dès le début de la scène).
public class TutoHUD : MonoBehaviour
{
    [SerializeField] private Texture2D[] panneaux;        // les 5 images, dans l'ordre des phases (Tuto1_Choix ... Tuto5_Vol)
    [SerializeField] private float distance = 0.8f;       // le panneau flotte à 80 cm devant les yeux...
    [SerializeField] private float decalageDroite = 0.27f; // ... 27 cm sur la droite...
    [SerializeField] private float decalageHaut = 0.16f;   // ... et 16 cm vers le haut
    [SerializeField] private float largeur = 0.24f;       // il fait 24 cm de large

    private Transform panneau;     // l'objet qui porte l'image (accroché à la tête)
    private RawImage image;        // l'image affichée
    private int phase = -1;        // la phase affichée (-1 = rien encore)
    private float tempsPop = 1f;   // depuis combien de temps le panneau a changé (pour le "pop")

    private void Update()
    {
        // La tête du joueur (la caméra du casque) peut arriver une image après nous : on attend qu'elle soit là.
        if (panneau == null)
        {
            if (Camera.main == null)
                return;
            Creer(Camera.main.transform);
        }

        // Quelle phase maintenant ? Si elle a changé, on change d'image et on relance le "pop".
        int nouvelle = PhaseEnCours();
        if (nouvelle != phase && nouvelle < panneaux.Length)
        {
            phase = nouvelle;
            image.texture = panneaux[phase];
            tempsPop = 0f;
        }

        // Le "pop" : le panneau grossit un peu trop puis revient à sa taille (en 0,35 seconde).
        tempsPop += Time.deltaTime;
        float t = Mathf.Clamp01(tempsPop / 0.35f);
        float taille = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
        panneau.localScale = Vector3.one * (largeur / 880f) * taille; // l'image fait 880 pixels de large
    }

    // Devine la phase en regardant ce qui est affiché dans le jeu :
    //  - pas encore de tissu : on choisit le cerf-volant ;
    //  - le tissu est là : on découpe ;
    //  - la pièce découpée est là, mais le vol est éteint : on peint ;
    //  - le vol est allumé : on accroche les cordes et on prend les poignées... puis on vole.
    private int PhaseEnCours()
    {
        // "FindFirstObjectByType" ne trouve que les objets visibles (les cachés sont ignorés).
        VolCerfVolant vol = FindFirstObjectByType<VolCerfVolant>();
        if (vol == null)
            return FindFirstObjectByType<PatronPointilles>() == null ? 0 : 1;
        if (!vol.enabled)
            return 2;
        return vol.EnVol ? 4 : 3;
    }

    // Crée le panneau et l'accroche à la tête : il suit le regard, toujours au même endroit dans la vue.
    private void Creer(Transform tete)
    {
        // Un "Canvas" en mode monde : une petite surface d'interface posée dans l'espace.
        GameObject objet = new GameObject("TutoHUD");
        objet.layer = 5; // calque "UI" : l'appareil photo ne le filme pas
        objet.transform.SetParent(tete, false);
        Vector3 place = new Vector3(decalageDroite, decalageHaut, distance);
        objet.transform.localPosition = place;
        objet.transform.localRotation = Quaternion.LookRotation(place); // tourné vers les yeux

        Canvas canvas = objet.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100; // devant les autres interfaces
        objet.GetComponent<RectTransform>().sizeDelta = new Vector2(880f, 662f); // la taille de l'image, en pixels

        // L'image du panneau, qui remplit tout le Canvas.
        GameObject fond = new GameObject("Image");
        fond.layer = 5;
        fond.transform.SetParent(objet.transform, false);
        image = fond.AddComponent<RawImage>();
        RectTransform cadre = image.rectTransform;
        cadre.anchorMin = Vector2.zero;
        cadre.anchorMax = Vector2.one;
        cadre.offsetMin = Vector2.zero;
        cadre.offsetMax = Vector2.zero;
        image.raycastTarget = false; // on ne clique pas dessus

        // Toujours visible, même si un objet (la table, une main...) passe devant.
        Material materiau = new Material(Shader.Find("UI/Default"));
        materiau.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
        image.material = materiau;

        panneau = objet.transform;
    }
}
