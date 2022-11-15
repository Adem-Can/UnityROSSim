using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class printmat : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Camera camera = GetComponent<Camera>();

        Matrix4x4 viewproj = camera.previousViewProjectionMatrix;

        Debug.Log(viewproj.ToString());


    }
}
