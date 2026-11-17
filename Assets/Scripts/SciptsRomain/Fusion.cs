using UnityEngine;

public class Fusion : MonoBehaviour
{
    [SerializeField] Transform newParent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<CerfVolant>())
        {
            Destroy(gameObject);
            Destroy(newParent.gameObject);
        }
    }
}
