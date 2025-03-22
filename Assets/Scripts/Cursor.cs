using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cursor : MonoBehaviour
{
    private Camera cam;
    private Canvas canvas;

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        canvas = GetComponentInParent<Canvas>();
    }

    // Update is called once per frame
    public void LateUpdate()
    {
        canvas.transform.rotation = cam.transform.rotation;
    }
}
