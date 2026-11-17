using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class TransitionDecoupePeinture : MonoBehaviour
{
    [Header("Objets")]
    [SerializeField] private GameObject tissu;
    [SerializeField] private GameObject pieceDecoupee;
    [SerializeField] private GameObject cutter;
    [SerializeField] private GameObject[] bombes;

    [Header("Fumée")]
    [SerializeField] private ParticleSystem fumeePrefab;
    [SerializeField] private float delaiBascule = 0.2f;
    [SerializeField] private float dureeFumee = 1.2f;

    private bool transitionLancee;

    private void Start()
    {
        foreach (GameObject bombe in bombes)
            bombe.SetActive(false);
    }

    // Appelé lorsque tous les pointillés sont reliés.
    public void TerminerDecoupe()
    {
        if (transitionLancee)
            return;

        transitionLancee = true;
        StartCoroutine(Transition());
    }

    private IEnumerator Transition()
    {
        // Laisse terminer la frame de découpe.
        yield return null;

        pieceDecoupee.SetActive(true);
        tissu.SetActive(false);

        // Libère le cutter s'il est encore tenu.
        XRGrabInteractable grab =
            cutter.GetComponent<XRGrabInteractable>();

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

        // Attend la fin du détachement XR puis fige le cutter.
        yield return new WaitForEndOfFrame();

        Rigidbody corps = cutter.GetComponent<Rigidbody>();

        if (corps != null)
            corps.isKinematic = true;

        // Disparition du cutter.
        CreerFumee(cutter.transform.position);

        yield return new WaitForSeconds(delaiBascule);

        cutter.SetActive(false);

        yield return new WaitForSeconds(
            Mathf.Max(0f, dureeFumee - delaiBascule)
        );

        // Apparition simultanée des bombes.
        foreach (GameObject bombe in bombes)
            CreerFumee(bombe.transform.position);

        yield return new WaitForSeconds(delaiBascule);

        foreach (GameObject bombe in bombes)
            bombe.SetActive(true);
    }

    private void CreerFumee(Vector3 position)
    {
        ParticleSystem fumee = Instantiate(
            fumeePrefab,
            position,
            Quaternion.identity
        );

        fumee.Play();

        Destroy(fumee.gameObject, dureeFumee);
    }
}