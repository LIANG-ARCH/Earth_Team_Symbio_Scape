using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    Vector3 movement;
    public float moveSpeed;
    private bool isDragging;
    private void FixedUpdate()
    {
        if(isDragging)
            Move();
        ScrollMove();
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            isDragging = true;
        if (Input.GetMouseButtonUp(0))
            isDragging = false;
        GetMovement();
    }
    void GetMovement()
    {
        if(isDragging)
        {
            movement.x = -Input.GetAxisRaw("Mouse X");
            movement.y = -Input.GetAxisRaw("Mouse Y");
        }
        
    }
    void Move()
    {
        transform.Translate(movement*Time.fixedDeltaTime*moveSpeed);
    }
    void ScrollMove()
    {
        Camera.main.orthographicSize -= Input.mouseScrollDelta.y;

    }
}
