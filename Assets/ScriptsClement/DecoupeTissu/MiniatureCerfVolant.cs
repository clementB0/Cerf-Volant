using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// FONCTIONNALITÉ : un petit cerf-volant du menu de choix.
// Main dessus → il grossit un peu. Main dessus + bouton de prise (grip) → il est choisi.
// À mettre sur chaque miniature, avec un "XR Simple Interactable" (du template : c'est lui qui détecte la main).
[RequireComponent(typeof(MeshFilter), typeof(MeshCollider), typeof(XRSimpleInteractable))]
[ExecuteAlways] // le script tourne aussi dans l'éditeur : on voit la forme sans lancer le jeu
public class MiniatureCerfVolant : MonoBehaviour
{
    [SerializeField] private FormeCerfVolant forme;     // la forme à montrer (Papillon, Raie, Dragon...)
    [SerializeField] private GameObject atelier;        // l'atelier de découpe qui correspond (le prefab CerfVolantADecouper dans la scène)
    [SerializeField] private ChoixCerfVolant choix;     // le menu, à prévenir quand on choisit
    [SerializeField] private float grossissement = 1.3f; // de combien il grossit quand la main est dessus

    private XRSimpleInteractable detecteur; // détecte la main (survol) et le bouton (sélection)
    private Vector3 tailleDeBase;           // sa taille normale, pour y revenir

    // À l'apparition (dans l'éditeur ET en jeu) : on fabrique la forme, et la zone que la main peut toucher a la même forme.
    private void OnEnable()
    {
        if (forme == null)
            return; // pas encore de forme choisie dans l'Inspector : rien à afficher

        Mesh maillage = forme.CreerMaillage();
        maillage.hideFlags = HideFlags.DontSave; // refabriquée à chaque ouverture : inutile de l'enregistrer dans la scène
        GetComponent<MeshFilter>().sharedMesh = maillage;
        GetComponent<MeshCollider>().sharedMesh = maillage;
    }

    private void Start()
    {
        if (!Application.isPlaying)
            return; // dans l'éditeur, on ne fait qu'afficher la forme (pas de main à détecter)

        tailleDeBase = transform.localScale;

        // On s'abonne aux "événements" du XR Simple Interactable : il appellera nos fonctions au bon moment.
        detecteur = GetComponent<XRSimpleInteractable>();
        detecteur.hoverEntered.AddListener(QuandLaMainArrive);
        detecteur.hoverExited.AddListener(QuandLaMainPart);
        detecteur.selectEntered.AddListener(QuandOnChoisit);
    }

    private void QuandLaMainArrive(HoverEnterEventArgs infos)
    {
        transform.localScale = tailleDeBase * grossissement;
    }

    private void QuandLaMainPart(HoverExitEventArgs infos)
    {
        // Le rayon et la main peuvent survoler en même temps : on ne rapetisse que si plus rien ne survole.
        if (!detecteur.isHovered)
            transform.localScale = tailleDeBase;
    }

    private void QuandOnChoisit(SelectEnterEventArgs infos)
    {
        choix.Choisir(atelier);
    }
}
