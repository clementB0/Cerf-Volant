using UnityEngine;
using UnityEngine.SceneManagement;


public class PlayButton : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SceneTestLukas";

    public void ChangeScene()
    {
        SceneManager.LoadScene(gameSceneName);
        
    }
}
