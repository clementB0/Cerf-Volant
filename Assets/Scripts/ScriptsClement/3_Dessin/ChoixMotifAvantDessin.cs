using System.Collections;
using UnityEngine;

// FONCTIONNALITÉ : le choix du motif (TemplateChoice, de Louis) arrive après le spray, juste avant le dessin aux crayons.
// Quand la peinture est finie, FinPeinture fait apparaître les crayons et écoute le bouton rouge "Passer au montage" :
// on cache les crayons et on bloque le bouton le temps de choisir le motif, puis les crayons réapparaissent dans un pouf,
// et on dessine en suivant le motif (en pointillés).
// (Les scripts de Louis et de l'équipe ne sont pas modifiés : on attend juste le bon moment.)
// À mettre sur l'objet "TemplateChoice", à côté du script TemplateChoice.
public class ChoixMotifAvantDessin : MonoBehaviour
{
    [SerializeField] private FinPeinture finPeinture;      // la fin de la peinture (sur GestionTransitionCutToTag)
    [SerializeField] private TemplateChoice choixMotif;    // le choix du motif, de Louis
    [SerializeField] private GameObject boiteCrayons;      // la boîte de crayons (Boite_Crayons_Dessin)
    [SerializeField] private Component boutonMontage;      // le bouton "Passer au montage" (son XRSimpleInteractable)
    [SerializeField] private ParticleSystem fumeePrefab;   // le nuage de fumée (VFX/APPARITION)
    [SerializeField] private AudioClip sonPouf;            // le "pouf !" (SonFumee.wav)
    [SerializeField] private AudioClip sonChoix;           // petit son quand on a choisi le motif (SonChoix.wav)

    private bool lance;

    // LateUpdate : juste après que FinPeinture a fait apparaître les crayons, et avant l'affichage de l'image (pas de clignotement).
    private void LateUpdate()
    {
        if (lance || !finPeinture.DessinEnCours)
            return;
        lance = true;
        if (!choixMotif.HasChosen) // (déjà choisi ailleurs, ex. après la découpe dans une autre scène : on ne redemande pas)
            StartCoroutine(ChoisirPuisDessiner());
    }

    private IEnumerator ChoisirPuisDessiner()
    {
        // Les crayons attendent : d'abord le motif. Le bouton reste sur la table, mais on ne peut pas appuyer dessus.
        boiteCrayons.SetActive(false);
        Behaviour bouton = (Behaviour)boutonMontage;
        bouton.enabled = false;

        // Le cerf-volant peint : celui de l'atelier choisi au début (les autres ateliers sont cachés).
        choixMotif.Show(FindFirstObjectByType<PieceDecoupee>());
        yield return new WaitUntil(() => choixMotif.HasChosen);
        if (sonChoix != null)
            AudioSource.PlayClipAtPoint(sonChoix, boiteCrayons.transform.position, 0.6f);

        // Les crayons reviennent, dans un pouf, et on peut de nouveau appuyer sur le bouton.
        Pouf(boiteCrayons.transform.position);
        boiteCrayons.SetActive(true);
        bouton.enabled = true;
    }

    private void Pouf(Vector3 position)
    {
        if (sonPouf != null)
            AudioSource.PlayClipAtPoint(sonPouf, position, 0.6f);
        if (fumeePrefab == null)
            return;
        ParticleSystem fumee = Instantiate(fumeePrefab, position, Quaternion.identity);
        fumee.gameObject.SetActive(true); // au cas où le prefab est enregistré caché
        fumee.Play(true);
        Destroy(fumee.gameObject, 2.5f); // l'effet APPARITION dure 2,5 s
    }
}
