using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed;
    public float runningSpeed;

    private Vector2 moveInput;
    private Vector2 moveVelocity;
    private Vector2 mousePos;
    private Vector2 lookDir;
    private float lookAngle;
    
    private Rigidbody2D rb;
    private Animator anim;
    public Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        float deltaAngle = Vector2.SignedAngle(moveInput, lookDir);
        if (deltaAngle >= 60 && deltaAngle <= 120)
        {
            anim.SetLayerWeight(anim.GetLayerIndex("Left"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("Right"), 1);
        }
        else if (deltaAngle >= -120 && deltaAngle <= -60)
        {
            anim.SetLayerWeight(anim.GetLayerIndex("Left"), 1);
            anim.SetLayerWeight(anim.GetLayerIndex("Right"), 0);
        }
        else
        {
            anim.SetLayerWeight(anim.GetLayerIndex("Left"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("Right"), 0);
        }

        if (moveInput.x == 0 && moveInput.y == 0)
        {
            anim.SetInteger("walkingMode", 0);
            moveVelocity = moveInput.normalized * speed;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            anim.SetInteger("walkingMode", 2);
            moveVelocity = moveInput.normalized * runningSpeed;
        }
        else
        {
            anim.SetInteger("walkingMode", 1);
            moveVelocity = moveInput.normalized * speed;
        }

        mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveVelocity * Time.fixedDeltaTime);

        lookDir = mousePos - rb.position;
        lookAngle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        rb.rotation = lookAngle;
    }
}
