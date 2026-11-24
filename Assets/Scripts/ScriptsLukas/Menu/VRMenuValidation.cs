using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.EventSystems;

public class VRMenuValidation : MonoBehaviour
{
    [SerializeField] private NearFarInteractor leftInteractor;
    [SerializeField] private NearFarInteractor rightInteractor;
    [SerializeField] private Button playButton;
    [SerializeField] private PlayButton menuManager;
    [SerializeField] private float validationTime = 1.5f;

    private InputAction leftTrigger;
    private InputAction rightTrigger;
    private NearFarInteractor activeInteractor;
    private Button selectedButton;
    private float timer;
    
    

    private void Awake()
    {
        leftTrigger = new InputAction("LeftTrigger", InputActionType.Button, "<XRController>{LeftHand}/triggerPressed");
        rightTrigger = new InputAction("RightTrigger", InputActionType.Button, "<XRController>{RightHand}/triggerPressed");
    }

    private void OnEnable()
    {
        leftTrigger.Enable();
        rightTrigger.Enable();
    }

    private void OnDisable()
    {
        leftTrigger.Disable();
        rightTrigger.Disable();
    }

   

    private void Update()
    {
        bool leftPressed = leftTrigger.IsPressed();
        bool rightPressed = rightTrigger.IsPressed();

        

        

       

        bool stillPressed = activeInteractor == leftInteractor ? leftPressed : rightPressed;

       

        

        if (timer >= 0.1)
        {
            Button completedButton = selectedButton;
            
            

            if (menuManager == null)
            {
                return;
            }

            if (completedButton == playButton)
            {
                menuManager.ChangeScene();
            }
           
        }

        
    }

    private Button GetButton(NearFarInteractor interactor)
    {
        if (interactor == null)
        {
            return null;
        }

        if (interactor.TryGetCurrentUIRaycastResult(out RaycastResult hit))
        {
            return hit.gameObject.GetComponentInParent<Button>();
        }

        return null;
    }

   

   

    private void OnDestroy()
    {
        leftTrigger.Dispose();
        rightTrigger.Dispose();
    }
}

