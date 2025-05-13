using UnityEngine;

public class RotatorTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("RotatorTest2");
        //a comment//
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(new Vector3(0,1,0));
    }
}
