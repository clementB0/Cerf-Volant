using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// FONCTIONNALITÉ : la bombe de peinture se vide, et il faut la secouer pour la recharger.
//  - Elle a une charge (1 = pleine, 0 = vide) qui baisse pendant qu'on peint.
//  - Vide : elle ne peint plus, et la manette qui la tient vibre très fort.
//  - On la secoue (des allers-retours rapides avec la main) : "cling cling", elle se recharge un peu à chaque secousse.
//  - Vide : "Secoue !" et deux flèches (en haut et en bas) qui montent et descendent autour d'elle, jusqu'à ce qu'elle soit pleine.
// À mettre sur une bombe de peinture (dans la scène), à côté de son script SprayInput.
public class ReservoirBombe : MonoBehaviour
{
    [SerializeField] private Color couleurAide = new Color(1f, 0.85f, 0.2f); // le "Secoue !" et les flèches : jaune vif
    [SerializeField] private float dureePleine = 6f;          // pleine, elle peint pendant 6 secondes
    [SerializeField] private float rechargeParSecousse = 0.1f; // chaque secousse rend 10 % : 10 secousses = pleine
    [SerializeField] private float vitesseSecousse = 0.6f;     // en dessous de 0,6 m/s, ce n'est pas une secousse (juste un mouvement)
    [SerializeField] private AudioClip sonSecouer;             // le "cling cling" de la bille dans la bombe (PaintShake.mp3)
    [SerializeField, Range(0f, 1f)] private float volumeSecouer = 0.8f;

    // Combien de peinture il reste (1 = pleine, 0 = vide). Lu par SonSpray pour baisser le son quand elle se vide.
    public float Charge { get; private set; } = 1f;

    private SprayInput spray;           // sait si on appuie sur la gâchette en la tenant
    private XRGrabInteractable prise;   // sait si on la tient, et avec quelle manette
    private SprayConeVFX jet;           // le jet de peinture (sans lui, la bombe ne peint plus)
    private SprayPainter peinture;      // éteint par la fin de la peinture : on ne rallume plus le jet après
    private AudioSource hautParleur;    // d'où sort le "cling cling" : la bombe
    private Vector3 anciennePlace;      // où elle était à l'image d'avant
    private Vector3 ancienneVitesse;    // et à quelle vitesse elle bougeait
    private Transform aideSecouer;      // "Secoue !" + les deux flèches (un objet à part, pour ne pas tourner avec la bombe)
    private MeshRenderer[] formes;      // les formes de la bombe : pour trouver son milieu
    private float demiHauteur;          // la moitié de sa hauteur : les flèches se mettent juste au-dessus et en dessous
    private bool aRecharger;            // vidée, pas encore pleine : on montre l'aide

    private void Start()
    {
        spray = GetComponent<SprayInput>();
        prise = GetComponent<XRGrabInteractable>();
        jet = GetComponent<SprayConeVFX>();
        peinture = GetComponent<SprayPainter>();
        anciennePlace = transform.position;

        hautParleur = gameObject.AddComponent<AudioSource>();
        hautParleur.clip = sonSecouer;
        hautParleur.volume = volumeSecouer;
        hautParleur.playOnAwake = false;
        hautParleur.spatialBlend = 1f; // en 3D : on l'entend venir de la bombe

        CreerAideSecouer();
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f)
            return; // jeu en pause : pas de calcul de vitesse

        // 1) Elle se vide pendant qu'on peint.
        if (spray.IsSpraying && Charge > 0f)
        {
            Charge = Mathf.Max(0f, Charge - Time.deltaTime / dureePleine);
            if (Charge == 0f)
            {
                jet.enabled = false; // plus de jet : le script de peinture ne peint plus sans lui
                Vibrer(1f, 0.6f);    // vide : la manette vibre très fort
            }
        }

        // 2) On la secoue : un aller-retour rapide = la main change brusquement de sens.
        Vector3 vitesse = (transform.position - anciennePlace) / Time.deltaTime;
        bool changeDeSens = Vector3.Dot(vitesse, ancienneVitesse) < 0f;
        bool assezVite = ancienneVitesse.magnitude > vitesseSecousse;
        if (prise.isSelected && changeDeSens && assezVite)
        {
            Charge = Mathf.Min(1f, Charge + rechargeParSecousse);
            if (!hautParleur.isPlaying && sonSecouer != null)
                hautParleur.Play(); // "cling cling"
            Vibrer(0.3f, 0.05f);    // petit tac dans la main à chaque secousse

            // Elle a de nouveau de la peinture : le jet revient (sauf si la peinture est finie : là, on n'y touche plus).
            if (!jet.enabled && peinture != null && peinture.enabled)
                jet.enabled = true;
        }
        anciennePlace = transform.position;
        ancienneVitesse = vitesse;

