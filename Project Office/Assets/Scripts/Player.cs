using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private Camera cam;
    [SerializeField] private Image healthDisplay;
    [SerializeField] private Text healDisplay;
    [SerializeField] private Image restartButton;
    [SerializeField] private Text restartText;
    [SerializeField] private GameObject hud;

    [field: Header("Characteristics")]
    [field: SerializeField] public float speed {get; private set;}
    [field: SerializeField] public float runningSpeed {get; private set;}
    [SerializeField] private int maxHealth;
    [SerializeField] private int health;
    [SerializeField] private int healCapacity;
    [SerializeField] private int healAmount;
    [SerializeField] private int healImpact;
    [SerializeField] private float healTime;

    [Header("Audio")]
    [SerializeField] private AudioSource playerAudioSource;
    [SerializeField] private AudioSource interactionAudioSource;
    [SerializeField] private AudioSource movementAudioSource;
    [SerializeField] private AudioClip healPickupSFX;
    [SerializeField] private AudioClip healingSFX;
    [SerializeField] private AudioClip walkingSFX;
    [SerializeField] private AudioClip runningSFX;

    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 moveInput;
    private Vector2 moveVelocity;
    private Vector2 lookDir;
    private float deltaAngle;
    private int walkingMode;
    
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        healDisplay.text = healAmount.ToString();
        healthDisplay.fillAmount = (float)health / (float)maxHealth;
        restartButton.enabled = false;
        restartText.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        lookDir = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition) - rb.position;
        deltaAngle = Vector2.SignedAngle(moveInput, lookDir);

        if (GetComponent<Shooting>().cooldown <= 0)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (healAmount > 0 && health < maxHealth)
                    {
                        StartCoroutine(Heal());
                    }
                }
        }

        if (moveInput.x == 0 && moveInput.y == 0)
        {
            anim.SetInteger("movementMode", 0);
            moveVelocity = moveInput.normalized * speed;
            movementAudioSource.Stop();
            walkingMode = 0;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            anim.SetInteger("movementMode", 2);
            moveVelocity = moveInput.normalized * runningSpeed;
            if (walkingMode != 2)
            {
                movementAudioSource.Stop();
                walkingMode = 2;
                movementAudioSource.clip = runningSFX;
                movementAudioSource.volume = 0.25f;
                movementAudioSource.Play();
            }
        }
        else
        {
            anim.SetInteger("movementMode", 1);
            moveVelocity = moveInput.normalized * speed;
            if (walkingMode != 1)
            {
                walkingMode = 1;
                movementAudioSource.clip = walkingSFX;
                movementAudioSource.volume = 0.125f;
                movementAudioSource.Play();
            }
        }

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

        if (health <= 0)
        {
            Destroy(gameObject);
            restartButton.enabled = true;
            restartText.enabled = true;
            hud.SetActive(false); 
        }
    }

    void FixedUpdate()
    {
        rb.velocity = new Vector2(moveVelocity.x, moveVelocity.y);
        rb.rotation = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
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

    public void TakeDamage(int damage)
    {
        health -= damage;
        healthDisplay.fillAmount = (float)health / (float)maxHealth;
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
