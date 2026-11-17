using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.OpenXR.Input;

public class Pen : MonoBehaviour
{
    public Camera cam;
    public int totalXPixels = 1024;
    public int totalYPixels = 512;
    public int brushSize = 4;
    public Color brushColor;

    public Transform topLeftCorner;
    public Transform bottomRightCorner;
    public Transform point;


    public Material material;
    public Texture2D generatedTexture;

    Color[] colorMap;

    int xPixel = 0;
    int yPixel = 0;

    float xMult;
    float yMult;

    private void Start()
    {
        colorMap = new Color[totalXPixels * totalYPixels];
        generatedTexture = new Texture2D(totalXPixels, totalYPixels , TextureFormat.RGBA32, false);
        generatedTexture.filterMode = FilterMode.Point;
        material.SetTexture("_BaseMap", generatedTexture);

        xMult = totalXPixels / (bottomRightCorner.localPosition.x - topLeftCorner.localPosition.x);
        yMult = totalXPixels / (bottomRightCorner.localPosition.y - topLeftCorner.localPosition.y);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<DrawCanvas>())
        {
            CalculatePixel();
        }
        
    }


    void CalculatePixel()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit, 10f))
        {
            point.position = hit.point;
            xPixel = (int)((point.localPosition.x - topLeftCorner.localPosition.x) / xMult);
            xPixel = (int)((point.localPosition.y - topLeftCorner.localPosition.y) / yMult);
        }
       
    }

}
