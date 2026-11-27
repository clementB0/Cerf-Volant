using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RespawnZone : MonoBehaviour
{
    [Tooltip("Glisse ici les objets de la scène qui doivent revenir à leur point de départ.")]
    [SerializeField] private List<GameObject> objetsAutorises = new List<GameObject>();

    private readonly Dictionary<Transform, Pose> pointsDeSpawn =
        new Dictionary<Transform, Pose>();

    // Pour ObjetsDerriereChevalet : les objets suivis, et les remettre à leur place de départ.
    public IEnumerable<Transform> Objets => pointsDeSpawn.Keys;

    public void Replacer(Transform cible)
    {
        if (pointsDeSpawn.TryGetValue(cible, out Pose spawn))
            ReplacerObjet(cible, spawn);
    }

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void Awake()
    {
        foreach (GameObject objet in objetsAutorises)
        {
            if (objet == null)
                continue;

            Transform cible = objet.transform;
            pointsDeSpawn[cible] = new Pose(cible.position, cible.rotation);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Le collider peut se trouver sur un enfant de l'objet choisi.
        Transform cible = other.transform;

        while (cible != null)
        {
            if (pointsDeSpawn.TryGetValue(cible, out Pose spawn))
            {
                ReplacerObjet(cible, spawn);
                return;
            }

            cible = cible.parent;
        }
    }

    private static void ReplacerObjet(Transform cible, Pose spawn)
    {
        if (cible.TryGetComponent(out PaintReservoir reservoir) && reservoir.OnRail)
            return;

        if (cible.TryGetComponent(out Rigidbody rb))
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.position = spawn.position;
            rb.rotation = spawn.rotation;
        }
        else
        {
            cible.SetPositionAndRotation(spawn.position, spawn.rotation);
        }
    }
}