        // 3) L'aide "Secoue !" : de quand elle est vide jusqu'à ce qu'elle soit pleine.
        if (Charge == 0f)
            aRecharger = true;
        else if (Charge >= 1f)
            aRecharger = false;
        aideSecouer.gameObject.SetActive(aRecharger);
        if (aRecharger)
            PlacerAideSecouer();
    }

    // Crée "Secoue !" et les deux flèches (cachés jusqu'à ce que la bombe soit vide).
    private void CreerAideSecouer()
    {
        formes = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer forme in formes)
            demiHauteur = Mathf.Max(demiHauteur, forme.bounds.extents.y);

        aideSecouer = new GameObject("AideSecouer_" + name).transform;
        Material couleur = new Material(Shader.Find("Sprites/Default")); // couleur unie, visible des deux côtés
        couleur.color = couleurAide;

        Mesh fleche = MaillageFleche();
        Fleche("FlecheHaut", fleche, couleur, new Vector3(0f, demiHauteur + 0.02f, 0f), 0f);
        Fleche("FlecheBas", fleche, couleur, new Vector3(0f, -demiHauteur - 0.02f, 0f), 180f); // la même, retournée

        // Le mot, au-dessus de la flèche du haut.
        TextMeshPro texte = new GameObject("Texte").AddComponent<TextMeshPro>();
        texte.transform.SetParent(aideSecouer, false);
        texte.transform.localPosition = new Vector3(0f, demiHauteur + 0.09f, 0f);
        texte.rectTransform.sizeDelta = new Vector2(0.4f, 0.08f);
        texte.text = "Secoue !";
        texte.fontSize = 0.4f; // environ 4 cm de haut
        texte.fontStyle = FontStyles.Bold;
        texte.alignment = TextAlignmentOptions.Center;
        texte.color = couleurAide;

        aideSecouer.gameObject.SetActive(false);
    }

    // Une flèche plate de 4 cm qui pointe vers le haut : la tige (un rectangle) puis la pointe (un triangle).
    // Sa base est en 0, sa pointe en y = 0,04. (Aussi utilisée par IndicationsCordes.)
    internal static Mesh MaillageFleche()
    {
        Mesh fleche = new Mesh();
        fleche.vertices = new[]
        {
            new Vector3(-0.005f, 0f), new Vector3(0.005f, 0f), new Vector3(0.005f, 0.02f), new Vector3(-0.005f, 0.02f), // la tige
            new Vector3(-0.015f, 0.02f), new Vector3(0.015f, 0.02f), new Vector3(0f, 0.04f),                           // la pointe
        };
        fleche.triangles = new[] { 0, 2, 1, 0, 3, 2, 4, 6, 5 };
        return fleche;
    }

    private void Fleche(string nom, Mesh forme, Material couleur, Vector3 place, float angle)
    {
        GameObject objet = new GameObject(nom);
        objet.transform.SetParent(aideSecouer, false);
        objet.transform.localPosition = place;
        objet.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        objet.AddComponent<MeshFilter>().sharedMesh = forme;
        objet.AddComponent<MeshRenderer>().sharedMaterial = couleur;
    }

    // L'aide se met autour de la bombe, tournée vers le joueur, et monte et descend comme le geste à faire.
    private void PlacerAideSecouer()
    {
        Bounds boite = formes[0].bounds;
        foreach (MeshRenderer forme in formes)
            boite.Encapsulate(forme.bounds);

        Vector3 versLaBombe = Camera.main != null ? boite.center - Camera.main.transform.position : Vector3.forward;
        versLaBombe.y = 0f; // reste droite (les flèches pointent toujours vers le haut et le bas)
        if (versLaBombe.sqrMagnitude < 1e-6f)
            versLaBombe = Vector3.forward;

        float secousse = Mathf.Sin(Time.time * 10f) * 0.02f; // monte et descend de 2 cm, 1,6 fois par seconde
        aideSecouer.SetPositionAndRotation(boite.center + Vector3.up * secousse, Quaternion.LookRotation(versLaBombe));
    }

    // Bombe cachée ou supprimée : l'aide (un objet à part) part avec elle.
    private void OnDisable()
    {
        if (aideSecouer != null)
            aideSecouer.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (aideSecouer != null)
            Destroy(aideSecouer.gameObject);
    }

    // Fait vibrer la manette qui la tient (force de 0 à 1, durée en secondes).
    // (Une main suivie sans manette ne peut pas vibrer : dans ce cas, il ne se passe rien.)
    private void Vibrer(float force, float duree)
    {
        if (prise.isSelected && prise.interactorsSelecting[0] is XRBaseInputInteractor manette)
            manette.SendHapticImpulse(force, duree);
    }
}
