using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Objects")]
    public GameObject ammoBoxPrefab;
    public GameObject healBoxPrefab;

    [Header("Characteristics")]
    public int health;
    public float speed;
    [Range(0, 1)]
    public float healDropChance;

    // Update is called once per frame
    void Update()
    {
        if (health <= 0)
        {
            Instantiate(ammoBoxPrefab, transform.position + new Vector3(-0.75f, 0, 0), transform.rotation * Quaternion.Euler(0, 0, 60));

            if(Random.Range(0f, 1f) <= healDropChance)
            {
                Instantiate(healBoxPrefab, transform.position + new Vector3(0.75f, 0, 0), transform.rotation * Quaternion.Euler(0, 0, 120));
            }

            Destroy(gameObject);
        }

        // TODO: код для определения игрока, передвижения в сторону игрока и стрельбы.
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
    }
}
