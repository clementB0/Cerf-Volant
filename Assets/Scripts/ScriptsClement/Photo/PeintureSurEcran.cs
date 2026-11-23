using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// FONCTIONNALITÉ : si tu te sprayes de la peinture dans la figure, de grosses taches de cette couleur
// couvrent ta vue (comme l'encre du calamar dans un jeu de course), puis s'en vont en 2 secondes en coulant un peu.
// À mettre sur l'objet "ChoixCerfVolant" (il est là dès le début de la scène). Il trouve tout seul les bombes de peinture.
public class PeintureSurEcran : MonoBehaviour
{
    [SerializeField] private float dureeDisparition = 2f; // les taches s'en vont en 2 secondes
    [SerializeField] private int nombreDeTaches = 6;

    private SprayInput[] bombes;          // les bombes de peinture de la scène
    private Transform ecran;              // l'interface collée devant les yeux, où on dessine les taches
    private Material materiauDevant;      // toujours dessiné par-dessus le décor
    private Texture2D forme;              // la forme d'une tache (dessinée par le script)
    private List<RawImage> taches = new List<RawImage>();
    private Color couleur;                // la couleur des taches en ce moment
    private float depuisLeDernierJet = 99f; // depuis combien de temps on n'a plus de peinture dans la figure

    private void Update()
    {
        Transform tete = Camera.main != null ? Camera.main.transform : null;
        if (tete == null)
            return;
        if (ecran == null)
        {
            CreerEcran(tete);
            // "Include" : on trouve aussi les bombes encore cachées (elles apparaissent après la découpe).
            bombes = FindObjectsByType<SprayInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        // Une bombe en train de peindre vise-t-elle ta tête ?
        foreach (SprayInput bombe in bombes)
        {
            if (bombe == null || !bombe.IsSpraying)
                continue;
            SprayConeVFX jet = bombe.GetComponent<SprayConeVFX>();
            Transform buse = jet.SprayOrigin;
            Vector3 versLaTete = tete.position - buse.position;
            // Dans la portée du jet, et dans son cône (un peu plus large : la tête n'est pas un point).
            bool touche = versLaTete.magnitude < jet.SprayRange + 0.1f && Vector3.Angle(buse.forward, versLaTete) < jet.ConeHalfAngle + 10f;
            if (touche)
                Asperger(bombe.GetComponent<SprayColor>().PaintColor);
        }

        // Les taches restent tant qu'on se sprayait, puis pâlissent et coulent vers le bas, jusqu'à disparaître.
        depuisLeDernierJet += Time.deltaTime;
        float visible = 1f - Mathf.Clamp01(depuisLeDernierJet / dureeDisparition);
        foreach (RawImage tache in taches)
        {
            tache.color = new Color(couleur.r, couleur.g, couleur.b, visible);
            tache.rectTransform.anchoredPosition += Vector2.down * 40f * Time.deltaTime * (1f - visible); // elles coulent en s'effaçant
        }
        if (visible <= 0f && taches.Count > 0)
            Effacer();
    }

    private void Asperger(Color nouvelleCouleur)
    {
        // Nouvelle couleur (ou plus de taches) : on en dessine de nouvelles, à des places au hasard.
        if (taches.Count == 0 || nouvelleCouleur != couleur)
        {
            Effacer();
            couleur = nouvelleCouleur;
            for (int i = 0; i < nombreDeTaches; i++)
            {
                RawImage tache = new GameObject("Tache").AddComponent<RawImage>();
                tache.gameObject.layer = 5; // calque "UI" : l'appareil photo ne le filme pas
                tache.transform.SetParent(ecran, false);
                tache.texture = forme;
                tache.raycastTarget = false;
                float taille = Random.Range(250f, 480f);
                tache.rectTransform.sizeDelta = new Vector2(taille, taille);
                tache.rectTransform.anchoredPosition = Random.insideUnitCircle * 300f; // surtout au milieu de la vue
                tache.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                tache.material = materiauDevant;
                taches.Add(tache);
            }
        }
        depuisLeDernierJet = 0f; // tant qu'on se sprayait, les taches restent bien opaques
    }

    private void Effacer()
    {
        foreach (RawImage tache in taches)
            Destroy(tache.gameObject);
        taches.Clear();
    }

    // L'écran de taches : une interface collée à 25 cm devant les yeux, qui couvre toute la vue.
    private void CreerEcran(Transform tete)
    {
        GameObject objet = new GameObject("PeintureSurEcran");
        objet.layer = 5;
        objet.transform.SetParent(tete, false);
        objet.transform.localPosition = new Vector3(0f, 0f, 0.25f);
        Canvas canvas = objet.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 200; // par-dessus le viseur et le tuto
        objet.GetComponent<RectTransform>().sizeDelta = new Vector2(1000f, 1000f);
        objet.transform.localScale = Vector3.one * 0.0006f; // 1000 pixels = 60 cm à 25 cm des yeux : toute la vue
        ecran = objet.transform;

        materiauDevant = new Material(Shader.Find("UI/Default"));
        materiauDevant.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);

        forme = DessinerTache(256);
    }

    // Dessine la forme d'une tache : un gros rond aux bords flous, entouré de gouttelettes. Blanc : la couleur est ajoutée après.
    private Texture2D DessinerTache(int taille)
    {
        // Les ronds qui forment la tache : (x, y, rayon), de 0 à 1.
        List<Vector3> ronds = new List<Vector3> { new Vector3(0.5f, 0.5f, 0.3f) };
        for (int i = 0; i < 9; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float loin = Random.Range(0.28f, 0.44f);
            ronds.Add(new Vector3(0.5f + Mathf.Cos(angle) * loin, 0.5f + Mathf.Sin(angle) * loin, Random.Range(0.03f, 0.09f)));
        }

        Texture2D texture = new Texture2D(taille, taille, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[taille * taille];
        for (int y = 0; y < taille; y++)
        {
            for (int x = 0; x < taille; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / taille, (y + 0.5f) / taille);
                float opacite = 0f;
                foreach (Vector3 rond in ronds)
                {
                    float dedans = (rond.z - Vector2.Distance(p, new Vector2(rond.x, rond.y))) / 0.01f; // bord flou de 1 %
                    opacite = Mathf.Max(opacite, Mathf.Clamp01(dedans));
                }
                pixels[y * taille + x] = new Color(1f, 1f, 1f, opacite);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
