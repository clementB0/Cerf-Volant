using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// FONCTIONNALITÉ : une fois le cerf-volant parti en vol, lâcher une poignée la fait tomber, et le cerf-volant tombe avec
// (il faudra le relancer en levant les deux poignées : "Releve", voir VolCerfVolant et MessageRelance).
// Peu après, la poignée réapparaît sur la table, à sa place de départ, dans un pouf.
// À mettre sur "PieceDecoupee", avec VolCerfVolant.
[RequireComponent(typeof(VolCerfVolant))]
public class RetourPoignees : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable[] poignees;   // les deux poignées (gauche, droite)
    [SerializeField] private ParticleSystem fumeePrefab;      // le nuage de fumée (VFX/APPARITION)
    [SerializeField] private AudioClip sonPouf;               // le "pouf !" (SonFumee.wav)
    [SerializeField] private float tempsAvantRetour = 1.5f;   // le temps de la voir tomber, avant qu'elle revienne sur la table

    private VolCerfVolant vol;
    private bool aVole;              // le cerf-volant est déjà parti en vol (avant, lâcher une poignée ne fait rien de spécial)
    private Vector3[] places;        // la place de départ de chaque poignée sur la table (dans l'atelier)
    private Quaternion[] rotations;

    private void Start()
    {
        vol = GetComponent<VolCerfVolant>();
        places = new Vector3[poignees.Length];
        rotations = new Quaternion[poignees.Length];
        for (int i = 0; i < poignees.Length; i++)
        {
            places[i] = poignees[i].transform.localPosition;
            rotations[i] = poignees[i].transform.localRotation;
            int numero = i; // (copie pour la ligne suivante : chaque poignée garde son numéro)
            poignees[i].selectExited.AddListener(_ => QuandLachee(numero));
        }
    }

    private void Update()
    {
        if (vol.EnVol)
            aVole = true;
    }

    private void QuandLachee(int numero)
    {
        if (!aVole || !isActiveAndEnabled)
            return;
        vol.Tomber(); // le cerf-volant tombe avec
        StartCoroutine(TomberPuisRevenir(poignees[numero], numero));
    }

    private IEnumerator TomberPuisRevenir(XRGrabInteractable poignee, int numero)
    {
        // Elle tombe pour de vrai (d'habitude, elle ne bouge que quand on la tient).
        Rigidbody corps = poignee.GetComponent<Rigidbody>();
        corps.isKinematic = false;
        corps.useGravity = true;

        yield return new WaitForSeconds(tempsAvantRetour);
        if (poignee.isSelected)
            yield break; // rattrapée entre-temps : on la laisse dans la main (elle retombera si on la relâche)

        // Pouf là où elle est, puis elle réapparaît immobile sur la table, dans un autre pouf.
        Pouf(poignee.transform.position);
        corps.linearVelocity = Vector3.zero;
        corps.angularVelocity = Vector3.zero;
        corps.useGravity = false;
        corps.isKinematic = true;
        poignee.transform.SetLocalPositionAndRotation(places[numero], rotations[numero]);
        Pouf(poignee.transform.position);
    }

    // Un nuage de fumée et un "pouf !" à cet endroit (comme PresentoirCerfVolant).
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
