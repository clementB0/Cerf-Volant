using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// FONCTIONNALITÉ : un appareil photo posé sur la table, qu'on peut prendre à tout moment (il ne disparaît jamais).
//  - On l'attrape : la vue se change en viseur (tout noir, sauf un carré zoomé au milieu = la future photo).
//  - On appuie sur la gâchette : clic, flash, et la photo (une pellicule) sort par le dessous en 1 seconde,
//    puis fait "pouf"... et réapparaît dans le livre posé à côté ("re-pouf").
//  - S'il tombe par terre, il revient tout seul sur la table.
// À mettre sur un objet vide posé sur la table, à l'endroit où l'appareil doit être rangé.
public class AppareilPhoto : MonoBehaviour
{
    [SerializeField] private LivrePhotos livre;            // le livre où les photos arrivent
    [SerializeField] private ParticleSystem fumeePrefab;   // le nuage de fumée (VFX/APPARITION)
    [SerializeField] private AudioClip sonPouf;            // le "pouf !" (SonFumee.wav)
    [SerializeField] private AudioClip sonDeclencheur;     // le "clic" de la photo (SonPrise.wav)
    [SerializeField] private float zoom = 30f;             // le champ de vision du viseur, en degrés (petit = zoom fort)
    [SerializeField] private float tailleCarre = 0.12f;    // la taille du carré du viseur (à 30 cm des yeux)

    private XRGrabInteractable prise;   // permet de l'attraper (et sait si on le tient)
    private Rigidbody corps;
    private Camera objectif;            // une deuxième caméra qui filme la vue zoomée
    private RenderTexture image;        // ce que filme l'objectif
    private GameObject viseur;          // le cache noir avec le carré, devant les yeux
    private Image flash;                // un éclair blanc quand on prend la photo
    private Vector3 placeDepart;        // sa place sur la table
    private Quaternion rotationDepart;
    private bool photoEnCours;          // une pellicule est en train de sortir : on attend
    private float tempsParTerre;        // depuis combien de temps il traîne par terre

    private void Start()
    {
        placeDepart = transform.position;
        rotationDepart = transform.rotation;

        // 1) Le dessin de l'appareil, en formes simples (style cartoon : couleurs plates).
        Bloc(PrimitiveType.Cube, new Vector3(0.13f, 0.09f, 0.055f), Vector3.zero, Quaternion.identity, new Color(0.37f, 0.78f, 0.95f), true); // le boîtier
        Quaternion versLAvant = Quaternion.Euler(90f, 0f, 0f); // un cylindre est debout : on le couche vers l'avant
        Bloc(PrimitiveType.Cylinder, new Vector3(0.06f, 0.015f, 0.06f), new Vector3(0f, -0.005f, 0.035f), versLAvant, new Color(0.95f, 0.95f, 0.97f), false); // la bague de l'objectif
        Bloc(PrimitiveType.Cylinder, new Vector3(0.038f, 0.017f, 0.038f), new Vector3(0f, -0.005f, 0.037f), versLAvant, new Color(0.12f, 0.15f, 0.28f), false); // la lentille
        Bloc(PrimitiveType.Cube, new Vector3(0.03f, 0.018f, 0.006f), new Vector3(-0.042f, 0.028f, 0.029f), Quaternion.identity, Color.white, false); // le flash
        Bloc(PrimitiveType.Cylinder, new Vector3(0.018f, 0.006f, 0.018f), new Vector3(0.045f, 0.048f, 0f), Quaternion.identity, new Color(1f, 0.42f, 0.36f), false); // le déclencheur

        // 2) On peut l'attraper. (Le boîtier a gardé son collider : c'est lui qu'on attrape.)
        corps = gameObject.AddComponent<Rigidbody>();
        prise = gameObject.AddComponent<XRGrabInteractable>();
        prise.activated.AddListener(QuandOnAppuie); // "activated" = la gâchette, pendant qu'on le tient

        // 3) L'objectif : une caméra qui filme dans une image carrée, éteinte tant qu'on ne vise pas.
        image = new RenderTexture(1024, 1024, 24);
        objectif = new GameObject("Objectif").AddComponent<Camera>();
        objectif.targetTexture = image;
        objectif.fieldOfView = zoom;
        objectif.stereoTargetEye = StereoTargetEyeMask.None; // une image normale, pas une vue de casque
        objectif.nearClipPlane = 0.25f;                      // ne filme pas ce qui est collé au visage (l'appareil, les mains)
        objectif.cullingMask = ~(1 << 5);                    // ne filme pas les interfaces (calque "UI" : viseur, tuto, peinture)
        objectif.enabled = false;
    }

    private void Update()
    {
        Transform tete = Camera.main != null ? Camera.main.transform : null;
        if (tete == null)
            return;
        if (viseur == null)
            CreerViseur(tete);

        // On vise dès qu'on le tient en main.
        bool vise = prise.isSelected;
        viseur.SetActive(vise);
        objectif.enabled = vise;
        if (vise)
            objectif.transform.SetPositionAndRotation(tete.position, tete.rotation); // il filme ce que tu regardes

        // Le flash s'efface en 0,3 seconde.
        flash.color = new Color(1f, 1f, 1f, Mathf.MoveTowards(flash.color.a, 0f, Time.deltaTime / 0.3f));

        RevenirSurLaTable();
    }

    private void QuandOnAppuie(ActivateEventArgs appui)
    {
        if (viseur.activeSelf && !photoEnCours)
            StartCoroutine(PrendrePhoto());
    }

