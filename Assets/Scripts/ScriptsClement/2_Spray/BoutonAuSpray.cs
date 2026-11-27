using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// FONCTIONNALITÉ : le bouton rouge "Passer au montage" apparaît dès le début de la peinture (quand les outils de peinture apparaissent).
// Appuyer dessus termine la peinture (elle ne finit plus toute seule au pourcentage).
// Ensuite il reste sur la table pour les crayons : FinPeinture l'écoute à son tour pour "Passer au montage".
// À mettre sur l'objet "GestionTransitionCutToTag" (toujours actif, à côté de FinPeinture).
public class BoutonAuSpray : MonoBehaviour
{
    [SerializeField] private GameObject outilPeinture;   // un des outils de peinture (le pistolet) : quand il apparaît, la peinture commence
    [SerializeField, FormerlySerializedAs("coccinelle")]
    private XRBaseInteractable bouton;                    // le bouton rouge "Passer au montage"

    private bool apparu;
    private bool finie;

    private void Update()
    {
        if (apparu || !outilPeinture.activeInHierarchy)
            return;
        apparu = true;
        bouton.selectEntered.AddListener(FinirPeinture);
        bouton.gameObject.SetActive(true);
    }

    private void FinirPeinture(SelectEnterEventArgs args)
    {
        if (finie)
            return;
        finie = true;
        bouton.selectEntered.RemoveListener(FinirPeinture); // le prochain appui sera pour les crayons

        // Le cerf-volant qu'on peint : celui de l'atelier choisi (les autres ateliers sont cachés).
        PaintableSurface peinture = FindFirstObjectByType<PaintableSurface>();
        if (peinture != null)
            peinture.Terminer();
    }
}
