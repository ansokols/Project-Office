using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PistolBullet : MonoBehaviour
{
    [Header("Objects")]
    public GameObject wallHitEffect;
    public GameObject enemyHitEffect;
    public GameObject enemyHitSprite;

    [Header("Characteristics")]
    public int damage;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Enemy"))
        {
            GameObject effect = Instantiate(enemyHitEffect, transform.position, Quaternion.identity  * Quaternion.Euler(0, 0, Random.Range(0f, 360f)));
            Instantiate(enemyHitSprite, effect.transform.position, effect.transform.rotation);
            collision.collider.transform.parent.GetComponent<Enemy>().TakeDamage(damage);
            Destroy(effect, 60f);
        }
        else
        {
            GameObject effect = Instantiate(wallHitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 20f);
        }
        Destroy(gameObject);
    }
}
