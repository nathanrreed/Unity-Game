using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Camera cam;
    private Canvas canvas;

    public void Start()
    {
        cam = Camera.main;
        canvas = GetComponentInParent<Canvas>();
    }
    public void LateUpdate()
    {
        canvas.transform.rotation = cam.transform.rotation;
    }
}
