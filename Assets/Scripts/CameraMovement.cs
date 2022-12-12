using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public static CameraMovement instance;
    public float minX, maxX;
    public float minY, maxY;
    public float speed;
    bool secondWave;
    void Start()
    {
        if (!instance)
        {
            instance = this;
        }
        StartCoroutine(changeMovement());
    }

    void Update()
    {
        float X = Input.GetAxis("Horizontal");
        float Y = Input.GetAxis("Vertical");
        Vector3 move = new Vector3(X * speed * Time.deltaTime, Y * speed * Time.deltaTime); 
        transform.Translate(move);
        
        transform.position = new Vector3(Mathf.Clamp(transform.position.x, minX, maxX), Mathf.Clamp(transform.position.y, minY, maxY), this.transform.position.z); 
    }
    IEnumerator changeMovement()
    {
        yield return new WaitForSeconds(100f);
        secondWave = true;
        if(secondWave)
        maxX = 47;
        yield return new WaitForSeconds(170f);
        secondWave = false;
        maxX = 50;
        minY = -10;
        yield return new WaitForSeconds(160f);
    }
}
