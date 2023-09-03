using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Objects")]
    public GameObject ammoBoxPrefab;
    public GameObject healBoxPrefab;
    public Transform player;
    public Image restartButton;
    public Text restartText;
    public Text winText;
    public GameObject hud;

    [Header("Enemy Characteristics")]
    public int health;
    public float speed;
    public float visionDistance;
    public float stoppingDistance;
    public float retreatDistance;
    [Range(0, 1)]
    public float healDropChance;

    [Header("Shooting Objects")]
    public Transform firePoint;
    public GameObject bulletPrefab;
    public GameObject shellPrefab;
    public GameObject flashEffect;

    [Header("Shooting Characteristics")]
    public float bulletForce;
    public float bulletSpread;
    public float shellForce;
    public float reloadTime;
    public float cooldownTime;
    [HideInInspector] public float cooldown;
    public int magSize;
    private int magOccupancy;

    [Header("Audio")]
    public AudioSource movementAudioSource;
    public AudioClip walkingSFX;
    public AudioClip runningSFX;
    public AudioSource playerAudioSource;
    public AudioClip shotSFX;
    public AudioClip reloadSFX;
    public AudioClip shellsSFX;

    [field: SerializeField, Header("Raycast")] private float rotationSpeed;
    private Transform raycast;
    private RaycastHit2D[] allHits;

    private Animator feetAnim;
    private Animator bodyAnim;
    private Vector2 moveVector;
    private int walkingMode;
    private bool isPlayerDetected;
    private static int enemyAmount; 

    // Start is called before the first frame update
    void Start()
    {
        feetAnim = GetComponent<Animator>();
        bodyAnim = transform.Find("Body").gameObject.GetComponent<Animator>();
        raycast = transform.Find("Raycast").gameObject.GetComponent<Transform>();
        magOccupancy = Random.Range(0, magSize + 1);

        enemyAmount = 14;
        restartButton.enabled = false;
        restartText.enabled = false;
        winText.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (player != null)
        {
            if (!isPlayerDetected)
            {
                raycast.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
                allHits = Physics2D.RaycastAll(raycast.position, raycast.right, visionDistance);
                for (int i = 1; i < allHits.Length; i++)
                {
                    if (allHits[i].collider.tag == "Solid")
                    {
                        Debug.DrawLine(raycast.position, allHits[i].point, Color.yellow);
                        break;
                    }
                    else if (allHits[i].collider.tag == "Player")
                    {
                        Debug.DrawLine(raycast.position, allHits[i].point, Color.green);
                        isPlayerDetected = true;
                        break;
                    }
                    else
                    {
                        Debug.DrawLine(raycast.position, raycast.position + raycast.right * visionDistance, Color.red);
                    }
                }
            }
            else
            {
                if (Vector2.Distance(transform.position, player.position) > visionDistance)
                {
                    movementAudioSource.Stop();
                    walkingMode = 0;
                    isPlayerDetected = false;

                    bodyAnim.SetBool("isRunning", false);
                    feetAnim.SetInteger("walkingMode", 0);
                }
                else if (Vector2.Distance(transform.position, player.position) > stoppingDistance)
                {
                    if (walkingMode != 2)
                    {
                        movementAudioSource.Stop();
                        walkingMode = 2;
                        movementAudioSource.clip = runningSFX;
                        movementAudioSource.volume = 0.25f;
                        movementAudioSource.Play();

                        bodyAnim.SetBool("isRunning", true);
                        feetAnim.SetInteger("walkingMode", 2);
                    }

                    moveVector = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);
                    RotateTowardsTarget();
                    Attack();
                }
                else if (Vector2.Distance(transform.position, player.position) < retreatDistance)
                {
                    if (walkingMode != 1)
                    {
                        movementAudioSource.Stop();
                        walkingMode = 1;
                        movementAudioSource.clip = walkingSFX;
                        movementAudioSource.volume = 0.125f;
                        movementAudioSource.Play();

                        bodyAnim.SetBool("isRunning", true);
                        feetAnim.SetInteger("walkingMode", 1);
                    }

                    moveVector = Vector2.MoveTowards(transform.position, player.position, -speed/2 * Time.deltaTime);
                    RotateTowardsTarget();
                    Attack();
                }
                else
                {
                    movementAudioSource.Stop();
                    walkingMode = 0;

                    bodyAnim.SetBool("isRunning", false);
                    feetAnim.SetInteger("walkingMode", 0);

                    RotateTowardsTarget();
                    Attack();
                }
            }

            if (health <= 0)
            {
                Instantiate(ammoBoxPrefab, transform.position + new Vector3(-0.75f, 0, 0), transform.rotation * Quaternion.Euler(0, 0, 60));

                if(Random.Range(0f, 1f) <= healDropChance)
                {
                    Instantiate(healBoxPrefab, transform.position + new Vector3(0.75f, 0, 0), transform.rotation * Quaternion.Euler(0, 0, 120));
                }

                enemyAmount -= 1;
                if (enemyAmount <= 0)
                {
                    restartButton.enabled = true;
                    restartText.enabled = true;
                    winText.enabled = true;
                    hud.SetActive(false);
                }
                Destroy(gameObject);
            }
        }
    }

    void FixedUpdate()
    {
        if (walkingMode != 0)
        {
            transform.position = moveVector;
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
    }

    private void RotateTowardsTarget()
    {
        Vector2 direction = player.position - transform.position;
        direction.Normalize();
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;       
        transform.rotation = Quaternion.Euler(Vector3.forward * (angle));
    }

    private void Attack()
    {
        if (cooldown <= 0)
        {
            if (magOccupancy != 0)
            {
                Shoot();
            }
            else
            {
                StartCoroutine(Reload());
            }
        }
        else
        {
            cooldown -= Time.deltaTime;
        }
    }

    private void Shoot()
    {
        playerAudioSource.PlayOneShot(shotSFX, 0.3f);
        StartCoroutine(PlaySoundWithDelay(shellsSFX, 0.5f, 0.25f));
        bodyAnim.Play("Pistol.Shoot", 0, 0f);

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        Vector3 dir = firePoint.right + new Vector3(Random.Range(-bulletSpread, bulletSpread), Random.Range(-bulletSpread, bulletSpread), .0f);
        rb.AddForce(dir * bulletForce, ForceMode2D.Impulse);

        GameObject shell = Instantiate(shellPrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D rb2 = shell.GetComponent<Rigidbody2D>();
        rb2.AddForce(-firePoint.up * shellForce, ForceMode2D.Impulse);
        Destroy(shell, 20f);


        GameObject effect = Instantiate(flashEffect, firePoint.position, firePoint.rotation);
        Destroy(effect, 0.05f);

        cooldown = cooldownTime;
        magOccupancy -= 1;
    }

    private IEnumerator Reload()
    {
        playerAudioSource.PlayOneShot(reloadSFX, 0.5f);
        bodyAnim.Play("Pistol.Reload", 0, 0f);

        cooldown = reloadTime;
        yield return new WaitForSeconds(reloadTime);

        magOccupancy = magSize;
    }

    private IEnumerator PlaySoundWithDelay(AudioClip clip, float volume, float delay)
    {
        yield return new WaitForSeconds(delay);
        playerAudioSource.PlayOneShot(clip, volume);
    }
}
