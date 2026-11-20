using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class FinPeinture : MonoBehaviour
{
    [SerializeField] private GameObject[] bombes;
    [SerializeField] private GameObject[] crayons;
    [SerializeField] private XRBaseInteractable boutonMontage;
    [SerializeField] private ParticleSystem fumeePrefab;

    [SerializeField, Min(0f)]
    private float delaiDisparition = 0.2f;

    [SerializeField, Min(0.1f)]
    private float dureeFumee = 1.2f;

    private bool finLancee;
    public bool DessinEnCours { get; private set; }

    public void TerminerPeinture()
    {
        TerminerPuis(null);
    }

    public void TerminerPuis(UnityEvent suite)
    {
        if (finLancee)
            return;

        finLancee = true;
        StartCoroutine(TransitionVersMontage(suite));
    }

    private IEnumerator TransitionVersMontage(UnityEvent suite)
    {
        yield return Disparition(bombes);

        if (boutonMontage != null)
        {
            foreach (var crayon in crayons)
                if (crayon != null)
                    CreerFumee(crayon.transform.position);

            yield return new WaitForSeconds(delaiDisparition);
            foreach (var crayon in crayons)
                if (crayon != null)
                    crayon.SetActive(true);

            DessinEnCours = true;
            boutonMontage.selectEntered.AddListener(ValiderDessin);
            boutonMontage.gameObject.SetActive(true);
            yield return new WaitUntil(() => !DessinEnCours);
            boutonMontage.selectEntered.RemoveListener(ValiderDessin);
            boutonMontage.gameObject.SetActive(false);
            yield return Disparition(crayons);
        }

        suite?.Invoke();
    }

    private void ValiderDessin(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        DessinEnCours = false;
    }

    private IEnumerator Disparition(GameObject[] outils)
    {
        yield return null;

        foreach (GameObject bombe in outils)
        {
            if (bombe == null)
                continue;

            // Arrête la peinture et son jet visuel.
            if (bombe.TryGetComponent<Pen>(out var crayon))
                crayon.enabled = false;

            foreach (SprayPainter peinture in
                     bombe.GetComponentsInChildren<SprayPainter>(true))
            {
                peinture.enabled = false;
            }

            foreach (SprayConeVFX effet in
                     bombe.GetComponentsInChildren<SprayConeVFX>(true))
            {
                effet.enabled = false;
            }

            XRGrabInteractable grab =
                bombe.GetComponent<XRGrabInteractable>();

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
        }

        yield return new WaitForEndOfFrame();

        foreach (GameObject bombe in outils)
        {
            if (bombe == null)
                continue;

            Rigidbody corps = bombe.GetComponent<Rigidbody>();

            if (corps != null)
                corps.isKinematic = true;

            CreerFumee(bombe.transform.position);
        }

        yield return new WaitForSeconds(delaiDisparition);

        foreach (GameObject bombe in outils)
        {
            if (bombe != null)
                bombe.SetActive(false);
        }
    }

    private void CreerFumee(Vector3 position)
    {
        if (fumeePrefab == null)
            return;

        var fumee = Instantiate(fumeePrefab, position, Quaternion.identity);
        fumee.gameObject.SetActive(true);
        fumee.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        fumee.Play(true);
        Destroy(fumee.gameObject, Mathf.Max(dureeFumee, delaiDisparition + 0.1f));
    }
}
