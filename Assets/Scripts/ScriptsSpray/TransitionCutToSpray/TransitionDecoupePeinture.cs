using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class TransitionDecoupePeinture : MonoBehaviour
{
    [Header("Outils")]
    [SerializeField] private GameObject cutter;
    [SerializeField, InspectorName("Outils de peinture")] private GameObject[] bombes;
    [SerializeField] private TemplateChoice templateChoice;

    [Header("Effet")]
    [SerializeField] private ParticleSystem fumeePrefab;
    [SerializeField, Min(0f)] private float delaiBascule = 0.2f;
    [SerializeField, Min(0.1f)] private float dureeFumee = 1.2f;

    private bool transitionLancee;

    private void Start()
    {
        // Les bombes attendent la fin de la découpe.
        foreach (GameObject bombe in bombes)
        {
            if (bombe != null)
                bombe.SetActive(false);
        }
    }

    // À ajouter aux événements de fin de découpe existants.
    public void TerminerDecoupe()
    {
        TerminerDecoupe(null);
    }

    public void TerminerDecoupeAvecTemplate(PieceDecoupee piece)
    {
        TerminerDecoupe(piece);
    }

    private void TerminerDecoupe(PieceDecoupee piece)
    {
        if (transitionLancee)
            return;

        if (cutter == null || fumeePrefab == null)
        {
            Debug.LogError(
                "Transition : assigne le Cutter et le Fumee Prefab.",
                this
            );

            return;
        }

        if (templateChoice != null && (piece == null || !piece.EstConstruite))
        {
            Debug.LogError("Transition : la sélection de template attend la pièce découpée.", this);
            return;
        }

        transitionLancee = true;
        StartCoroutine(Transition(piece));
    }

    private IEnumerator Transition(PieceDecoupee piece)
    {
        // Laisse les événements de découpe finir leur travail.
        yield return null;

        XRGrabInteractable grab =
            cutter.GetComponent<XRGrabInteractable>();

        // Libère le cutter s'il est encore tenu.
        if (grab != null)
        {
            if (grab.interactionManager != null)
            {
                for (int i = grab.interactorsSelecting.Count - 1;
                     i >= 0;
                     i--)
                {
                    grab.interactionManager.SelectExit(
                        grab.interactorsSelecting[i],
                        grab
                    );
                }
            }

            grab.enabled = false;
        }

        yield return new WaitForEndOfFrame();

        // Maintient le cutter sur place pendant sa disparition.
        Rigidbody corps = cutter.GetComponent<Rigidbody>();

        if (corps != null)
            corps.isKinematic = true;

        CreerFumee(cutter.transform.position);

        yield return new WaitForSeconds(delaiBascule);

        cutter.SetActive(false);

        yield return new WaitForSeconds(
            Mathf.Max(0f, dureeFumee - delaiBascule)
        );

        if (templateChoice != null)
        {
            templateChoice.Show(piece);
            yield return new WaitUntil(() => templateChoice.HasChosen);
        }

        // Un nuage à l'emplacement de chaque bombe.
        foreach (GameObject bombe in bombes)
        {
            if (bombe != null)
                CreerFumee(bombe.transform.position);
        }

        yield return new WaitForSeconds(delaiBascule);

        foreach (GameObject bombe in bombes)
        {
            if (bombe != null)
                bombe.SetActive(true);
        }
    }

    private void CreerFumee(Vector3 position)
    {
        ParticleSystem fumee = Instantiate(
            fumeePrefab,
            position,
            Quaternion.identity
        );

        // Fonctionne aussi si le prefab était enregistré inactif.
        fumee.gameObject.SetActive(true);

        fumee.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        fumee.Play(true);

        Destroy(
            fumee.gameObject,
            Mathf.Max(dureeFumee, delaiBascule + 0.1f)
        );
    }
}
