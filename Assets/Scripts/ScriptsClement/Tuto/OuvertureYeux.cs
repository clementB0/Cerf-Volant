using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// FONCTIONNALITÉ : quand on appuie sur Y pour recommencer, on ferme les yeux (deux paupières noires),
// la scène se recharge pendant qu'ils sont fermés, puis on se réveille : les yeux s'entrouvrent,
// se referment (on cligne un peu)... puis s'ouvrent complètement.
// Remplace la fumée de rechargement (ReloadGame est désactivé dans la scène). Rien ne se passe au lancement du jeu.
public class OuvertureYeux : MonoBehaviour
{
    [SerializeField] private InputActionReference boutonY;   // le bouton qui relance la partie
    [SerializeField] private float dureeFermeture = 0.4f;    // le temps pour fermer les yeux avant le rechargement

    // L'ouverture des yeux après le rechargement : 0 = fermés, 1 = grands ouverts. Le temps est en secondes.
    [SerializeField] private AnimationCurve ouverture = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.8f, 0f),     // on dort encore un instant
        new Keyframe(1.2f, 0.3f),   // les yeux s'entrouvrent...
        new Keyframe(1.45f, 0f),    // ... et se referment
        new Keyframe(1.9f, 0.55f),  // un peu plus...
        new Keyframe(2.1f, 0.1f),   // ... on cligne encore
        new Keyframe(3.2f, 1f));    // puis grands ouverts

    private static bool enCours;    // un rechargement est déjà lancé : on ignore les autres appuis sur Y

    private RectTransform paupiereHaut;
    private RectTransform paupiereBas;
    private bool estLEffet;         // vrai seulement pour la copie qui survit au rechargement

    private void OnEnable()
    {
        if (boutonY == null)
            return;
        boutonY.action.performed += AppuiY;
        boutonY.action.Enable();
    }

    private void OnDisable()
    {
        if (boutonY != null)
            boutonY.action.performed -= AppuiY;
    }

    private void AppuiY(InputAction.CallbackContext contexte)
    {
        if (enCours)
            return;
        enCours = true;

        // Ce composant disparaît avec la scène : on crée une copie à part qui, elle, survit au rechargement.
        OuvertureYeux effet = new GameObject("Clignement des yeux").AddComponent<OuvertureYeux>();
        effet.ouverture = ouverture;
        effet.dureeFermeture = dureeFermeture;
        effet.estLEffet = true;
        DontDestroyOnLoad(effet.gameObject);
        effet.StartCoroutine(effet.Recharger(gameObject.scene.buildIndex));
    }

    private IEnumerator Recharger(int scene)
    {
        Creer();
        Application.onBeforeRender += SuivreTete;

        // 1) On ferme les yeux.
        for (float t = 0f; t < dureeFermeture; t += Time.deltaTime)
        {
            Placer(1f - t / dureeFermeture);
            yield return null;
        }
        Placer(0f);
        yield return null; // une image bien noire avant de changer de scène

        // 2) La scène se recharge pendant que les yeux sont fermés.
        yield return SceneManager.LoadSceneAsync(scene);

        // 3) On se réveille, en clignant.
        float fin = ouverture.keys[ouverture.length - 1].time;
        for (float t = 0f; t < fin; t += Time.deltaTime)
        {
            Placer(Mathf.Clamp01(ouverture.Evaluate(t)));
            yield return null;
        }

        Destroy(gameObject); // fini : on enlève les paupières
    }

    // Les paupières restent collées à la tête du joueur (la caméra du casque change avec la nouvelle scène).
    private void SuivreTete()
    {
        if (Camera.main != null)
            transform.SetPositionAndRotation(Camera.main.transform.position, Camera.main.transform.rotation);
    }

    private void LateUpdate()
    {
        if (estLEffet)
            SuivreTete();
    }

    // o = ouverture des yeux (0 = fermés, 1 = grands ouverts).
    // Ici 1 = 1 mm. Fermés : les bords flous se chevauchent de 3 cm (pas de fente de lumière).
    // Grands ouverts : chaque bord est à 27 cm du centre, hors de la vue (on voit environ jusqu'à 21 cm).
    private void Placer(float o)
    {
        float bord = o * 300f - 30f;
        paupiereHaut.anchoredPosition = new Vector2(0f, bord);
        paupiereBas.anchoredPosition = new Vector2(0f, -bord);
    }

    private void OnDestroy()
    {
        if (!estLEffet)
            return;
        Application.onBeforeRender -= SuivreTete;
        enCours = false;
    }

    // Crée les deux paupières, à 15 cm devant les yeux (cet objet suit la tête).
    private void Creer()
    {
        GameObject objet = new GameObject("Paupieres");
        objet.layer = 5; // calque "UI" : l'appareil photo ne le filme pas
        objet.transform.SetParent(transform, false);
        objet.transform.localPosition = new Vector3(0f, 0f, 0.15f);
        objet.transform.localScale = Vector3.one * 0.001f; // 1 pixel du Canvas = 1 mm
        SuivreTete();

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
        Placer(1f); // au départ les yeux sont ouverts
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
