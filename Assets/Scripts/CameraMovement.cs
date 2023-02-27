using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public static CameraMovement instance;
    public float minX, maxX;
    public float minY, maxY;
    public float speed;
    void Start()
    {
        if (!instance)
        {
            instance = this;
        }
    }

    void Update()
    {
        float X = Input.GetAxis("Horizontal");
        float Y = Input.GetAxis("Vertical");
        Vector3 move = new Vector3(X * speed * Time.deltaTime, Y * speed * Time.deltaTime); 
        transform.Translate(move);   
    }
}
