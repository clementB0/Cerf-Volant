using UnityEngine;

public class CerfVolant : MonoBehaviour
{
    internal Transform CordeAttendue { private get; set; }

    internal bool Accepte(Transform corde)
    {
        // Les anciennes scènes de test utilisent aussi des cibles libres.
        return ReferenceEquals(CordeAttendue, null) || CordeAttendue == corde;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Fusion>(out var fusion) && Accepte(fusion.transform))
        {
            foreach (Transform enfant in transform)
            {
                if (!enfant.gameObject.activeSelf)
                {
                    enfant.gameObject.SetActive(true);
                }
            }
        }
    }
}
