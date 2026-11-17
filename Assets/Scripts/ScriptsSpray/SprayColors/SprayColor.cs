using UnityEngine;

public class SprayColor : MonoBehaviour
{
    [Header("Couleur de la peinture")]
    [Tooltip("Couleur utilisée par le jet et les nouvelles traces.")]
    [SerializeField]
    private Color paintColor = Color.red;

    public Color PaintColor => paintColor;
}