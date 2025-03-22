using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    private int max;
    private Camera cam;
    private Canvas canvas;
    private Image health_image;
   
    public void Start()
    {
        health_image = GetComponentInChildren<Image>();
        cam = Camera.main;
        canvas = GetComponentInParent<Canvas>();
    }
    public void SetMaxHealth(int max_hp)
    {
        max = max_hp;
    }
    public void SetHealth(int hp)
    {
        health_image.fillAmount = ((float)hp / (float)max);
    }

    public void LateUpdate()
    {
        canvas.transform.rotation = cam.transform.rotation;
    }

    public void SetVisible(bool set)
    {
        canvas.enabled = set;
    }
}
