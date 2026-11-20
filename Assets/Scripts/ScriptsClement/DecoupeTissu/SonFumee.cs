using UnityEngine;

// FONCTIONNALITÉ : un "pouf !" avec les nuages de fumée.
// Il y a deux nuages de fumée dans la partie :
//  - à la fin de la découpe, le cutter disparaît dans la fumée ;
//  - à la fin de la peinture, les bombes de peinture disparaissent dans la fumée.
// À mettre sur l'objet "PieceDecoupee", à côté de VolCerfVolant.
public class SonFumee : MonoBehaviour
{
    [SerializeField] private AudioClip sonFumee;   // le "pouf !" (SonFumee.wav)
    [SerializeField] private Transform cutter;     // là où part la fumée de fin de découpe
    [SerializeField] private float volume = 0.6f;

    private VolCerfVolant vol;
    private bool volAllume;   // était-il allumé à l'image d'avant ?

    private void Start()
    {
        vol = GetComponent<VolCerfVolant>();
        volAllume = vol.enabled;

        // Start est appelé quand la pièce apparaît, c'est-à-dire à la fin de la découpe : le cutter part en fumée.
        if (cutter != null)
            Pouf(cutter.position);
    }

    private void Update()
    {
        // VolCerfVolant vient juste de s'allumer : la peinture est finie, les bombes partent en fumée.
        if (vol.enabled && !volAllume)
        {
            // Un "pouf" sur chaque bombe encore visible ("Exclude" : on ignore celles qui sont cachées).
            foreach (SprayInput bombe in FindObjectsByType<SprayInput>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                Pouf(bombe.transform.position);
        }

        volAllume = vol.enabled;
    }

    private void Pouf(Vector3 position)
    {
        if (sonFumee != null)
            AudioSource.PlayClipAtPoint(sonFumee, position, volume);
    }
}
