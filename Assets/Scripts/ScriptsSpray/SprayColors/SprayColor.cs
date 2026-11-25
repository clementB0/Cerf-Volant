using UnityEngine;

public class SprayColor : MonoBehaviour
{
    [Header("Couleur de la peinture")]
    [Tooltip("Couleur utilisée par le jet et les nouvelles traces.")]
    [SerializeField]
    private Color paintColor = Color.red;

    [SerializeField] private Renderer reservoir;

    [SerializeField] private ReservoirSocket reservoirSocket;

    public Color PaintColor => reservoirSocket != null && reservoirSocket.LockedReservoir != null
        ? reservoirSocket.LockedReservoir.PaintColor
        : paintColor;

    private void Awake()
    {
        if (reservoir == null)
            return;

        var properties = new MaterialPropertyBlock();
        reservoir.GetPropertyBlock(properties);
        properties.SetColor("_BaseColor", paintColor);
        properties.SetColor("_Color", paintColor);
        reservoir.SetPropertyBlock(properties);
    }
}
