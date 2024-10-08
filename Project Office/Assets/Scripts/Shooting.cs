using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Shooting : MonoBehaviour
{
    private Animator anim;

    [Header("Objects")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private GameObject flashEffect;
    [SerializeField] private Text ammoDisplay;
    [SerializeField] private Image magDisplay;

    [Header("Characteristics")]
    [SerializeField] private float bulletForce;
    [SerializeField] private float shellForce;
    [SerializeField] private float reloadTime;
    [SerializeField] private float cooldownTime;
    public float cooldown {get; set;}
    [SerializeField] private int magSize;
    [SerializeField] private int ammoAmount;
    private int magOccupancy;

    [Header("Audio")]
    [SerializeField] private AudioSource playerAudioSource;
    [SerializeField] private AudioSource interactionAudioSource;
    [field: SerializeField] public AudioSource shootingAudioSource {get; private set;}
    [SerializeField] private AudioClip shotSFX;
    [SerializeField] private AudioClip drySFX;
    [SerializeField] private AudioClip reloadSFX;
    [SerializeField] private AudioClip shellsSFX;
    [SerializeField] private AudioClip ammoPickupSFX;

    // Start is called before the first frame update
    void Start()
    {
        anim = transform.Find("Body").GetComponent<Animator>();
        ammoDisplay.text = ammoAmount.ToString();
        magDisplay.fillAmount = (float)magOccupancy / (float)magSize;
    }

    // Update is called once per frame
    void Update()
    {
        if (cooldown <= 0)
        {
            //if (Input.GetMouseButton(0)) //Full auto
            if (Input.GetMouseButtonDown(0))
                {
                    if (magOccupancy != 0)
                    {
                        Shoot();
                    }
                    else
                    {
                        playerAudioSource.PlayOneShot(drySFX, 0.3f);
                    }
                }

            if (Input.GetKeyDown(KeyCode.R))
                {
                    if (magOccupancy != magSize && ammoAmount != 0)
                    {
                        StartCoroutine(Reload());
                    }
                }
        }
        else
        {
            cooldown -= Time.deltaTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ammo"))
        {
            interactionAudioSource.PlayOneShot(ammoPickupSFX, 0.075f);
            ammoAmount += Random.Range(8, 12);
            ammoDisplay.text = ammoAmount.ToString();
            Destroy(other.gameObject);
        }
    }

    private void Shoot()
    {
        shootingAudioSource.PlayOneShot(shotSFX, 0.3f);
        StartCoroutine(PlaySoundWithDelay(shellsSFX, 0.5f, 0.25f));
        anim.Play("Pistol.Shoot", 0, 0f);

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.AddForce(firePoint.right * bulletForce, ForceMode2D.Impulse);

        GameObject shell = Instantiate(shellPrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D rb2 = shell.GetComponent<Rigidbody2D>();
        rb2.AddForce(-firePoint.up * shellForce, ForceMode2D.Impulse);
        Destroy(shell, 20f);

        GameObject effect = Instantiate(flashEffect, firePoint.position, firePoint.rotation);
        Destroy(effect, 0.05f);

        cooldown = cooldownTime;
        magOccupancy -= 1;
        magDisplay.fillAmount -= 1.0f / (float)magSize;
    }

    private IEnumerator Reload()
    {
        playerAudioSource.PlayOneShot(reloadSFX, 0.4f);
        anim.Play("Pistol.Reload", 0, 0f);

        cooldown = reloadTime;
        yield return new WaitForSeconds(reloadTime);

        ammoAmount += magOccupancy;
        magOccupancy = 0;
        if (ammoAmount > magSize)
        {
            magOccupancy = magSize;
            ammoAmount -= magSize;
        } else {
            magOccupancy = ammoAmount;
            ammoAmount = 0;
        }

        ammoDisplay.text = ammoAmount.ToString();
        magDisplay.fillAmount = (float)magOccupancy / (float)magSize;
    }

    private IEnumerator PlaySoundWithDelay(AudioClip clip, float volume, float delay)
    {
        yield return new WaitForSeconds(delay);
        playerAudioSource.PlayOneShot(clip, volume);
    }
}
