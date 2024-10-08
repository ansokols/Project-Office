using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PistolBullet : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private GameObject wallHitEffect;
    [SerializeField] private GameObject enemyHitEffect;
    [SerializeField] private GameObject enemyHitSprite;

    [Header("Characteristics")]
    [SerializeField] private int damage;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Enemy"))
        {
            GameObject effect = Instantiate(enemyHitEffect, transform.position, Quaternion.identity  * Quaternion.Euler(0, 0, Random.Range(0f, 360f)));
            Instantiate(enemyHitSprite, effect.transform.position, effect.transform.rotation);
            collision.collider.transform.parent.GetComponent<Enemy>().TakeDamage(damage);
            Destroy(effect, 60f);
        }
        else if (collision.collider.CompareTag("Player"))
        {
            GameObject effect = Instantiate(enemyHitEffect, transform.position, Quaternion.identity  * Quaternion.Euler(0, 0, Random.Range(0f, 360f)));
            Instantiate(enemyHitSprite, effect.transform.position, effect.transform.rotation);
            collision.collider.transform.parent.GetComponent<Player>().TakeDamage(damage);
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
