using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PlayButton : MonoBehaviour
{
    [Tooltip("Chemin ou nom d'une scène activée dans la liste de scènes du profil de build.")]
    [SerializeField] private string gameSceneName;
    [SerializeField] private CutCoupon coupon;
    [SerializeField] private TMP_Text playLabel;

    private bool cutValidated;

    // Appelé par l'événement de fin de découpe de PatronPointilles.
    public void ChangeScene()
    {
        if (cutValidated)
            return;

        cutValidated = true;
        if (playLabel != null)
            playLabel.color = new Color(0.02f, 0.35f, 0.26f);

        string destination = gameSceneName?.Trim();
        if (string.IsNullOrEmpty(destination))
        {
            Debug.LogError("Menu : renseignez Game Scene Name sur le composant Play Button.", this);
        }
        else if (!Application.CanStreamedLevelBeLoaded(destination))
        {
            Debug.LogError($"Menu : la scène « {destination} » doit être activée dans les profils de build.", this);
            destination = null;
        }
        StartCoroutine(LoadAfterFeedback(destination));
    }

    private IEnumerator LoadAfterFeedback(string destination)
    {
        if (coupon != null)
            yield return coupon.Separate();

        if (!string.IsNullOrEmpty(destination))
            SceneManager.LoadSceneAsync(destination);
    }
}
