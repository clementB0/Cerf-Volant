using UnityEngine;
using UnityEngine.InputSystem;

// FONCTIONNALITÉ : un raccourci. Bouton X (manette gauche) : on passe directement à l'étape où on accroche les fils
// au cerf-volant (le montage), sans découper, peindre ni dessiner.
// Il ne marche que si un cerf-volant a déjà été choisi, et une seule fois par partie.
// À mettre sur l'objet "ChoixCerfVolant" (à côté du script ChoixCerfVolant, qui sait quel cerf-volant a été choisi).
[RequireComponent(typeof(ChoixCerfVolant))]
public class RaccourciMontage : MonoBehaviour
{
    // Les scripts qui enchaînent les étapes tout seuls (TransitionDecoupePeinture, FinPeinture, ChoixMotifAvantDessin) :
    // on les arrête, sinon ils feraient réapparaître les bombes ou les crayons quelques secondes plus tard.
    [SerializeField] private MonoBehaviour[] transitions;
    [SerializeField] private GameObject[] aCacher;      // tout ce qui ne sert plus au montage : le cutter, les 3 bombes, la boîte de crayons, le choix du motif
    [SerializeField] private Component boutonMontage;   // le bouton rouge "Passer au montage" : caché aussi

    private ChoixCerfVolant choix;
    private InputAction boutonX;   // le bouton X de la manette gauche
    private bool fait;             // vrai une fois le raccourci utilisé (il ne sert qu'une fois)

    private void Start()
    {
        choix = GetComponent<ChoixCerfVolant>();
    }

    private void OnEnable()
    {
        // "{PrimaryButton}" = le 1er bouton de la manette : X sur la manette gauche d'un Quest.
        boutonX = new InputAction(binding: "<XRController>{LeftHand}/{PrimaryButton}");
        boutonX.Enable();
    }

    private void OnDisable()
    {
        boutonX.Dispose(); // on arrête d'écouter le bouton
    }

    private void Update()
    {
        if (fait || !boutonX.WasPressedThisFrame())
            return;

        // Aucun cerf-volant choisi : le bouton ne fait rien.
        GameObject atelier = choix.AtelierChoisi;
        if (atelier == null)
            return;

        // La pièce (le cerf-volant découpé) et le tissu de cet atelier. ("true" : on cherche aussi dans les objets cachés.)
        PieceDecoupee piece = atelier.GetComponentInChildren<PieceDecoupee>(true);
        PatronPointilles patron = atelier.GetComponentInChildren<PatronPointilles>(true);
        if (piece == null || patron == null || piece.PriseFinaleAutorisee)
            return; // on est déjà au montage (ou plus loin) : rien à sauter

        fait = true;

        // 1) La découpe. Si la pièce n'est pas encore apparue, on coupe d'abord ses deux petits spectacles d'arrivée
        //    (le saut de victoire et le chevalet de peinture) : ils la déplaceraient pendant qu'on essaie de l'attraper.
        if (!piece.gameObject.activeSelf)
        {
            if (piece.TryGetComponent(out ReussiteDecoupe saut))
                saut.enabled = false;
            if (piece.TryGetComponent(out PresentoirCerfVolant chevalet))
                chevalet.enabled = false;
        }
        patron.FinirTout(); // comme si le dernier trait venait d'être découpé : la pièce apparaît, le tissu disparaît

        // 2) On arrête les enchaînements en cours (ex. "les bombes apparaissent dans 1 seconde").
        foreach (MonoBehaviour transition in transitions)
            if (transition != null)
                transition.StopAllCoroutines();

        // 3) On cache les outils des étapes sautées.
        foreach (GameObject objet in aCacher)
            if (objet != null)
                objet.SetActive(false);
        if (boutonMontage != null)
            boutonMontage.gameObject.SetActive(false);

        // 4) Le montage : exactement ce que fait la fin de la peinture d'habitude.
        piece.GetComponent<MeshRenderer>().enabled = true; // (le choix du motif cache la pièce pendant qu'on choisit : on la remontre)
        piece.AutoriserPriseFinale();                      // on peut attraper le cerf-volant
        piece.GetComponent<VolCerfVolant>().CommencerMontage(); // les poignées et les cordes à accrocher apparaissent
    }
}
