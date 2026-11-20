using UnityEngine;
using UnityEngine.Events;

public class Pen : MonoBehaviour
{
    //to communicate the CalculatePixel function from Draw.cs to Pen.cs : 
    //public Draw other = get a reference to the Draw.cs script
    //other.CalculatePixel(); = get the CalculatePixel function from the Draw.cs script (other is necessary)
    // EXCEPT IT DOESNT WORK FOR SOME REASON

    // on collision stay?

    public Draw _draw;
    
    public void OnCollisionEnter (Collision drawing)
    {
        Debug.Log("nul");
        if (drawing.gameObject.GetComponent<DrawCanvas>())
        {

            Debug.Log("if you collide with drawcanvas");
            // is working
            _draw.CalculatePixel();
            Debug.Log("CalculatePixel function");
            // is not working
            
        }

        if (drawing.gameObject.GetComponent<DrawCanvas>() != null)
        {
            Debug.Log("gameobject null");
        }

    }
    private void Start()
    {
        Debug.Log("start");
    }

}
