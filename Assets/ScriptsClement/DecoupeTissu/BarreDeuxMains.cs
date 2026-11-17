using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// FONCTIONNALITÉ : la barre du cerf-volant se tient à deux mains, chaque main à SA place.
// La main gauche va sur "Prise Main Gauche", la main droite sur "Prise Main Droite", peu importe laquelle attrape en premier.
// À mettre sur l'objet "Baton", à la place du XR Grab Interactable.
//
// C'est le XR Grab Interactable du template ("hérite" = il fait tout pareil), avec une seule différence :
// le choix de l'endroit où la main se place.
public class BarreDeuxMains : XRGrabInteractable
{
    [SerializeField] private Transform priseMainGauche; // l'objet "PriseGauche" sur la barre
    [SerializeField] private Transform priseMainDroite; // l'objet "PriseDroite" sur la barre

    protected override void Awake()
    {
        base.Awake(); // le XR Grab Interactable se prépare normalement

        // "Multiple" = la barre accepte deux mains en même temps.
        selectMode = InteractableSelectMode.Multiple;
    }

    // Le template appelle ça pour savoir où placer la main qui tient l'objet.
    public override Transform GetAttachTransform(IXRInteractor interactor)
    {
        if (interactor.handedness == InteractorHandedness.Left)
            return priseMainGauche;

        if (interactor.handedness == InteractorHandedness.Right)
            return priseMainDroite;

        return base.GetAttachTransform(interactor); // main inconnue : on laisse le template faire comme d'habitude
    }

    // Le template appelle ça plusieurs fois par image pour déplacer l'objet tenu.
    // Avec une main, il fait tout. Avec deux, il ne suit que la 1re main (la 2e pouvait bouger sans que la barre suive) :
    // alors, juste après lui, on place nous-mêmes la barre pour qu'elle reste dans LES DEUX mains.
    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        // On ne s'en mêle qu'avec deux mains, et seulement aux moments où le template déplace l'objet.
        bool momentDeBouger = updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic
                           || updatePhase == XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender;
        if (interactorsSelecting.Count < 2 || !momentDeBouger)
            return;

        // Quelle main est la gauche, laquelle est la droite (si on ne sait pas, on garde l'ordre où elles ont attrapé).
        IXRSelectInteractor mainGauche = interactorsSelecting[0];
        IXRSelectInteractor mainDroite = interactorsSelecting[1];
        if (mainGauche.handedness == InteractorHandedness.Right || mainDroite.handedness == InteractorHandedness.Left)
            (mainGauche, mainDroite) = (mainDroite, mainGauche); // on échange les deux

        // Le point exact de chaque main (le même que le template utilise pour tenir les objets).
        Transform gauche = mainGauche.GetAttachTransform(this);
        Transform droite = mainDroite.GetAttachTransform(this);

        // La barre : au milieu des deux mains, tendue de la main gauche vers la main droite,
        // le "haut" de la barre = le haut moyen des deux mains.
        Vector3 versLaDroite = droite.position - gauche.position;
        if (versLaDroite.sqrMagnitude < 0.0001f)
            return; // mains collées l'une à l'autre : impossible de savoir dans quel sens tendre la barre
        Vector3 haut = gauche.up + droite.up;
        Vector3 devant = Vector3.Cross(versLaDroite, haut); // perpendiculaire aux deux : l'avant de la barre

        transform.SetPositionAndRotation(
            (gauche.position + droite.position) / 2f,
            Quaternion.LookRotation(devant, haut));
    }
}
