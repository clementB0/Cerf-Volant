using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// FONCTIONNALITÉ : au lancement, on se réveille. Deux paupières noires couvrent la vue,
// s'entrouvrent, se referment (on cligne un peu)... puis s'ouvrent complètement et disparaissent.
// À mettre sur un objet présent dès le début de la scène (par exemple "ChoixCerfVolant").
public class OuvertureYeux : MonoBehaviour
{
    // L'ouverture des yeux dans le temps : 0 = fermés, 1 = grands ouverts. Le temps est en secondes.
    [SerializeField] private AnimationCurve ouverture = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.8f, 0f),     // on dort encore un instant
        new Keyframe(1.2f, 0.3f),   // les yeux s'entrouvrent...
        new Keyframe(1.45f, 0f),    // ... et se referment
        new Keyframe(1.9f, 0.55f),  // un peu plus...
        new Keyframe(2.1f, 0.1f),   // ... on cligne encore
        new Keyframe(3.2f, 1f));    // puis grands ouverts

    private RectTransform paupiereHaut;
    private RectTransform paupiereBas;
    private float temps;

    private void Update()
    {
        // La tête du joueur (la caméra du casque) peut arriver une image après nous : on attend qu'elle soit là.
        if (paupiereHaut == null)
        {
            if (Camera.main == null)
                return;
            Creer(Camera.main.transform);
        }

        temps += Time.deltaTime;
        float o = Mathf.Clamp01(ouverture.Evaluate(temps));

        // Ici 1 = 1 mm. Fermés : les bords flous se chevauchent de 3 cm (pas de fente de lumière).
        // Grands ouverts : chaque bord est à 27 cm du centre, hors de la vue (on voit environ jusqu'à 21 cm).
        float bord = o * 300f - 30f;
        paupiereHaut.anchoredPosition = new Vector2(0f, bord);
        paupiereBas.anchoredPosition = new Vector2(0f, -bord);

        // Fini : on enlève les paupières.
        if (temps > ouverture.keys[ouverture.length - 1].time)
        {
            Destroy(paupiereHaut.parent.gameObject);
            enabled = false;
        }
    }

    // Crée les deux paupières, à 15 cm devant les yeux (elles suivent la tête).
    private void Creer(Transform tete)
    {
        GameObject objet = new GameObject("Paupieres");
        objet.layer = 5; // calque "UI" : l'appareil photo ne le filme pas
        objet.transform.SetParent(tete, false);
        objet.transform.localPosition = new Vector3(0f, 0f, 0.15f);
        objet.transform.localScale = Vector3.one * 0.001f; // 1 pixel du Canvas = 1 mm

        Canvas canvas = objet.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 200; // devant tout, même le panneau de tuto

        // Toujours visible, même si un objet passe entre les yeux et les paupières.
        Material materiau = new Material(Shader.Find("UI/Default"));
        materiau.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);

        // Noir plein, avec le bord de la paupière flou sur 3 cm (le bas de l'image = le bord).
        Texture2D degrade = new Texture2D(1, 100);
        degrade.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 100; y++)
            degrade.SetPixel(0, y, new Color(0f, 0f, 0f, Mathf.Clamp01(y / 5f))); // 5 % de 60 cm = 3 cm
        degrade.Apply();

        paupiereHaut = Paupiere(objet.transform, materiau, degrade, 1f);
        paupiereBas = Paupiere(objet.transform, materiau, degrade, -1f);
    }

    // Une paupière : un rectangle noir de 2 m x 60 cm, son bord flou au centre de la vue.
    // Celle du bas est la même, retournée (sens = -1).
    private static RectTransform Paupiere(Transform parent, Material materiau, Texture2D degrade, float sens)
    {
        GameObject objet = new GameObject(sens > 0f ? "Haut" : "Bas");
        objet.layer = 5;
        objet.transform.SetParent(parent, false);

        RawImage image = objet.AddComponent<RawImage>();
        image.texture = degrade;
        image.material = materiau;
        image.raycastTarget = false;

        RectTransform cadre = image.rectTransform;
        cadre.anchorMin = cadre.anchorMax = new Vector2(0.5f, 0.5f);
        cadre.pivot = new Vector2(0.5f, 0f); // le bord (le bas de l'image) est sur le point de position
        cadre.sizeDelta = new Vector2(2000f, 600f);
        cadre.localScale = new Vector3(1f, sens, 1f);
        return cadre;
    }
}
