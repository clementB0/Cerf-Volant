using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// FONCTIONNALITÉ : la guitare électrique posée par terre, qu'on attrape avec la MAIN GAUCHE, PAR LE MANCHE.
//  - Le modèle 3D (Guitare.fbx, fait dans Blender) apparaît à la place de l'objet.
//  - Seul le manche s'attrape, et seulement avec la main gauche (la droite sert à gratter : voir SonGuitare).
//  - Dès qu'on la prend, elle se met en position de jeu : le corps contre soi, les cordes devant.
// À mettre sur un objet vide posé par terre, à l'endroit où la guitare doit être.
public class PriseGuitare : MonoBehaviour
{
    [SerializeField] private GameObject modele;   // le modèle 3D (Guitare.fbx)
    [SerializeField] private float masse = 3.5f;  // une vraie guitare électrique pèse 3 à 4 kg

    private void Awake()
    {
        // 1) Le modèle, posé pile sur l'objet. (Dans le modèle : y = vers la tête du manche, z = côté des cordes.)
        Instantiate(modele, transform, false);

        // 2) Le manche : la seule zone qu'on peut attraper.
        //    Il doit exister AVANT d'ajouter la prise : la prise ne retient que les zones déjà là.
        BoxCollider manche = new GameObject("Zone_Manche").AddComponent<BoxCollider>();
        manche.transform.SetParent(transform, false);
        manche.center = new Vector3(0f, 0.17f, 0.0235f);   // le long du manche, de la jonction avec le corps jusqu'à la tête
        manche.size = new Vector3(0.05f, 0.40f, 0.03f);

        // 3) La guitare tombe et roule comme un vrai objet...
        Rigidbody corps = gameObject.AddComponent<Rigidbody>();
        corps.mass = masse;

        // ... et on peut l'attraper.
        XRGrabInteractable prise = gameObject.AddComponent<XRGrabInteractable>();

        // La position de jeu : la main tient le manche, la tête de la guitare part vers l'avant de la main (le bras tendu à gauche),
        // le corps revient contre le joueur, et les cordes regardent devant lui (vers le côté droit de la main gauche).
        // "Point de prise" = l'endroit et l'orientation de la guitare qui se collent à la main.
        Transform pointDePrise = new GameObject("Point de prise").transform;
        pointDePrise.SetParent(transform, false);
        pointDePrise.localPosition = new Vector3(0f, 0.28f, 0.0235f);                     // sur le manche, vers la tête
        pointDePrise.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.right); // l'avant de la main = vers la tête du manche
        prise.attachTransform = pointDePrise;
        // Seulement la main gauche.
        prise.selectFilters.Add(new XRSelectFilterDelegate((main, guitare) => main.handedness == InteractorHandedness.Left));

        // 4) Le corps : il touche le sol et la table, mais on ne peut pas l'attraper
        //    (ajouté APRÈS la prise : elle ne le connaît pas).
        BoxCollider caisse = new GameObject("Zone_Corps").AddComponent<BoxCollider>();
        caisse.transform.SetParent(transform, false);
        caisse.center = new Vector3(0.027f, -0.20f, 0f);
        caisse.size = new Vector3(0.47f, 0.58f, 0.045f);
    }
}
