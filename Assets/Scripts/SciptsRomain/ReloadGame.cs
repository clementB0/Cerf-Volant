using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ReloadGame : MonoBehaviour
{
    [SerializeField]
    private InputActionReference buttonY;
    [SerializeField] private ReloadSmoke transition;

    private bool ownsAction;

    private void OnEnable()
    {
        var action = buttonY?.action;
        if (action == null)
            return;

        ownsAction = !action.enabled;
        action.performed += OnButtonY;
        action.Enable();
    }

    private void OnDisable()
    {
        var action = buttonY?.action;
        if (action == null)
            return;

        action.performed -= OnButtonY;
        if (ownsAction)
            action.Disable();
    }

    private void OnButtonY(InputAction.CallbackContext context)
    {
        if (transition != null)
            transition.Reload(gameObject.scene.buildIndex);
        else
            SceneManager.LoadScene("DecoupeTissu");
    }
}

