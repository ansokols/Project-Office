using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private Transform target;

    [Header("Characteristics")]
    [Range(0, 5)]
    [SerializeField] private float followSpeed;
    [SerializeField] private Vector3 positionOffset;
    [SerializeField] private float zoomMin;
    [SerializeField] private float zoomMax;
    [SerializeField] private float scrollSpeed;

    // Update is called once per frame
    void Update()
    {
        zoom(Input.GetAxis("Mouse ScrollWheel") * scrollSpeed);
    }

    void FixedUpdate()
    {
        if (target != null)
        {
            Vector3 newPos = target.position + positionOffset;
            transform.position = Vector3.Slerp(transform.position, newPos, followSpeed * Time.deltaTime);
        }
    }

    private void zoom(float increment)
    {
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - increment, zoomMin, zoomMax);
    }
}