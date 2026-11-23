using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// FONCTIONNALITÉ : un indice quand le joueur ne fait rien. Après quelques secondes sans qu'il touche un de ces objets,
// ils sautillent chacun leur tour (avec un léger décalage), pour attirer son attention.
// Même style que la fin de la découpe : petits sauts, l'objet grossit un peu, se tortille et se balance de gauche à droite.
// Dès qu'une main survole ou tient un des objets, tous arrêtent (ils reviennent à leur place) et le compte reprend à zéro.
// Il ne fait rien tant qu'aucun des objets n'est visible (ex. les bombes, avant la peinture).
// Réutilisable : choix des cerfs-volants, cutter, bombes de peinture, crayons...
public class SautillementAttente : MonoBehaviour
{
    [SerializeField] private GameObject[] objets;            // les objets qui sautillent
    [SerializeField] private float attente = 8f;             // ils commencent après 8 secondes sans qu'on les touche
    [SerializeField] private float decalage = 0.35f;         // le suivant commence 0,35 seconde après le précédent
    [SerializeField] private float pause = 1.2f;             // pause après le dernier, avant de recommencer
    [SerializeField] private float dureeAnimation = 0.8f;    // l'animation d'un objet dure 0,8 seconde
    [SerializeField] private int nombreSauts = 2;            // il fait 2 petits sauts pendant ce temps
    [SerializeField] private float hauteurSaut = 0.04f;      // chaque saut monte de 4 cm
    [SerializeField] private float grossissement = 0.1f;     // il grossit jusqu'à +10 %
    [SerializeField] private float tortillement = 12f;       // il tourne de 12° à gauche puis à droite
    [SerializeField] private float balancement = 0.015f;     // il se décale de 1,5 cm à gauche puis à droite

    private float calme;              // depuis combien de temps personne ne touche aux objets
    private float tempsCycle;         // où on en est dans le tour
    private bool[] enAnimation;
    // Comment était l'objet avant son animation (on l'y remet à la fin).
    private Vector3[] positions;
    private Quaternion[] rotations;
    private Vector3[] tailles;
    private bool[] avaitLaGravite;    // les objets soumis à la gravité sont tenus en l'air pendant l'animation

    private void Start()
    {
        int n = objets.Length;
        enAnimation = new bool[n];
        positions = new Vector3[n];
        rotations = new Quaternion[n];
        tailles = new Vector3[n];
        avaitLaGravite = new bool[n];
    }

    private void Update()
    {
        // La phase n'a pas commencé (ou est finie) : aucun objet visible.
        bool phaseEnCours = false;
        bool quelquUnTouche = false;
        foreach (GameObject objet in objets)
        {
            if (objet == null || !objet.activeInHierarchy)
                continue;
            phaseEnCours = true;
            XRBaseInteractable interaction = objet.GetComponent<XRBaseInteractable>();
            if (interaction != null && (interaction.isHovered || interaction.isSelected))
                quelquUnTouche = true;
        }

        if (!phaseEnCours || quelquUnTouche)
        {
            calme = 0f;      // on recommence à compter
            tempsCycle = 0f;
            FinirLesAnimations();
            return;
        }

        calme += Time.deltaTime;
        if (calme < attente)
            return;

        // Chacun son tour : l'objet n°i commence à i × décalage dans le tour, puis tout recommence après la pause.
        float dureeTour = objets.Length * decalage + dureeAnimation + pause;
        tempsCycle = (tempsCycle + Time.deltaTime) % dureeTour;

        // "Gauche-droite" vu par le joueur (à plat).
        Vector3 droite = Camera.main != null ? Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized : Vector3.right;

        for (int i = 0; i < objets.Length; i++)
        {
            GameObject objet = objets[i];
            if (objet == null || !objet.activeInHierarchy)
                continue;

            float dedans = tempsCycle - i * decalage; // depuis combien de temps cet objet s'anime (négatif = pas encore)
            if (dedans < 0f || dedans >= dureeAnimation)
            {
                Finir(i);
                continue;
            }

            if (!enAnimation[i])
                Commencer(i);

            float u = dedans / dureeAnimation;                                     // de 0 à 1 pendant l'animation
            float enveloppe = Mathf.Sin(u * Mathf.PI);                              // 0 → 1 → 0 : plus fort au milieu
            float sauts = Mathf.Abs(Mathf.Sin(u * Mathf.PI * nombreSauts));         // petits sauts qui rebondissent
            float tourne = Mathf.Sin(u * Mathf.PI * 6f) * tortillement * enveloppe; // se tortille (comme à la fin de la découpe)
            float cote = Mathf.Sin(u * Mathf.PI * 2f) * balancement;                // va à droite, puis à gauche

            objet.transform.position = positions[i] + Vector3.up * sauts * hauteurSaut + droite * cote;
            objet.transform.rotation = Quaternion.AngleAxis(tourne, Vector3.up) * rotations[i];
            objet.transform.localScale = tailles[i] * (1f + grossissement * enveloppe);
        }
    }

    // L'objet n°i commence son animation : on retient sa place, et on coupe sa gravité le temps de l'animation.
    private void Commencer(int i)
    {
        Transform t = objets[i].transform;
        positions[i] = t.position;
        rotations[i] = t.rotation;
        tailles[i] = t.localScale;
        Rigidbody corps = objets[i].GetComponent<Rigidbody>();
        avaitLaGravite[i] = corps != null && !corps.isKinematic;
        if (avaitLaGravite[i])
            corps.isKinematic = true;
        enAnimation[i] = true;
    }

    // L'objet n°i a fini : on le remet comme avant (sauf sa place s'il est déjà dans la main du joueur).
    private void Finir(int i)
    {
        if (!enAnimation[i] || objets[i] == null)
            return;
        enAnimation[i] = false;

        Transform t = objets[i].transform;
        t.localScale = tailles[i];
        XRBaseInteractable interaction = objets[i].GetComponent<XRBaseInteractable>();
        bool dansLaMain = interaction != null && interaction.isSelected;
        if (!dansLaMain)
            t.SetPositionAndRotation(positions[i], rotations[i]);

        if (avaitLaGravite[i])
        {
            Rigidbody corps = objets[i].GetComponent<Rigidbody>();
            corps.isKinematic = false;
            corps.linearVelocity = Vector3.zero;
            corps.angularVelocity = Vector3.zero;
        }
    }

    private void FinirLesAnimations()
    {
        for (int i = 0; i < objets.Length; i++)
            Finir(i);
    }

    private void OnDisable()
    {
        if (enAnimation != null)
            FinirLesAnimations();
    }
}
