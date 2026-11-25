using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

// FONCTIONNALITÉ : au lancement du jeu, le joueur apparaît toujours au même endroit, tourné du même côté.
// Sans ce script, sa place dépend de l'endroit où était le casque avant d'appuyer sur Play.
// Le point de départ = l'endroit où le "XR Origin" est posé dans la scène, et la direction vers laquelle il est tourné.
// Pour changer le point de départ : déplacer / tourner le XR Origin dans la scène (rien à régler ici).
// À mettre sur l'objet "ChoixCerfVolant" (il est là dès le début de la scène). On ne modifie pas le XR Origin.
public class DepartJoueur : MonoBehaviour
{
    [SerializeField] private float attenteMax = 3f; // si le casque ne donne aucune position après ce temps, on ne fait rien

    // "IEnumerator Start" = un Start qui peut attendre plusieurs images (avec "yield return null").
    private IEnumerator Start()
    {
        XROrigin joueur = FindFirstObjectByType<XROrigin>();
        if (joueur == null || joueur.Camera == null)
            yield break; // pas de joueur VR dans la scène : rien à placer

        // On retient le point de départ tout de suite, avant de bouger quoi que ce soit.
        Vector3 depart = joueur.transform.position;
        Vector3 direction = joueur.transform.forward;

        // Les premières images, le casque n'a pas encore donné sa position (la tête est à zéro) : on attend qu'il la donne.
        Transform tete = joueur.Camera.transform;
        float fin = Time.time + attenteMax;
        while (tete.localPosition == Vector3.zero && tete.localRotation == Quaternion.identity)
        {
            if (Time.time > fin)
                yield break; // pas de casque (ou il est pile au point de départ) : on laisse tout comme c'est
            yield return null; // on attend l'image suivante
        }

        // 1) On tourne le joueur pour que son regard aille dans la direction de départ.
        joueur.MatchOriginUpCameraForward(Vector3.up, direction);
        // 2) On déplace le joueur pour que sa tête soit juste au-dessus du point de départ (on garde sa hauteur).
        joueur.MoveCameraToWorldLocation(new Vector3(depart.x, tete.position.y, depart.z));
        // Le XR Origin a un "Character Controller" : sans cette ligne, il peut annuler le déplacement à l'image suivante.
        Physics.SyncTransforms();
    }
}
