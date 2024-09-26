using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBattleState : EnemyState
{
    private float cooldown;
    private int magOccupancy;
    public float extraDetectionTime;
    private float extraDetectionDeadline;

    public EnemyBattleState(EnemyStateMachine enemyStateMachine, Enemy enemy, EnemyReferences enemyReferences) : base(enemyStateMachine, enemy, enemyReferences)
    {
        cooldown = 0;
        magOccupancy = enemy.magSize;
        extraDetectionTime = 0.5f;
        //magOccupancy = Random.Range(0, enemy.magSize + 1);
    }

    public override void Enter()
    {
        Debug.Log("Battle state [ENTER]");

        enemyReferences.vision.SetActive(true);
    }

    public override void Exit()
    {
        Debug.Log("Battle state [EXIT]");
    }

    public override void Update()
    {
        if(/*enemyReferences.agent.pathStatus == NavMeshPathStatus.PathComplete && */enemyReferences.agent.remainingDistance <= enemyReferences.agent.stoppingDistance)
        {
            enemy.enemyStateMachine.ChangeState(enemy.enemyDeepSearchState);
            return;
        }

        float distanceToPlayer = Vector2.Distance(enemy.transform.position, enemyReferences.player.position);
        Vector2 directionToPlayer = (enemyReferences.player.position - enemy.transform.position).normalized;

        if (distanceToPlayer > enemy.stoppingDistance)
        {
            enemy.ToggleMovementMode(Enums.MovementMode.Run);

            if (enemy.getCurrentVisionState(directionToPlayer, enemy.visionDistance) != Enums.VisionState.NotVisible)
            {
                enemy.currentTarget = enemyReferences.player.position;
                enemy.RotateTowardsTarget(enemy.currentTarget, 200f);
                Attack();
                extraDetectionDeadline = extraDetectionTime + Time.time;
            }
            else
            {
                enemy.RotateTowardsMovement(200f);
                if (Time.time <= extraDetectionDeadline)
                {
                    enemy.currentTarget = enemyReferences.player.position;
                }
            }

            enemy.UpdateEnemyPath(enemy.currentTarget);
        }
        else if (distanceToPlayer < enemy.retreatDistance)
        {
            enemy.ToggleMovementMode(Enums.MovementMode.Run);
            
            if (enemy.getCurrentVisionState(directionToPlayer, enemy.visionDistance) != Enums.VisionState.NotVisible)
            {
                enemy.currentTarget = enemyReferences.player.position;
                Vector2 target = (Vector2)enemy.transform.position - directionToPlayer * enemy.stoppingDistance;
                enemy.UpdateEnemyPath(target);
                enemy.RotateTowardsTarget(enemy.currentTarget, 200f);
                Attack();
                extraDetectionDeadline = extraDetectionTime + Time.time;
            }
            else
            {
                enemy.RotateTowardsMovement(200f);

                if (Time.time <= extraDetectionDeadline)
                {
                    enemy.currentTarget = enemyReferences.player.position;
                }

                enemy.UpdateEnemyPath(enemy.currentTarget);
            }
        }
        else
        {
            if (enemy.getCurrentVisionState(directionToPlayer, enemy.visionDistance) != Enums.VisionState.NotVisible)
            {
                enemy.ToggleMovementMode(Enums.MovementMode.Stop);
                enemy.currentTarget = enemyReferences.player.position;
                enemy.RotateTowardsTarget(enemy.currentTarget, 200f);
                Attack();
                extraDetectionDeadline = extraDetectionTime + Time.time;
            }
            else
            { 
                enemy.ToggleMovementMode(Enums.MovementMode.Run);
                enemy.RotateTowardsMovement(200f);

                if (Time.time <= extraDetectionDeadline)
                {
                    enemy.currentTarget = enemyReferences.player.position;
                }
            }
            enemy.UpdateEnemyPath(enemy.currentTarget);
        }
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
                enemy.StartCoroutine(Reload());
            }
        }
        else
        {
            cooldown -= Time.deltaTime;
        }
    }

    private void Shoot()
    {
        enemyReferences.playerAudioSource.PlayOneShot(enemyReferences.shotSFX, 0.3f);
        enemy.StartCoroutine(PlaySoundWithDelay(enemyReferences.shellsSFX, 0.5f, 0.25f));
        enemyReferences.bodyAnim.Play("Pistol.Shoot", 0, 0f);

        GameObject bullet = GameObject.Instantiate(enemyReferences.bulletPrefab, enemyReferences.firePoint.position, enemyReferences.firePoint.rotation);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        Vector3 dir = enemyReferences.firePoint.right + new Vector3(Random.Range(-enemy.bulletSpread, enemy.bulletSpread), Random.Range(-enemy.bulletSpread, enemy.bulletSpread), .0f);
        rb.AddForce(dir * enemy.bulletForce, ForceMode2D.Impulse);

        GameObject shell = GameObject.Instantiate(enemyReferences.shellPrefab, enemyReferences.firePoint.position, enemyReferences.firePoint.rotation);
        Rigidbody2D rb2 = shell.GetComponent<Rigidbody2D>();
        rb2.AddForce(-enemyReferences.firePoint.up * enemy.shellForce, ForceMode2D.Impulse);
        GameObject.Destroy(shell, 20f);


        GameObject effect = GameObject.Instantiate(enemyReferences.flashEffect, enemyReferences.firePoint.position, enemyReferences.firePoint.rotation);
        GameObject.Destroy(effect, 0.05f);

        cooldown = Random.Range(enemy.minCooldownTime, enemy.maxCooldownTime);
        magOccupancy -= 1;
    }

    private IEnumerator Reload()
    {
        enemyReferences.playerAudioSource.PlayOneShot(enemyReferences.reloadSFX, 0.5f);
        enemyReferences.bodyAnim.Play("Pistol.Reload", 0, 0f);

        cooldown = enemy.reloadTime;
        yield return new WaitForSeconds(enemy.reloadTime);

        magOccupancy = enemy.magSize;
    }

    private IEnumerator PlaySoundWithDelay(AudioClip clip, float volume, float delay)
    {
        yield return new WaitForSeconds(delay);
        enemyReferences.playerAudioSource.PlayOneShot(clip, volume);
    }
}
