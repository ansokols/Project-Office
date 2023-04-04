using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PistolBullet : MonoBehaviour
{
    public GameObject wallHitEffect;
    public GameObject enemyHitEffect;
    public int damage;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Enemy"))
        {
            GameObject effect = Instantiate(enemyHitEffect, transform.position, Quaternion.identity);
            collision.collider.transform.parent.GetComponent<Enemy>().TakeDamage(damage);
            Destroy(effect, 60f);
            Destroy(gameObject);
        }
        else
        {
            GameObject effect = Instantiate(wallHitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 20f);
            Destroy(gameObject);
        }
    }
}
