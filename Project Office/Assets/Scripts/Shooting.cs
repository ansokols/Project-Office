using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Shooting : MonoBehaviour
{
    private Animator anim;

    [Header("Objects")]
    public Transform firePoint;
    public GameObject bulletPrefab;
    public GameObject shellPrefab;
    public GameObject flashEffect;
    public Text ammoDisplay;
    public Image magDisplay;

    [Header("Characteristics")]
    public float bulletForce;
    public float shellForce;
    public float reloadTime;
    public float cooldownTime;
    [HideInInspector]
    public float cooldown;
    public int magSize;
    public int ammoAmount;
    private int magOccupancy;

    [Header("Audio")]
    public AudioSource playerAudioSource;
    public AudioSource interactionAudioSource;
    public AudioClip shotSFX;
    public AudioClip drySFX;
    public AudioClip reloadSFX;
    public AudioClip shellsSFX;
    public AudioClip ammoPickupSFX;

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
                        playerAudioSource.PlayOneShot(drySFX, 0.4f);
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
        playerAudioSource.PlayOneShot(shotSFX, 0.3f);
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
        playerAudioSource.PlayOneShot(reloadSFX, 0.5f);
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
