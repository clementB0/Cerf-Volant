using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
public class PaintReservoir : MonoBehaviour
{
    [SerializeField] private Color paintColor = Color.red;
    [SerializeField] private Renderer shell;
    [SerializeField] private Collider grabCollider;

    private XRGrabInteractable grab;
    private Rigidbody body;
    private MaterialPropertyBlock properties;
    private bool colorDirty;

    public Color PaintColor => paintColor;
    public XRGrabInteractable Grab => grab;
    public Collider GrabCollider => grabCollider;
    public bool OnRail { get; private set; }

    private void OnEnable()
    {
        ApplyColor();
        if (!Application.isPlaying)
            return;

        grab = GetComponent<XRGrabInteractable>();
        body = GetComponent<Rigidbody>();
        grab.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        if (grab != null)
            grab.selectExited.RemoveListener(OnReleased);
    }

    private void OnValidate()
    {
        colorDirty = true;
    }

    private void Update()
    {
        if (colorDirty)
            ApplyColor();
    }

    public void SetOnRail(bool attached)
    {
        OnRail = attached;
        grab.trackPosition = !attached;
        grab.trackRotation = !attached;
        grab.trackScale = !attached;
        body.isKinematic = attached || grab.isSelected;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        body.isKinematic = OnRail;
    }

    private void ApplyColor()
    {
        colorDirty = false;
        if (shell == null)
            return;

        properties ??= new MaterialPropertyBlock();
        shell.GetPropertyBlock(properties);
        properties.SetColor("_BaseColor", paintColor);
        properties.SetColor("_Color", paintColor);
        shell.SetPropertyBlock(properties);
    }
}
