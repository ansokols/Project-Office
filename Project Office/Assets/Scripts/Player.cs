using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("Objects")]
    public Camera cam;
    public Image healthDisplay;
    public Text healDisplay;

    [Header("Characteristics")]
    public float speed;
    public float runningSpeed;
    public int maxHealth;
    public int health;
    public int healCapacity;
    public int healAmount;
    public int healImpact;
    public float healTime;

    [Header("Audio")]
    public AudioSource playerAudioSource;
    public AudioSource interactionAudioSource;
    public AudioClip healPickupSFX;
    public AudioClip healingSFX;

    private Vector2 moveInput;
    private Vector2 moveVelocity;
    private Vector2 mousePos;
    private Vector2 lookDir;
    private float lookAngle;
    
    private Rigidbody2D rb;
    private Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        healDisplay.text = healAmount.ToString();
        healthDisplay.fillAmount = (float)health / (float)maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<Shooting>().cooldown <= 0)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (healAmount > 0 && health != maxHealth)
                    {
                        StartCoroutine(Heal());
                    }
                }
        }

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Heal") && healAmount < healCapacity)
        {
            interactionAudioSource.PlayOneShot(healPickupSFX, 0.125f);
            healAmount += 1;
            healDisplay.text = healAmount.ToString();
            Destroy(other.gameObject);
        }
    }

    private IEnumerator Heal()
    {
        playerAudioSource.PlayOneShot(healingSFX, 0.25f);

        Shooting shooting = GetComponent<Shooting>();
        shooting.cooldown = healTime;
        yield return new WaitForSeconds(healTime);

        if (health < maxHealth - healImpact)
        {
            health = health + healImpact;
        } else {
            health = maxHealth;
        }
        healAmount -= 1;

        healDisplay.text = healAmount.ToString();
        healthDisplay.fillAmount = (float)health / (float)maxHealth;
    }
}
