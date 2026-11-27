using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// FONCTIONNALITÉ : un outil (pistolet, pot, crayon...) qui tombe derrière le chevalet du cerf-volant est hors de portée.
// S'il y reste 2 secondes, il fait "pouf" et revient à sa place de départ (celle de RespawnZone), dans un autre pouf.
// À mettre sur l'objet "PieceDecoupee", avec PresentoirCerfVolant.
[RequireComponent(typeof(PresentoirCerfVolant))]
public class ObjetsDerriereChevalet : MonoBehaviour
{
    [SerializeField] private float attente = 2f;        // 2 secondes derrière le chevalet avant de revenir
    [SerializeField] private float profondeur = 0.6f;   // la zone "derrière" va jusqu'à 60 cm derrière la toile
    [SerializeField] private float marge = 0.1f;        // et dépasse de 10 cm de chaque côté de la toile
    [SerializeField] private float hauteur = 0.5f;      // jusqu'à 50 cm au-dessus de la table

    private PresentoirCerfVolant chevalet;
    private RespawnZone[] zones;                                       // elles savent où chaque outil est né
    private readonly Dictionary<Transform, float> arrives = new Dictionary<Transform, float>(); // depuis quand il est derrière

    private void Start()
    {
        chevalet = GetComponent<PresentoirCerfVolant>();
        zones = FindObjectsByType<RespawnZone>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        if (!chevalet.EstPose)
        {
            arrives.Clear();
            return;
        }

        foreach (RespawnZone zone in zones)
        {
            foreach (Transform outil in zone.Objets)
            {
                if (outil == null || !outil.gameObject.activeInHierarchy || !EstDerriere(outil.position) || EstTenu(outil))
                {
                    arrives.Remove(outil);
                    continue;
                }
                if (!arrives.TryGetValue(outil, out float depuis))
                {
                    arrives[outil] = Time.time;
                    continue;
                }
                if (Time.time - depuis < attente)
                    continue;

                // Pouf là où il est, puis pouf à sa place de départ.
                chevalet.Pouf(outil.position);
                zone.Replacer(outil);
                chevalet.Pouf(outil.position);
                arrives.Remove(outil);
            }
        }
    }

    // Derrière la toile (vu du joueur), pas trop loin, pas trop sur le côté, et sur la table.
    private bool EstDerriere(Vector3 position)
    {
        Vector3 ecart = position - chevalet.PiedChevalet;
        float loin = Vector3.Dot(ecart, chevalet.Devant);
        float cote = Mathf.Abs(Vector3.Dot(ecart, Vector3.Cross(Vector3.up, chevalet.Devant)));
        return loin > 0.02f && loin < profondeur && cote < chevalet.DemiLargeur + marge && ecart.y > -0.1f && ecart.y < hauteur;
    }

    // Tenu dans une main, ou pot clipsé sur le pistolet : on n'y touche pas.
    private static bool EstTenu(Transform outil)
    {
        if (outil.TryGetComponent(out PaintReservoir pot) && pot.OnRail)
            return true;
        XRGrabInteractable prise = outil.GetComponentInChildren<XRGrabInteractable>();
        return prise != null && prise.isSelected;
    }
}
