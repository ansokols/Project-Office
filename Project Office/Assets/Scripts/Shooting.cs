using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Shooting : MonoBehaviour
{
    private Animator anim;
    [Header("Data")]
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
    public int magSize;
    public int ammoAmount;

    private float cooldown;
    private int magOccupancy;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shotSFX;
    public AudioClip drySFX;
    public AudioClip reloadSFX;
    public AudioClip shellsSFX;

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
                        audioSource.PlayOneShot(drySFX);
                    }
                }

            if (Input.GetKeyDown(KeyCode.R))
                {
                    if (magOccupancy != magSize && ammoAmount != 0)
                    {
                        Reload();
                    }
                }
        }
        else
        {
            cooldown -= Time.deltaTime;
        }
    }

    void Shoot()
    {
        audioSource.PlayOneShot(shotSFX);
        StartCoroutine(playSoundWithDelay(shellsSFX, 0.25f));
        anim.Play("Pistol.Shoot", 0, 0f);

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.AddForce(firePoint.right * bulletForce, ForceMode2D.Impulse);

        GameObject shell = Instantiate(shellPrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D rb2 = shell.GetComponent<Rigidbody2D>();
        rb2.AddForce(-firePoint.up * shellForce, ForceMode2D.Impulse);
        Destroy(shell, 20f);

        GameObject effect = Instantiate(flashEffect, firePoint.position, firePoint.rotation);
        Destroy(effect, 0.1f);

        cooldown = cooldownTime;
        magOccupancy -= 1;
        magDisplay.fillAmount -= 1.0f / (float)magSize;
    }

    void Reload()
    {
        audioSource.PlayOneShot(reloadSFX);
        anim.Play("Pistol.Reload", 0, 0f);

        ammoAmount += magOccupancy;
        magOccupancy = 0;
        if (ammoAmount > magSize) {
            magOccupancy = magSize;
            ammoAmount -= magSize;
        } else {
            magOccupancy = ammoAmount;
            ammoAmount = 0;
        }

        cooldown = reloadTime;
        ammoDisplay.text = ammoAmount.ToString();
        magDisplay.fillAmount = (float)magOccupancy / (float)magSize;
    }

    IEnumerator playSoundWithDelay(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.PlayOneShot(clip);
    }
}
