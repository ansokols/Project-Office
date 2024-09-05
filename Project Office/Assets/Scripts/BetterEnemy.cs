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
    [SerializeField] private float walkingSpeed;
    [SerializeField] private float runningSpeed;
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
    [SerializeField] private float minCooldownTime;
    [SerializeField] private float maxCooldownTime;
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

    private enum WalkingMode
    {
        Stop,
        Walk,
        Run
    }
    private WalkingMode currentWalkingMode;

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
            switch (currentEnemyState)
            {
                default:
                case EnemyState.Idle:
                    //currentTarget = new Vector3(12.1001f, 1.3247f, 10f);
                    //UpdateEnemyPath(currentTarget);
                    //Debug.Log("        " + agent.destination + " | " + currentTarget);
                    SeekPlayer();
                    break;

                case EnemyState.Search:
                    if(agent.remainingDistance <= agent.stoppingDistance) //done with path
                    {
                        ToggleWalkingMode(WalkingMode.Stop);
                        Vector3 point;
                        if (RandomPoint(currentTarget, instantDetectionDistance, out point)) //pass in our centre point and radius of area
                        {
                            Debug.DrawRay(point, Vector3.up, Color.blue, 1.0f); //so you can see with gizmos
                            UpdateEnemyPath(point);
                        }
                        while (true)
                        {
                            if (RotateTowardsTarget(point, 75f))
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        ToggleWalkingMode(WalkingMode.Walk);
                        RotateTowardsMovement(200f);
                    }
                    SeekPlayer();
                    break;

                case EnemyState.Battle:
                    //UpdateEnemyPath(currentTarget);

                    float distanceToPlayer = Vector2.Distance(transform.position, player.position);
                    Vector2 directionToPlayer = (player.position - transform.position).normalized;
                    /*
                    if(agent.pathStatus == NavMeshPathStatus.PathComplete)
                    {
                        Debug.Log("PathComplete");
                    }
                    if(agent.remainingDistance < 0.1)
                    {
                        Debug.Log("remainingDistance < 0.1");
                    }
                    if((Vector2)agent.destination == (Vector2)currentTarget)
                    {
                        Debug.Log("destination == currentTarget");
                    }
                    else
                    {
                        Debug.Log("        " + agent.destination + " | " + currentTarget);
                    }
                    */
                    if(agent.pathStatus == NavMeshPathStatus.PathComplete && agent.remainingDistance < 0.1 && (Vector2)agent.destination == (Vector2)currentTarget)
                    {
                        ToggleWalkingMode(WalkingMode.Stop);
                        currentEnemyState = EnemyState.Search;
                        Debug.Log("Stop");
                    }
                    else if (distanceToPlayer > stoppingDistance)
                    {
                        ToggleWalkingMode(WalkingMode.Run);

                        if (getCurrentVisionState(directionToPlayer, visionDistance) != VisionState.NotVisible)
                        {
                            currentTarget = player.position;
                            //UpdateEnemyPath(currentTarget);
                            RotateTowardsTarget(currentTarget, 200f);
                            Attack();
                            extraDetectionTimeDeadline = extraDetectionTime + Time.time;
                        }
                        else
                        {
                            RotateTowardsMovement(200f);
                            if (Time.time <= extraDetectionTimeDeadline)
                            {
                                currentTarget = player.position;
                                //UpdateEnemyPath(currentTarget);
                            }
                        }
                        UpdateEnemyPath(currentTarget);
                    }
                    else if (distanceToPlayer < retreatDistance)
                    {
                        ToggleWalkingMode(WalkingMode.Run);
                        
                        if (getCurrentVisionState(directionToPlayer, visionDistance) != VisionState.NotVisible)
                        {
                            currentTarget = player.position;
                            Vector2 target = (Vector2)transform.position - directionToPlayer * stoppingDistance;
                            UpdateEnemyPath(target);
                            RotateTowardsTarget(currentTarget, 200f);
                            Attack();
                            extraDetectionTimeDeadline = extraDetectionTime + Time.time;
                        }
                        else
                        {
                            RotateTowardsMovement(200f);

                            if (Time.time <= extraDetectionTimeDeadline)
                            {
                                currentTarget = player.position;
                                //UpdateEnemyPath(currentTarget);
                            }

                            UpdateEnemyPath(currentTarget);
                        }
                    }
                    else
                    {
                        if (getCurrentVisionState(directionToPlayer, visionDistance) != VisionState.NotVisible)
                        {
                            ToggleWalkingMode(WalkingMode.Stop);
                            currentTarget = player.position;
                            //UpdateEnemyPath(currentTarget);
                            RotateTowardsTarget(currentTarget, 200f);
                            Attack();
                            extraDetectionTimeDeadline = extraDetectionTime + Time.time;
                        }
                        else
                        { 
                            ToggleWalkingMode(WalkingMode.Run);
                            RotateTowardsMovement(200f);

                            if (Time.time <= extraDetectionTimeDeadline)
                            {
                                currentTarget = player.position;
                                //UpdateEnemyPath(currentTarget);
                            }
                        }
                        UpdateEnemyPath(currentTarget);
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

    private void ToggleWalkingMode(WalkingMode targetWalkingMode)
    {
        if (currentWalkingMode != targetWalkingMode)
        {
            currentWalkingMode = targetWalkingMode;
            switch (targetWalkingMode)
            {
                default:
                case WalkingMode.Stop:
                    agent.speed = 0;
                    
                    movementAudioSource.Stop();

                    bodyAnim.SetBool("isRunning", false);
                    legsAnim.SetInteger("walkingMode", 0);
                    break;

                case WalkingMode.Walk:
                    agent.speed = walkingSpeed;

                    movementAudioSource.Stop();
                    movementAudioSource.clip = walkingSFX;
                    movementAudioSource.volume = 0.125f;
                    movementAudioSource.Play();

                    bodyAnim.SetBool("isRunning", true);
                    legsAnim.SetInteger("walkingMode", 1);
                    break;

                case WalkingMode.Run:
                    agent.speed = runningSpeed;

                    movementAudioSource.Stop();
                    movementAudioSource.clip = runningSFX;
                    movementAudioSource.volume = 0.25f;
                    movementAudioSource.Play();

                    bodyAnim.SetBool("isRunning", true);
                    legsAnim.SetInteger("walkingMode", 2);
                    break;
            }
        }
    }

    private void SeekPlayer()
    {
        if (Vector2.Distance(transform.position, player.position) <= instantDetectionDistance)
        {
            currentTarget = player.position;
            UpdateEnemyPath(currentTarget);
            currentEnemyState = EnemyState.Battle;
        }

        else if (Vector2.Distance(transform.position, player.position) <= detectionDistance)
        {
            float detectionTimeDeadline = midPFOVdetectionTime * farPFOVdetectionTime;
            Vector2 directionToPlayer = (player.position - transform.position).normalized;

            VisionState currentVisionState = getCurrentVisionState(directionToPlayer, detectionDistance);
            switch (currentVisionState)
            {
                default:
                case VisionState.NotVisible:
                    if (detectionLevel >= detectionTimeDeadline)
                    {
                        if (RotateTowardsTarget(currentTarget, 50f))
                        {
                            currentEnemyState = EnemyState.Search;
                        }
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
                        if (RotateTowardsTarget(currentTarget, 50f))
                        {
                            currentEnemyState = EnemyState.Search;
                        }
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
                        if (RotateTowardsTarget(currentTarget, 50f))
                        {
                            currentEnemyState = EnemyState.Search;
                        }
                    }
                    else
                    {
                        detectionLevel += detectionTimeDeadline / farPFOVdetectionTime * Time.deltaTime;
                    }
                    break;
            }
        }
    }

    private void UpdateEnemyPath(Vector3 target)
    {
        if (Time.time >= pathUpdateDeadline) {
            pathUpdateDeadline = Time.time + pathUpdateDelay;
            agent.SetDestination(target);
        }
    }

    private bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        Vector3 randomPoint = center + Random.insideUnitSphere * range; //random point in a sphere 
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas)) //documentation: https://docs.unity3d.com/ScriptReference/AI.NavMesh.SamplePosition.html
        { 
            //the 1.0f is the max distance from the random point to a point on the navmesh, might want to increase if range is big
            //or add a for loop like in the documentation
            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
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

    private bool RotateTowardsMovement(float rotationSpeed)
    {

        float angle = Mathf.Atan2(agent.velocity.y, agent.velocity.x) * Mathf.Rad2Deg;
        //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(Vector3.forward * (angle)), 10 * Time.deltaTime);
        Quaternion targetRotation = Quaternion.Euler(new Vector3(0, 0, angle));
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        return transform.rotation == targetRotation;


        /*
        Vector3 direction = (agent.steeringTarget - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(0, 0, direction.z));
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        return transform.rotation == targetRotation;
        */
    }

    private bool RotateTowardsTarget(Vector3 target, float rotationSpeed)
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

        return transform.rotation == targetRotation;
    }

    private void Attack()
    {
        if (cooldown <= 0)
        {
            if (magOccupancy != 0)
            {
                //Shoot();
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

        cooldown = Random.Range(minCooldownTime, maxCooldownTime);
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
