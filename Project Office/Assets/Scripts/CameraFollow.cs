using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Objects")]
    public Transform target;

    [Header("Characteristics")]
    [Range(0, 5)]
    public float followSpeed;
    public Vector3 positionOffset;
    public float zoomMin;
    public float zoomMax;
    public float scrollSpeed;

    // Update is called once per frame
    void Update()
    {
        zoom(Input.GetAxis("Mouse ScrollWheel") * scrollSpeed);
    }

    void FixedUpdate()
    {
        Vector3 newPos = target.position + positionOffset;
        transform.position = Vector3.Slerp(transform.position, newPos, followSpeed * Time.deltaTime);
    }

    private void zoom(float increment)
    {
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - increment, zoomMin, zoomMax);
    }
}