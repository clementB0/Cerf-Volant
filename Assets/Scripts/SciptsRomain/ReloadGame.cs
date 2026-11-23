using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ReloadGame : MonoBehaviour
{
    [SerializeField]
    private InputActionReference buttonY;

    private void OnEnable()
    {
        buttonY.action.performed += OnButtonY;
        buttonY.action.Enable();
    }

    private void OnDisable()
    {
        buttonY.action.performed -= OnButtonY;
        buttonY.action.Disable();
    }

    private void OnButtonY(InputAction.CallbackContext context)
    {
        Debug.Log("Bouton Y appuyé !");

        // Ton action ici
        FaireAction();
    }

    private void FaireAction()
    {
        SceneManager.LoadScene("DecoupeTissu");
    }
}

