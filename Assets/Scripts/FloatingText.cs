using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// https://www.youtube.com/watch?v=ysg9oaZEgwc&ab_channel=JonBednez
public class FloatingText : MonoBehaviour
{
    Transform camera;
    Transform unit;
    Transform worldSpaceCanvas;

    public Vector3 offset;

    void Start()
    {
        camera = Camera.main.transform;
        unit = transform.parent;
        worldSpaceCanvas = GameObject.FindAnyObjectByType<Canvas>().transform;

        transform.SetParent(worldSpaceCanvas);
    }

    // Update is called once per frame
    void Update()
    {
        // look at camera
        transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position);
        transform.position = transform.position + offset;
    }
}
