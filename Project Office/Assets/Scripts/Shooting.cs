using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooting : MonoBehaviour
{
    private Animator anim;
    public Transform firePoint;
    public GameObject bulletPrefab;
    public GameObject flashEffect;
    public float bulletForce;

    public float reloadTime;
    public float cooldownTime;
    private float cooldown;
    public int clipSize;
    private int clipOccupancy;
    public int ammoAmount;

    public AudioSource audioSource;
    public AudioClip shotSFX;
    public AudioClip drySFX;
    public AudioClip reloadSFX;
    public AudioClip shellsSFX;

    // Start is called before the first frame update
    void Start()
    {
        anim = GameObject.Find("Body").GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (cooldown <= 0)
        {
            if (Input.GetMouseButtonDown(0))
                {
                    if (clipOccupancy != 0)
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
                    if (clipOccupancy != clipSize && ammoAmount != 0)
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
        audioSource.PlayOneShot(shellsSFX);
        anim.Play("Pistol.Shoot", 0, 0f);

        GameObject effect = Instantiate(flashEffect, firePoint.position, firePoint.rotation);
        Destroy(effect, 0.1f);

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.AddForce(firePoint.right * bulletForce, ForceMode2D.Impulse);

        cooldown = cooldownTime;
        clipOccupancy -= 1;
    }

    void Reload()
    {
        audioSource.PlayOneShot(reloadSFX);
        anim.Play("Pistol.Reload", 0, 0f);

        ammoAmount += clipOccupancy;
        clipOccupancy = 0;
        if (ammoAmount > clipSize) {
            clipOccupancy = clipSize;
            ammoAmount -= clipSize;
        } else {
            clipOccupancy = ammoAmount;
            ammoAmount = 0;
        }

        cooldown = reloadTime;
    }
}
