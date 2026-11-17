using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class FinPeinture : MonoBehaviour
{
    [SerializeField] private GameObject[] bombes;
    [SerializeField] private ParticleSystem fumeePrefab;

    [SerializeField, Min(0f)]
    private float delaiDisparition = 0.2f;

    [SerializeField, Min(0.1f)]
    private float dureeFumee = 1.2f;

    private bool finLancee;

    public void TerminerPeinture()
    {
        if (finLancee)
            return;

        finLancee = true;
        StartCoroutine(Disparition());
    }

    private IEnumerator Disparition()
    {
        yield return null;

        foreach (GameObject bombe in bombes)
        {
            if (bombe == null)
                continue;

            // Arrête la peinture et son jet visuel.
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

        foreach (GameObject bombe in bombes)
        {
            if (bombe == null)
                continue;

            Rigidbody corps = bombe.GetComponent<Rigidbody>();

            if (corps != null)
                corps.isKinematic = true;

            if (fumeePrefab != null)
            {
                ParticleSystem fumee = Instantiate(
                    fumeePrefab,
                    bombe.transform.position,
                    Quaternion.identity
                );

                fumee.gameObject.SetActive(true);

                fumee.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );

                fumee.Play(true);

                Destroy(
                    fumee.gameObject,
                    Mathf.Max(dureeFumee, delaiDisparition + 0.1f)
                );
            }
        }

        yield return new WaitForSeconds(delaiDisparition);

        foreach (GameObject bombe in bombes)
        {
            if (bombe != null)
                bombe.SetActive(false);
        }
    }
}