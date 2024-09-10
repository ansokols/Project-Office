using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class BetterEnemy : MonoBehaviour
{
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

    private EnemyReferences enemyReferences;

    private static int enemyAmount;

    private Vector3 currentTarget;
    private Vector3 home;
    private int enemiesLayer;
    private int layerMask;

    private float pathUpdateDelay;
    private float pathUpdateDeadline;
    private float midPFOVdetectionTime;
    private float farPFOVdetectionTime;
    private float extraDetectionTime;
    private float extraDetectionDeadline;
    private float movementPauseDeadline;

    private float detectionLevel;
    private float searchTime;
    private bool isTurned;

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

    private enum MovementMode
    {
        Stop,
        Walk,
        Run
    }
    private MovementMode currentMovementMode;

    void Awake()
    {
        enemyReferences = GetComponent<EnemyReferences>();
    }

    // Start is called before the first frame update
    void Start()
    {
        currentEnemyState = EnemyState.Idle;
        isTurned = false;

        enemyReferences.agent.updateRotation = false;
        enemyReferences.agent.updateUpAxis = false;
        pathUpdateDelay = 0.2f;
        extraDetectionTime = 0.5f;
        enemiesLayer = 8;
        layerMask = ~(1 << enemiesLayer);

        home = transform.position;
        detectionLevel = 0f;
        searchTime = 0f;
        midPFOVdetectionTime = 4f;
        farPFOVdetectionTime = 7f;

        magOccupancy = Random.Range(0, magSize + 1);

        enemyAmount = 14;
        enemyReferences.restartButton.enabled = false;
        enemyReferences.restartText.enabled = false;
        enemyReferences.winText.enabled = false;
        enemyReferences.hud.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (enemyReferences.player != null)
        {
            switch (currentEnemyState)
            {
                default:
                case EnemyState.Idle:
                    /*currentTarget = new Vector3(12.1001f, 1.3247f, 10f);
                    UpdateEnemyPath(currentTarget);
                    //enemyReferences.agent.speed = 1f;
                    ToggleMovementMode(MovementMode.Run);
                    RotateTowardsMovement(100f);
                    
                    //Debug.Log("        " + enemyReferences.agent.destination + " | " + currentTarget);*/
                    SeekPlayer();
                    break;

                case EnemyState.Search:
                    if (searchTime <= 0)
                    {
                        UpdateEnemyPath(home);
                        ToggleMovementMode(MovementMode.Walk);
                        RotateTowardsMovement(150f);
                        SeekPlayer();

                        if (enemyReferences.agent.remainingDistance <= enemyReferences.agent.stoppingDistance)
                        {
                            currentEnemyState = EnemyState.Idle;
                            ToggleMovementMode(MovementMode.Stop);
                            Debug.Log("Search --> Idle");
                        }
                        break;
                    }

                    if (enemyReferences.agent.remainingDistance <= enemyReferences.agent.stoppingDistance) //done with path
                    {
                        //if (Random.value < 0.5f)
                        //{
                            isTurned = false;
                            ToggleMovementMode(MovementMode.Stop);
                            Vector3 point;
                            if (RandomPoint(currentTarget, instantDetectionDistance, out point)) //pass in our centre point and radius of area
                            {
                                Debug.DrawRay(point, Vector3.up, Color.blue, 1.0f); //so you can see with gizmos
                                UpdateEnemyPath(point);
                            }
                            /*
                            while (true)
                            {
                                if (RotateTowardsTarget(point, 75f))
                                {
                                    break;
                                }
                            }
                            */
                            enemyReferences.agent.speed = 0.01f;
                            //Debug.Log("EnemyState.Search: " + "Path calculated");
                        //}
                    }
                    else
                    {
                        if (isTurned)
                        {
                            //if (Time.time >= movementPauseDeadline)
                            //{
                                ToggleMovementMode(MovementMode.Walk);
                                RotateTowardsMovement(150f);
                            //}
                            //Debug.Log("EnemyState.Search: " + "IF1");
                        }
                        else if (RotateTowardsMovement(100f))
                        {
                            //if (Time.time >= movementPauseDeadline)
                            //{
                                //enemyReferences.agent.speed = walkingSpeed;
                                isTurned = true;
                                //Debug.Log("EnemyState.Search: " + "IF2");
                            //}
                            //Debug.Log("EnemyState.Search: " + "IF3");
                        }
                        //else
                        //{
                        //    movementPauseDeadline = Time.time + 2f;
                        //    Debug.Log("EnemyState.Search: " + "IF4");
                        //}
                    }
                    SeekPlayer();
                    searchTime -= Time.deltaTime;
                    break;

                case EnemyState.Battle:
                    //UpdateEnemyPath(currentTarget);

                    float distanceToPlayer = Vector2.Distance(transform.position, enemyReferences.player.position);
                    Vector2 directionToPlayer = (enemyReferences.player.position - transform.position).normalized;
                    
                    /*
                    if(enemyReferences.agent.pathStatus == NavMeshPathStatus.PathComplete)
                    {
                        Debug.Log("PathComplete");
                    }
                    if(enemyReferences.agent.remainingDistance < 0.1)
                    {
                        Debug.Log("remainingDistance < 0.1");
                    }
                    */
                    /*
                    if((Vector2)enemyReferences.agent.destination == (Vector2)currentTarget)
                    {
                        Debug.Log("destination == currentTarget");
                    }
                    else
                    {
                        Debug.Log("        " + enemyReferences.agent.destination + " | " + currentTarget);
                    }
                    */
                    /*
                    if(enemyReferences.agent.remainingDistance <= enemyReferences.agent.stoppingDistance)
                    {
                        Debug.Log("enemyReferences.agent.remainingDistance <= enemyReferences.agent.stoppingDistance");
                    }
                    else
                    {
                        Debug.Log("        " + enemyReferences.agent.remainingDistance + " | " + enemyReferences.agent.stoppingDistance);
                    }
                    */
                    
                    if(/*enemyReferences.agent.pathStatus == NavMeshPathStatus.PathComplete && */enemyReferences.agent.remainingDistance <= enemyReferences.agent.stoppingDistance)
                    {
                        ToggleMovementMode(MovementMode.Stop);
                        currentEnemyState = EnemyState.Search;
                        //Reset();
                        Debug.Log("Battle --> Search");
                    }
                    else if (distanceToPlayer > stoppingDistance)
                    {
                        ToggleMovementMode(MovementMode.Run);

                        if (getCurrentVisionState(directionToPlayer, visionDistance) != VisionState.NotVisible)
                        {
                            currentTarget = enemyReferences.player.position;
                            //UpdateEnemyPath(currentTarget);
                            RotateTowardsTarget(currentTarget, 200f);
                            Attack();
                            extraDetectionDeadline = extraDetectionTime + Time.time;
                        }
                        else
                        {
                            RotateTowardsMovement(200f);
                            if (Time.time <= extraDetectionDeadline)
                            {
                                currentTarget = enemyReferences.player.position;
                                //UpdateEnemyPath(currentTarget);
                            }
                        }
                        UpdateEnemyPath(currentTarget);
                    }
                    else if (distanceToPlayer < retreatDistance)
                    {
                        ToggleMovementMode(MovementMode.Run);
                        
                        if (getCurrentVisionState(directionToPlayer, visionDistance) != VisionState.NotVisible)
                        {
                            currentTarget = enemyReferences.player.position;
                            Vector2 target = (Vector2)transform.position - directionToPlayer * stoppingDistance;
                            UpdateEnemyPath(target);
                            RotateTowardsTarget(currentTarget, 200f);
                            Attack();
                            extraDetectionDeadline = extraDetectionTime + Time.time;
                        }
                        else
                        {
                            RotateTowardsMovement(200f);

                            if (Time.time <= extraDetectionDeadline)
                            {
                                currentTarget = enemyReferences.player.position;
                                //UpdateEnemyPath(currentTarget);
                            }

                            UpdateEnemyPath(currentTarget);
                        }
                    }
                    else
                    {
                        if (getCurrentVisionState(directionToPlayer, visionDistance) != VisionState.NotVisible)
                        {
                            ToggleMovementMode(MovementMode.Stop);
                            currentTarget = enemyReferences.player.position;
                            //UpdateEnemyPath(currentTarget);
                            RotateTowardsTarget(currentTarget, 200f);
                            Attack();
                            extraDetectionDeadline = extraDetectionTime + Time.time;
                        }
                        else
                        { 
                            ToggleMovementMode(MovementMode.Run);
                            RotateTowardsMovement(200f);

                            if (Time.time <= extraDetectionDeadline)
                            {
                                currentTarget = enemyReferences.player.position;
                                //UpdateEnemyPath(currentTarget);
                            }
                        }
                        UpdateEnemyPath(currentTarget);
                    }
                    break;
            }

            if (health <= 0)
            {
                Instantiate(enemyReferences.ammoBoxPrefab, transform.position + new Vector3(-0.75f, 0, 0), transform.rotation * Quaternion.Euler(0, 0, 60));

                if(Random.Range(0f, 1f) <= healDropChance)
                {
                    Instantiate(enemyReferences.healBoxPrefab, transform.position + new Vector3(0.75f, 0, 0), transform.rotation * Quaternion.Euler(0, 0, 120));
                }

                enemyAmount -= 1;
                if (enemyAmount <= 0)
                {
                    enemyReferences.restartButton.gameObject.SetActive(true);
                    enemyReferences.restartButton.enabled = true;
                    enemyReferences.restartText.enabled = true;
                    enemyReferences.winText.enabled = true;
                }
                Destroy(gameObject);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
    }

    public void Reset()
    {
        detectionLevel = 0;
        isTurned = false;
    }

    private void ToggleMovementMode(MovementMode targetMovementMode)
    {
        if (currentMovementMode != targetMovementMode)
        {
            currentMovementMode = targetMovementMode;
            switch (targetMovementMode)
            {
                default:
                case MovementMode.Stop:
                    enemyReferences.agent.speed = 0;
                    
                    enemyReferences.movementAudioSource.Stop();

                    enemyReferences.bodyAnim.SetBool("isRunning", false);
                    enemyReferences.legsAnim.SetInteger("movementMode", 0);
                    break;

                case MovementMode.Walk:
                    enemyReferences.agent.speed = walkingSpeed;

                    enemyReferences.movementAudioSource.Stop();
                    enemyReferences.movementAudioSource.clip = enemyReferences.walkingSFX;
                    enemyReferences.movementAudioSource.volume = 0.125f;
                    enemyReferences.movementAudioSource.Play();

                    enemyReferences.bodyAnim.SetBool("isRunning", true);
                    enemyReferences.legsAnim.SetInteger("movementMode", 1);
                    break;

                case MovementMode.Run:
                    enemyReferences.agent.speed = runningSpeed;

                    enemyReferences.movementAudioSource.Stop();
                    enemyReferences.movementAudioSource.clip = enemyReferences.runningSFX;
                    enemyReferences.movementAudioSource.volume = 0.25f;
                    enemyReferences.movementAudioSource.Play();

                    enemyReferences.bodyAnim.SetBool("isRunning", true);
                    enemyReferences.legsAnim.SetInteger("movementMode", 2);
                    break;
            }
        }
    }

    private void SeekPlayer()
    {
        if (Vector2.Distance(transform.position, enemyReferences.player.position) <= instantDetectionDistance)
        {
            currentTarget = enemyReferences.player.position;
            UpdateEnemyPath(currentTarget);
            Debug.Log("SeekPlayer(1): " + currentEnemyState + " --> Battle");
            currentEnemyState = EnemyState.Battle;
            //Reset();
            return;
        }

        float detectionDeadline = midPFOVdetectionTime * farPFOVdetectionTime;

        if (detectionLevel >= detectionDeadline)
        {
            if (RotateTowardsTarget(currentTarget, 50f))
            {
                Debug.Log("SeekPlayer(2): " + currentEnemyState + " --> Search");
                currentEnemyState = EnemyState.Search;
                searchTime = 25f;
                detectionLevel -= detectionDeadline / midPFOVdetectionTime * Time.deltaTime;
                Debug.Log(detectionLevel);
                //Reset();
            }
        }
        
        if (Vector2.Distance(transform.position, enemyReferences.player.position) <= detectionDistance)
        {      
            Vector2 directionToPlayer = (enemyReferences.player.position - transform.position).normalized;
            VisionState currentVisionState = getCurrentVisionState(directionToPlayer, detectionDistance);

            switch (currentVisionState)
            {
                default:
                case VisionState.NotVisible:
                    if (detectionLevel > 0 && detectionLevel < detectionDeadline)
                    {
                        detectionLevel -= detectionDeadline / farPFOVdetectionTime * Time.deltaTime;
                        Debug.Log(detectionLevel);
                    }
                    break;

                case VisionState.CentralFOV:
                    currentTarget = enemyReferences.player.position;
                    UpdateEnemyPath(currentTarget);
                    Debug.Log("SeekPlayer(3): " + currentEnemyState + " --> Battle");
                    currentEnemyState = EnemyState.Battle;
                    //Reset();
                    break;

                case VisionState.MidPeripheralFOV:
                    if (detectionLevel >= detectionDeadline)
                    {
                        currentTarget = enemyReferences.player.position;
                    }
                    else
                    {
                        detectionLevel += detectionDeadline / midPFOVdetectionTime * Time.deltaTime;
                        Debug.Log(detectionLevel);
                    }
                    break;

                case VisionState.FarPeripheralFOV:
                    if (detectionLevel >= detectionDeadline)
                    {
                        currentTarget = enemyReferences.player.position;
                    }
                    else
                    {
                        detectionLevel += detectionDeadline / farPFOVdetectionTime * Time.deltaTime;
                        Debug.Log(detectionLevel);
                    }
                    break;
            }
        }
        else
        {
            if (detectionLevel > 0 && detectionLevel < detectionDeadline)
            {
                detectionLevel -= detectionDeadline / farPFOVdetectionTime * Time.deltaTime;
                Debug.Log(detectionLevel);
            }
        }
    }

    private void UpdateEnemyPath(Vector3 target)
    {
        if (Time.time >= pathUpdateDeadline && enemyReferences.agent.destination != target)
        {
            pathUpdateDeadline = Time.time + pathUpdateDelay;
            enemyReferences.agent.SetDestination(target);
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

        float angle = Mathf.Atan2(enemyReferences.agent.velocity.y, enemyReferences.agent.velocity.x) * Mathf.Rad2Deg;
        //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(Vector3.forward * (angle)), 10 * Time.deltaTime);
        Quaternion targetRotation = Quaternion.Euler(new Vector3(0, 0, angle));

        if (transform.rotation != targetRotation)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            //return transform.rotation == targetRotation;
            //Debug.Log("*rotation*");
            return false;
        }
        else
        {
            //Debug.Log("Already rotated");
            return true;
        }

        /*
        Vector3 direction = (enemyReferences.agent.steeringTarget - transform.position).normalized;
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
        //transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(enemyReferences.player.transform.eulerAngles.y, enemyReferences.player.transform.eulerAngles.x, 0), 40 * rotationSpeed * Time.deltaTime);

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
        enemyReferences.playerAudioSource.PlayOneShot(enemyReferences.shotSFX, 0.3f);
        StartCoroutine(PlaySoundWithDelay(enemyReferences.shellsSFX, 0.5f, 0.25f));
        enemyReferences.bodyAnim.Play("Pistol.Shoot", 0, 0f);

        GameObject bullet = Instantiate(enemyReferences.bulletPrefab, enemyReferences.firePoint.position, enemyReferences.firePoint.rotation);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        Vector3 dir = enemyReferences.firePoint.right + new Vector3(Random.Range(-bulletSpread, bulletSpread), Random.Range(-bulletSpread, bulletSpread), .0f);
        rb.AddForce(dir * bulletForce, ForceMode2D.Impulse);

        GameObject shell = Instantiate(enemyReferences.shellPrefab, enemyReferences.firePoint.position, enemyReferences.firePoint.rotation);
        Rigidbody2D rb2 = shell.GetComponent<Rigidbody2D>();
        rb2.AddForce(-enemyReferences.firePoint.up * shellForce, ForceMode2D.Impulse);
        Destroy(shell, 20f);


        GameObject effect = Instantiate(enemyReferences.flashEffect, enemyReferences.firePoint.position, enemyReferences.firePoint.rotation);
        Destroy(effect, 0.05f);

        cooldown = Random.Range(minCooldownTime, maxCooldownTime);
        magOccupancy -= 1;
    }

    private IEnumerator Reload()
    {
        enemyReferences.playerAudioSource.PlayOneShot(enemyReferences.reloadSFX, 0.5f);
        enemyReferences.bodyAnim.Play("Pistol.Reload", 0, 0f);

        cooldown = reloadTime;
        yield return new WaitForSeconds(reloadTime);

        magOccupancy = magSize;
    }

    private IEnumerator PlaySoundWithDelay(AudioClip clip, float volume, float delay)
    {
        yield return new WaitForSeconds(delay);
        enemyReferences.playerAudioSource.PlayOneShot(clip, volume);
    }

    private Vector2 DirectionFromAngle(float eulerY, float angleInDegrees)
    {
        angleInDegrees -= eulerY - 90;

        return new Vector2(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
