using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PlayButton : MonoBehaviour
{
    [Tooltip("Chemin ou nom d'une scène activée dans la liste de scènes du profil de build.")]
    [SerializeField] private string gameSceneName;
    [SerializeField] private Transform cutTab;
    [SerializeField] private TMP_Text playLabel;

    private bool cutValidated;

    // Appelé par l'événement de fin de découpe de PatronPointilles.
    public void ChangeScene()
    {
        if (cutValidated)
            return;

        cutValidated = true;
        if (playLabel != null)
            playLabel.color = new Color(0.76f, 1f, 0.81f);

        string destination = gameSceneName?.Trim();
        if (string.IsNullOrEmpty(destination))
        {
            Debug.LogError("Menu : renseignez Game Scene Name sur le composant Play Button.", this);
        }
        else if (!Application.CanStreamedLevelBeLoaded(destination))
        {
            Debug.LogError($"Menu : la scène « {destination} » doit être activée dans les profils de build.", this);
        }
        else
        {
            StartCoroutine(LoadAfterFeedback(destination));
        }
    }

    private IEnumerator LoadAfterFeedback(string destination)
    {
        const float duration = 0.35f;
        if (cutTab != null)
        {
            Vector3 start = cutTab.localPosition;
            Vector3 end = start + new Vector3(0f, -0.03f, -0.045f);
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                cutTab.localPosition = Vector3.Lerp(start, end, elapsed / duration);
                yield return null;
            }
            cutTab.localPosition = end;
        }
        else
        {
            yield return new WaitForSecondsRealtime(duration);
        }

        SceneManager.LoadSceneAsync(destination);
    }
}
