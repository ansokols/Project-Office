using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class BetterEnemy : MonoBehaviour
{
    [Header("Enemy Objects")]
    [SerializeField] private GameObject ammoBoxPrefab;
    [SerializeField] private GameObject healBoxPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private Image restartButton;
    [SerializeField] private Text restartText;
    [SerializeField] private Text winText;
    [SerializeField] private GameObject hud;

    [Header("Enemy Characteristics")]
    [SerializeField] private int health;
    [SerializeField] private float speed;
    [SerializeField] private float instantDetectionDistance;
    [SerializeField] private float detectionDistance;
    [SerializeField] private float visionDistance;
    [SerializeField] private float stoppingDistance;
    [SerializeField] private float retreatDistance;
    [Range(0,360)]
    [SerializeField] private float centralFOV;
    [Range(0,360)]
    [SerializeField] private float midPeripheralFOV;
    [Range(0,360)]
    [SerializeField] private float farPeripheralFOV;
    [Range(0, 1)]
    [SerializeField] private float healDropChance;

    [Header("Shooting Objects")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private GameObject flashEffect;

    [Header("Shooting Characteristics")]
    [SerializeField] private float bulletForce;
    [SerializeField] private float bulletSpread;
    [SerializeField] private float shellForce;
    [SerializeField] private float reloadTime;
    [SerializeField] private float cooldownTime;
    private float cooldown;
    [SerializeField] private int magSize;
    private int magOccupancy;

    [Header("Audio")]
    [SerializeField] private AudioSource movementAudioSource;
    [SerializeField] private AudioClip walkingSFX;
    [SerializeField] private AudioClip runningSFX;
    [SerializeField] private AudioSource playerAudioSource;
    [SerializeField] private AudioClip shotSFX;
    [SerializeField] private AudioClip reloadSFX;
    [SerializeField] private AudioClip shellsSFX;

    private Animator legsAnim;
    private Animator bodyAnim;
    private NavMeshAgent agent;
    private Vector3 currentTarget;

    private int enemiesLayer;
    private int layerMask;
  
    private static int enemyAmount;
    private int walkingMode;

    private float pathUpdateDelay;
    private float pathUpdateDeadline;
    private float detectionLevel;
    private float midPFOVdetectionTime;
    private float farPFOVdetectionTime;
    private float extraDetectionTime;
    private float extraDetectionTimeDeadline;

    private enum EnemyState
    {
        Idle,
        Patrol,
        Battle,
        Search
    }
    private EnemyState currentEnemyState;

    private enum VisionState
    {
        NotVisible,
        CentralFOV,
        MidPeripheralFOV,
        FarPeripheralFOV
    }

    // Start is called before the first frame update
    void Start()
    {
        currentEnemyState = EnemyState.Idle;

        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        pathUpdateDelay = 0.2f;
        extraDetectionTime = 0.5f;
        enemiesLayer = 8;
        layerMask = ~(1 << enemiesLayer);

        detectionLevel = 0f;
        midPFOVdetectionTime = 4f;
        farPFOVdetectionTime = 7f;

        legsAnim = GetComponent<Animator>();
        bodyAnim = transform.Find("Body").gameObject.GetComponent<Animator>();
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
            Vector2 directionToPlayer = (player.position - transform.position).normalized;

            switch (currentEnemyState)
            {
                default:
                case EnemyState.Idle:
                    if (Vector2.Distance(transform.position, player.position) <= instantDetectionDistance)
                    {
                        currentTarget = player.position;
                        UpdateEnemyPath(currentTarget);
                        currentEnemyState = EnemyState.Battle;
                        break;
                    }

                    if (Vector2.Distance(transform.position, player.position) <= detectionDistance)
                    {
                        Debug.Log(detectionLevel);
                        float detectionTimeDeadline = midPFOVdetectionTime * farPFOVdetectionTime;
                        VisionState currentVisionState = getCurrentVisionState(directionToPlayer, detectionDistance);
                        switch (currentVisionState)
                        {
                            default:
                            case VisionState.NotVisible:
                                if (detectionLevel >= detectionTimeDeadline)
                                {
                                    RotateTowardsTarget(currentTarget, 50f);
                                }
                                else if (detectionLevel > 0)
                                {
                                    detectionLevel -= (midPFOVdetectionTime + farPFOVdetectionTime) / 2 * Time.deltaTime;
                                }
                                break;

                            case VisionState.CentralFOV:
                                currentTarget = player.position;
                                UpdateEnemyPath(currentTarget);
                                currentEnemyState = EnemyState.Battle;
                                break;

                            case VisionState.MidPeripheralFOV:
                                if (detectionLevel >= detectionTimeDeadline)
                                {
                                    currentTarget = player.position;
                                    RotateTowardsTarget(currentTarget, 50f);
                                }
                                else
                                {
                                    detectionLevel += detectionTimeDeadline / midPFOVdetectionTime * Time.deltaTime;
                                }
                                break;

                            case VisionState.FarPeripheralFOV:
                                if (detectionLevel >= detectionTimeDeadline)
                                {
                                    currentTarget = player.position;
                                    RotateTowardsTarget(currentTarget, 50f);
                                }
                                else
                                {
                                    detectionLevel += detectionTimeDeadline / farPFOVdetectionTime * Time.deltaTime;
                                }
                                break;
                        }
                    }
                    break;

                case EnemyState.Battle:
                    agent.speed = speed;

                    if(agent.pathStatus == NavMeshPathStatus.PathComplete && agent.remainingDistance < 0.1 && agent.destination == currentTarget)
                    {
                        movementAudioSource.Stop();
                        walkingMode = 0;
                        currentEnemyState = EnemyState.Idle;

                        bodyAnim.SetBool("isRunning", false);
                        legsAnim.SetInteger("walkingMode", 0);
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
                            legsAnim.SetInteger("walkingMode", 2);
                        }

                        if (getCurrentVisionState(directionToPlayer, visionDistance) != VisionState.NotVisible)
                        {
                            currentTarget = player.position;
                            UpdateEnemyPath(currentTarget);
                            RotateTowardsTarget(currentTarget, 150f);
                            Attack();
                            extraDetectionTimeDeadline = extraDetectionTime + Time.time;
                        }
                        else
                        {
                            RotateTowardsMovement();
                            if (Time.time <= extraDetectionTimeDeadline)
                            {
                                currentTarget = player.position;
                                UpdateEnemyPath(currentTarget);
                            }
                        }
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
                            legsAnim.SetInteger("walkingMode", 1);
                        }
                        
                        if (getCurrentVisionState(directionToPlayer, visionDistance) != VisionState.NotVisible)
                        {
                            currentTarget = player.position;
                            Vector2 target = (Vector2)transform.position - directionToPlayer * stoppingDistance;
                            UpdateEnemyPath(target);
                            RotateTowardsTarget(currentTarget, 150f);
                            Attack();
                            extraDetectionTimeDeadline = extraDetectionTime + Time.time;
                        }
                        else
                        {
                            UpdateEnemyPath(currentTarget);
                            RotateTowardsMovement();
                            if (Time.time <= extraDetectionTimeDeadline)
                            {
                                currentTarget = player.position;
                                UpdateEnemyPath(currentTarget);
                            }
                        }
                    }
                    else
                    {
                        movementAudioSource.Stop();
                        walkingMode = 0;

                        bodyAnim.SetBool("isRunning", false);
                        legsAnim.SetInteger("walkingMode", 0);

                        if (getCurrentVisionState(directionToPlayer, visionDistance) != VisionState.NotVisible)
                        {
                            agent.speed = 0;
                            currentTarget = player.position;
                            UpdateEnemyPath(currentTarget);
                            RotateTowardsTarget(currentTarget, 150f);
                            Attack();
                            extraDetectionTimeDeadline = extraDetectionTime + Time.time;
                        }
                        else
                        { 
                            RotateTowardsMovement();
                            if (Time.time <= extraDetectionTimeDeadline)
                            {
                                currentTarget = player.position;
                                UpdateEnemyPath(currentTarget);
                            }
                        }
                    }
                    break;
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
/*
    void FixedUpdate()
    {
        if (walkingMode != 0)
        {
            transform.position = moveVector;
        }
    }
*/

    public void TakeDamage(int damage)
    {
        health -= damage;
    }

    private void UpdateEnemyPath(Vector3 target)
    {
        if (Time.time >= pathUpdateDeadline) {
            pathUpdateDeadline = Time.time + pathUpdateDelay;
            agent.SetDestination(target);
        }
    }

    private VisionState getCurrentVisionState(Vector2 directionToTarget, float distance)
    {
        //TODO добавить разные поля обзора в дебаг
        Vector2 viewAngle1 = DirectionFromAngle(transform.eulerAngles.z, -centralFOV / 2);
        Vector2 viewAngle2 = DirectionFromAngle(transform.eulerAngles.z, centralFOV / 2);
        Vector2 viewAngle3 = DirectionFromAngle(transform.eulerAngles.z, -midPeripheralFOV / 2);
        Vector2 viewAngle4 = DirectionFromAngle(transform.eulerAngles.z, midPeripheralFOV / 2);
        Vector2 viewAngle5 = DirectionFromAngle(transform.eulerAngles.z, -farPeripheralFOV / 2);
        Vector2 viewAngle6 = DirectionFromAngle(transform.eulerAngles.z, farPeripheralFOV / 2);
        Debug.DrawLine(transform.position, (Vector2)transform.position + viewAngle1 * distance, Color.magenta);
        Debug.DrawLine(transform.position, (Vector2)transform.position + viewAngle2 * distance, Color.magenta);
        Debug.DrawLine(transform.position, (Vector2)transform.position + viewAngle3 * distance, Color.cyan);
        Debug.DrawLine(transform.position, (Vector2)transform.position + viewAngle4 * distance, Color.cyan);
        Debug.DrawLine(transform.position, (Vector2)transform.position + viewAngle5 * distance, Color.blue);
        Debug.DrawLine(transform.position, (Vector2)transform.position + viewAngle6 * distance, Color.blue);

        float angleToTarget = Vector2.Angle(transform.right, directionToTarget);

        if (angleToTarget <= farPeripheralFOV / 2)
        {
            RaycastHit2D[] allHits = Physics2D.RaycastAll(transform.position, directionToTarget, distance, layerMask);

            //TODO чёрная / жёлтая / красная линия короче, чем нужно если Z != 10
            Debug.DrawLine(transform.position, (Vector2)transform.position + directionToTarget * distance, Color.yellow);
            for (int i = 0; i < allHits.Length; i++)
            {
                if (allHits[i].collider.tag == "Solid")
                {
                    Debug.DrawLine(transform.position, (Vector2)transform.position + directionToTarget * distance, Color.red);
                    Debug.DrawLine(transform.position, allHits[i].point, Color.yellow);
                    break;
                }
                if (allHits[i].collider.tag == "Player")
                {
                    Debug.DrawLine(transform.position, allHits[i].point, Color.green);
                    if (angleToTarget <= centralFOV / 2)
                    {
                        return VisionState.CentralFOV;
                    }
                    if (angleToTarget <= midPeripheralFOV / 2)
                    {
                        return VisionState.MidPeripheralFOV;
                    }
                    if (angleToTarget <= farPeripheralFOV / 2)
                    {
                        return VisionState.FarPeripheralFOV;
                    }
                }
            } 
        }

        Debug.DrawLine(transform.position, (Vector2)transform.position + directionToTarget * distance, Color.black);
        return VisionState.NotVisible;  
    }

    private void RotateTowardsMovement()
    {
        float angle = Mathf.Atan2(agent.velocity.y, agent.velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(Vector3.forward * (angle)), 10 * Time.deltaTime);
    }

    private void RotateTowardsTarget(Vector3 target, float rotationSpeed)
    {
        //Vector2 directionToTarget = (target - (Vector2)transform.position).normalized;
        //float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;       
        //transform.rotation = Quaternion.Euler(Vector3.forward * (angle));
        //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(Vector3.forward * (angle)), rotationSpeed * Time.deltaTime);
        //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.AngleAxis(angle, Vector3.forward), rotationSpeed * Time.deltaTime);
        
        //transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(Vector3.forward * (angle)), rotationSpeed * Time.deltaTime);
        //transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(player.transform.eulerAngles.y, player.transform.eulerAngles.x, 0), 40 * rotationSpeed * Time.deltaTime);

        //Quaternion rotation = Quaternion.LookRotation(directionToTarget);
        //transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 0.2f);

        float angle = Mathf.Atan2(target.y - transform.position.y, target.x - transform.position.x ) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(new Vector3(0, 0, angle));
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
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

    private Vector2 DirectionFromAngle(float eulerY, float angleInDegrees)
    {
        angleInDegrees -= eulerY - 90;

        return new Vector2(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