    private IEnumerator PrendrePhoto()
    {
        photoEnCours = true;

        // Clic et flash.
        if (sonDeclencheur != null)
            AudioSource.PlayClipAtPoint(sonDeclencheur, transform.position, 0.7f);
        flash.color = Color.white;

        // La photo : on copie l'image de l'objectif dans une image à garder.
        Texture2D photo = new Texture2D(image.width, image.height, TextureFormat.RGB24, false);
        RenderTexture.active = image;
        photo.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0);
        photo.Apply();
        RenderTexture.active = null;

        // La pellicule : une carte blanche avec la photo, cachée dans le boîtier, qui sort par le dessous en 1 seconde.
        GameObject pellicule = Bloc(PrimitiveType.Cube, new Vector3(0.075f, 0.09f, 0.003f), Vector3.zero, Quaternion.identity, Color.white, false);
        GameObject dessin = GameObject.CreatePrimitive(PrimitiveType.Quad); // la photo elle-même, côté joueur (l'arrière de l'appareil)
        Destroy(dessin.GetComponent<Collider>());
        dessin.transform.SetParent(pellicule.transform, false);
        dessin.transform.localPosition = new Vector3(0f, 0.08f, -0.6f); // un peu en haut : le bas d'un polaroid est plus large
        dessin.transform.localScale = new Vector3(0.85f, 0.7f, 1f); // un Quad se voit du côté -z : ici, l'arrière, vers toi
        Material matiere = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        matiere.mainTexture = photo;
        dessin.GetComponent<Renderer>().material = matiere;

        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            pellicule.transform.localPosition = Vector3.down * Mathf.SmoothStep(0f, 0.09f, t); // elle glisse vers le bas
            yield return null;
        }
        yield return new WaitForSeconds(0.3f);

        // Pouf : elle disparaît de l'appareil...
        Pouf(pellicule.transform.position);
        Destroy(pellicule);
        yield return new WaitForSeconds(0.4f);

        // ... et re-pouf : elle arrive dans le livre.
        if (livre != null)
            livre.AjouterPhoto(photo);
        photoEnCours = false;
    }

    // Tombé par terre et lâché depuis 2 secondes (ou tombé sous le sol) : pouf, il revient sur la table.
    private void RevenirSurLaTable()
    {
        bool perdu = !prise.isSelected && transform.position.y < 0.3f;
        tempsParTerre = perdu ? tempsParTerre + Time.deltaTime : 0f;
        if (tempsParTerre < 2f && transform.position.y > -5f)
            return;

        Pouf(transform.position);
        corps.linearVelocity = Vector3.zero;
        corps.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(placeDepart, rotationDepart);
        Pouf(placeDepart);
        tempsParTerre = 0f;
    }

    // Le viseur : un grand cache noir devant les yeux, avec au milieu un carré qui montre l'objectif (bordé de blanc).
    private void CreerViseur(Transform tete)
    {
        viseur = new GameObject("Viseur");
        viseur.layer = 5; // calque "UI" : l'objectif ne le filme pas
        viseur.transform.SetParent(tete, false);
        viseur.transform.localPosition = new Vector3(0f, 0f, 0.3f);
        Canvas canvas = viseur.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 150; // par-dessus le tuto
        viseur.GetComponent<RectTransform>().sizeDelta = new Vector2(1000f, 1000f);
        viseur.transform.localScale = Vector3.one * 0.0012f; // 1000 pixels = 1,2 m : ça couvre toute la vue

        // Toujours dessiné par-dessus le décor.
        Material devant = new Material(Shader.Find("UI/Default"));
        devant.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);

        float carre = tailleCarre / 0.0012f; // la taille du carré, en pixels du Canvas
        Rectangle("Noir", 1000f, Color.black, devant);
        Rectangle("Bord", carre + 24f, Color.white, devant);
        RawImage vue = new GameObject("Vue").AddComponent<RawImage>();
        Placer(vue, carre, devant);
        vue.texture = image;
        flash = Rectangle("Flash", 1000f, new Color(1f, 1f, 1f, 0f), devant);

        viseur.SetActive(false);
    }

    private Image Rectangle(string nom, float taille, Color couleur, Material materiau)
    {
        Image rectangle = new GameObject(nom).AddComponent<Image>();
        Placer(rectangle, taille, materiau);
        rectangle.color = couleur;
        return rectangle;
    }

    // Range un élément d'interface au milieu du viseur, carré, de cette taille (en pixels).
    private void Placer(Graphic element, float taille, Material materiau)
    {
        element.gameObject.layer = 5;
        element.transform.SetParent(viseur.transform, false);
        element.rectTransform.sizeDelta = new Vector2(taille, taille);
        element.material = materiau;
        element.raycastTarget = false;
    }

    // Crée une forme simple, rangée dans l'appareil (elle bouge avec lui), d'une couleur plate.
    private GameObject Bloc(PrimitiveType forme, Vector3 taille, Vector3 place, Quaternion rotation, Color couleur, bool garderCollider)
    {
        GameObject bloc = GameObject.CreatePrimitive(forme);
        bloc.transform.SetParent(transform, false);
        bloc.transform.localPosition = place;
        bloc.transform.localRotation = rotation;
        bloc.transform.localScale = taille;
        if (!garderCollider)
            Destroy(bloc.GetComponent<Collider>());
        Material matiere = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        matiere.color = couleur;
        bloc.GetComponent<Renderer>().material = matiere;
        return bloc;
    }

    private void Pouf(Vector3 position)
    {
        if (sonPouf != null)
            AudioSource.PlayClipAtPoint(sonPouf, position, 0.6f);
        if (fumeePrefab == null)
            return;
        ParticleSystem fumee = Instantiate(fumeePrefab, position, Quaternion.identity);
        fumee.gameObject.SetActive(true);
        fumee.Play(true);
        Destroy(fumee.gameObject, 2.5f); // l'effet APPARITION dure 2,5 s
    }

    private void OnDestroy()
    {
        if (image != null)
            image.Release();
    }
}
