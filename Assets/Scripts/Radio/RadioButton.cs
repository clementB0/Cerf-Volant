using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class RadioButton : MonoBehaviour
{
    private enum Command { Previous, PlayPause, Next }
    [SerializeField] private Command command;
    [SerializeField] private Transform cap;
    [SerializeField] private Vector3 pressDirection = Vector3.down;
    private RadioPlayer player;
    private XRSimpleInteractable button;
    private Vector3 rest;
    private float releaseTime;

    private void Awake()
    {
        player = GetComponentInParent<RadioPlayer>();
        button = GetComponent<XRSimpleInteractable>();
        rest = cap.localPosition;
    }

    private void OnEnable() => button.firstSelectEntered.AddListener(Press);

    private void OnDisable()
    {
        button.firstSelectEntered.RemoveListener(Press);
        cap.localPosition = rest;
    }

    private void Press(SelectEnterEventArgs args)
    {
        cap.localPosition = rest + pressDirection.normalized * 0.006f;
        releaseTime = Time.time + 0.12f;
        if (command == Command.PlayPause)
            player.Toggle();
        else
            player.Skip(command == Command.Next ? 1 : -1);
    }

    private void Update()
    {
        if (!button.isSelected && Time.time >= releaseTime)
            cap.localPosition = rest;
    }
}
