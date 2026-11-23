using UnityEngine;
using UnityEngine.SceneManagement;


public class PlayButton : MonoBehaviour
{
    private void Start()
    {
        SceneManager.LoadScene("SceneTestLukas");
    }
}
