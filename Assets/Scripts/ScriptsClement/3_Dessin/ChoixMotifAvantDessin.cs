using System.Collections;
using UnityEngine;

// FONCTIONNALITÉ : le choix du motif (TemplateChoice, de Louis) arrive après le spray, juste avant le dessin aux crayons.
// Quand la peinture est finie, FinPeinture fait apparaître la boîte de crayons et le bouton "Passer au montage" :
// on les cache le temps de choisir le motif, puis ils réapparaissent dans un pouf, et on dessine en suivant le motif (en pointillés).
// (Les scripts de Louis et de l'équipe ne sont pas modifiés : on attend juste le bon moment.)
// À mettre sur l'objet "TemplateChoice", à côté du script TemplateChoice.
public class ChoixMotifAvantDessin : MonoBehaviour
{
    [SerializeField] private FinPeinture finPeinture;      // la fin de la peinture (sur GestionTransitionCutToTag)
    [SerializeField] private TemplateChoice choixMotif;    // le choix du motif, de Louis
    [SerializeField] private GameObject boiteCrayons;      // la boîte de crayons (Boite_Crayons_Dessin)
    [SerializeField] private Component boutonMontage;      // le bouton "Passer au montage"
    [SerializeField] private ParticleSystem fumeePrefab;   // le nuage de fumée (FumeeTransition)
    [SerializeField] private AudioClip sonPouf;            // le "pouf !" (SonFumee.wav)

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
        // Les crayons et le bouton attendent : d'abord le motif.
        boiteCrayons.SetActive(false);
        boutonMontage.gameObject.SetActive(false);

        // Le cerf-volant peint : celui de l'atelier choisi au début (les autres ateliers sont cachés).
        choixMotif.Show(FindFirstObjectByType<PieceDecoupee>());
        yield return new WaitUntil(() => choixMotif.HasChosen);

        // Les crayons et le bouton reviennent, dans un pouf.
        Pouf(boiteCrayons.transform.position);
        boiteCrayons.SetActive(true);
        boutonMontage.gameObject.SetActive(true);
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
        Destroy(fumee.gameObject, 1.2f);
    }
}
