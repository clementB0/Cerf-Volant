using UnityEngine;
using UnityEngine.Events;

public class Pen : MonoBehaviour
{
    //to communicate the CalculatePixel function from Draw.cs to Pen.cs : 
    //public Draw other = get a reference to the Draw.cs script
    //other.CalculatePixel(); = get the CalculatePixel function from the Draw.cs script (other is necessary)
    public Draw _draw;
    public void OnCollisionEnter(Collision drawing)
    {
        Debug.Log("nul");
        if (drawing.gameObject.GetComponent<DrawCanvas>())
        {
            
            _draw.CalculatePixel();
            Debug.Log("CalculatePixel function");
            
        }

    }
    private void Start()
    {
        Debug.Log("start");
    }

}
