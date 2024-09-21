using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using System.Linq;

public class BetterEnemy : MonoBehaviour
{
    [Header("Enemy Characteristics")]
    [SerializeField] private Enums.IdleMode idleMode;
    [SerializeField] private int health;
    [SerializeField] private float walkingSpeed;
    [SerializeField] private float runningSpeed;
    [SerializeField] private float instantDetectionDistance;
    [SerializeField] private float detectionDistance;
    [SerializeField] private float visionDistance;
    [SerializeField] private float stoppingDistance;
    [SerializeField] private float retreatDistance;
    [SerializeField] private float quickSearchRadius;
    [SerializeField] private float deepSearchRadius;
    [SerializeField] private float quickSearchTime;
    [SerializeField] private float deepSearchTime;
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
    private int waypointIndex;
    private int relevantLocationIndex;
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
    private bool isTurned;
    private bool isTriggered;
    private float timeSearched;
    //private Transform[] relevantLocations;
    private List<Transform> relevantLocations;

    private Enums.EnemyState currentEnemyState;
    private Enums.MovementMode currentMovementMode;

    void Awake()
    {
        enemyReferences = GetComponent<EnemyReferences>();
    }

    // Start is called before the first frame update
    void Start()
    {
        currentEnemyState = Enums.EnemyState.Idle;
        isTurned = false;
        isTriggered = false;
        timeSearched = 0f;
        relevantLocations = null;

        enemyReferences.agent.updateRotation = false;
        enemyReferences.agent.updateUpAxis = false;
        pathUpdateDelay = 0.2f;
        extraDetectionTime = 0.5f;
        enemiesLayer = 8;
        layerMask = ~(1 << enemiesLayer);

        home = transform.position;
        waypointIndex = 0;
        relevantLocationIndex = 0;
        detectionLevel = 0f;
        midPFOVdetectionTime = 4f;
        farPFOVdetectionTime = 7f;

        magOccupancy = Random.Range(0, magSize + 1);

        enemyAmount = 14;
        enemyReferences.restartButton.enabled = false;
        enemyReferences.restartText.enabled = false;
        enemyReferences.winText.enabled = false;
        enemyReferences.hud.SetActive(true);

        UpdateEnemyPath(home);
    }

    // Update is called once per frame
    void Update()
    {
        if (enemyReferences.player != null)
        {
            switch (currentEnemyState)
            {
                default:
                case Enums.EnemyState.Idle:
                    switch (idleMode)
                    {
                        default:
                        case Enums.IdleMode.Passive:
                            enemyReferences.vision.SetActive(false);
                            /*
                            Debug.LogWarning(gameObject.name + " seems to be stuck");
                                Debug.LogWarning("	navTarget:" + enemyReferences.waypoints[waypointIndex] + "::" + enemyReferences.waypoints[waypointIndex].position);
                                Debug.LogWarning("	nav distination:" + enemyReferences.agent.destination);
                                //Debug.LogWarning("	distance from nav destination:" + distanceFromNavTargetOrLKP);
                                Debug.LogWarning("	nav remaingDistance:" + enemyReferences.agent.remainingDistance);
                                Debug.LogWarning("	nav stoppingDistance:" + enemyReferences.agent.stoppingDistance);
                                Debug.LogWarning("	nav speed:" + enemyReferences.agent.speed);
                                Debug.LogWarning("	nav autoRepath:" + enemyReferences.agent.autoRepath);
                                Debug.LogWarning("	nav has path:" + enemyReferences.agent.hasPath);
                                Debug.LogWarning("	nav is computing path:" + enemyReferences.agent.pathPending);
                                Debug.LogWarning("	nav is on nav mesh:" + enemyReferences.agent.isOnNavMesh);
                                Debug.LogWarning("	nav path is stale:" + enemyReferences.agent.isPathStale);
                                Debug.LogWarning("	nav path status:" + enemyReferences.agent.pathStatus);
                                Debug.LogWarning("	nav desired velocity:" + enemyReferences.agent.desiredVelocity);
                            */

                            if (isTriggered)
                            {
                                UpdateEnemyPath(home);
                                if (enemyReferences.agent.remainingDistance <= enemyReferences.agent.stoppingDistance) //done with path
                                {
                                    ToggleMovementMode(Enums.MovementMode.Stop);
                                }
                                else
                                {
                                    ToggleMovementMode(Enums.MovementMode.Walk);
                                    RotateTowardsMovement(150f);
                                }
                            }

                            /*currentTarget = new Vector3(12.1001f, 1.3247f, 10f);
                            UpdateEnemyPath(currentTarget);
                            //enemyReferences.agent.speed = 1f;
                            ToggleMovementMode(MovementMode.Run);
                            RotateTowardsMovement(100f);
                            //Debug.Log("        " + enemyReferences.agent.destination + " | " + currentTarget);*/

                            SeekPlayer();
                            break;
                        
                        case Enums.IdleMode.ConsistentPatrol:
                            enemyReferences.vision.SetActive(true);
                            if (enemyReferences.waypoints != null && enemyReferences.waypoints.Length != 0)
                            {
                                if ((Vector2)enemyReferences.agent.transform.position == (Vector2)enemyReferences.waypoints[waypointIndex].position) //done with path
                                {
                                    IncreaseWaypointIndex();
                                }
                                else
                                {
                                    UpdateEnemyPath(enemyReferences.waypoints[waypointIndex].position);
                                    ToggleMovementMode(Enums.MovementMode.Walk);
                                    RotateTowardsMovement(200f);
                                }
                            }
                            SeekPlayer();
                            break;
                        
                        case Enums.IdleMode.RandomPatrol:
                            enemyReferences.vision.SetActive(true);
                            if (enemyReferences.waypoints != null && enemyReferences.waypoints.Length != 0)
                            {
                                if ((Vector2)enemyReferences.agent.transform.position == (Vector2)enemyReferences.waypoints[waypointIndex].position) //done with path
                                {
                                    waypointIndex = Random.Range(0, enemyReferences.waypoints.Length - 1);
                                }
                                else
                                {
                                    UpdateEnemyPath(enemyReferences.waypoints[waypointIndex].position);
                                    ToggleMovementMode(Enums.MovementMode.Walk);
                                    RotateTowardsMovement(200f);
                                }
                            }
                            SeekPlayer();
                            break;
                    }
                    break;

                case Enums.EnemyState.QuickSearch:
                    enemyReferences.vision.SetActive(true);

                    if (timeSearched >= quickSearchTime)
                    {
                        timeSearched = 0f;
                        currentEnemyState = Enums.EnemyState.Idle;
                        Debug.Log("QuickSearch --> Idle");           
                        return;
                    }

                    if (enemyReferences.agent.remainingDistance <= enemyReferences.agent.stoppingDistance) //done with path
                    {
                        isTurned = false;
                        ToggleMovementMode(Enums.MovementMode.Stop);
                        Vector3 point;
                        if (RandomPoint(currentTarget, quickSearchRadius, out point)) //pass in our centre point and radius of area
                        {
                            Debug.DrawRay(point, Vector3.up, Color.blue, 1.0f); //so you can see with gizmos
                            UpdateEnemyPath(point);
                        }
                        enemyReferences.agent.speed = 0.01f;
                        //Debug.Log("Enums.EnemyState.Search: " + "Path calculated");
                    }
                    else
                    {
                        if (!SeekPlayer())
                        {
                            if (isTurned)
                            {
                                //Debug.Log("if 2 start");
                                ToggleMovementMode(Enums.MovementMode.Walk);
                                RotateTowardsMovement(150f);
                                //Debug.Log("if 2 end");
                            }
                            else if (RotateTowardsMovement(100f))
                            {
                                //Debug.Log("if 3 start");
                                isTurned = true;
                                //Debug.Log("if 3 end");
                            }
                        }
                        else
                        {
                            ToggleMovementMode(Enums.MovementMode.Stop);
                            timeSearched = 0f;
                        }
                    }
                    timeSearched += Time.deltaTime;
                    break;

                case Enums.EnemyState.DeepSearch:
                    enemyReferences.vision.SetActive(true);

                    if (relevantLocations == null)
                    {
                        relevantLocations = enemyReferences.locations.Where(location => Vector2.Distance(transform.position, location.position) <= deepSearchRadius).ToList();
                        relevantLocations.Sort((location1, location2) => Vector2.Distance(transform.position, location1.position).CompareTo(Vector2.Distance(transform.position, location2.position)));
                        relevantLocations.ForEach(Debug.Log);
                        Debug.Log(gameObject.name + ":: relevantLocations calculated");
                    }

                    if (timeSearched >= deepSearchTime)
                    {
                        timeSearched = 0f;
                        relevantLocationIndex = 0;
                        relevantLocations = null;
                        currentEnemyState = Enums.EnemyState.Idle;
                        Debug.Log("DeepSearch(1) --> Idle");           
                        return;
                    }

                    if (enemyReferences.agent.remainingDistance <= enemyReferences.agent.stoppingDistance) //done with path
                    {
                        if (relevantLocationIndex < relevantLocations.Count)
                        {
                            //Debug.Log("if 1 start");
                            UpdateEnemyPath(relevantLocations[relevantLocationIndex].position);
                            ToggleMovementMode(Enums.MovementMode.Stop);
                            enemyReferences.agent.speed = 0.01f;
                            isTurned = false;
                            relevantLocationIndex++;
                            //Debug.Log("if 1 end");
                        }
                        else
                        {
                            /*
                            timeSearched = 0f;
                            relevantLocationIndex = 0;
                            relevantLocations = null;
                            currentEnemyState = Enums.EnemyState.Idle;
                            Debug.Log("DeepSearch(2) --> Idle");
                            */
                            relevantLocationIndex = 0;         
                            return;
                        }
                    }
                    else
                    {
                        if (!SeekPlayer())
                        {
                            if (isTurned)
                            {
                                //Debug.Log("if 2 start");
                                ToggleMovementMode(Enums.MovementMode.Walk);
                                RotateTowardsMovement(150f);
                                //Debug.Log("if 2 end");
                            }
                            else if (RotateTowardsMovement(100f))
                            {
                                //Debug.Log("if 3 start");
                                isTurned = true;
                                //Debug.Log("if 3 end");
                            }
                        }
                        else
                        {
                            ToggleMovementMode(Enums.MovementMode.Stop);
                            timeSearched = 0f;
                            relevantLocationIndex = 0;
                            relevantLocations = null;
                        }
                    }
                    timeSearched += Time.deltaTime;

                    break;

                case Enums.EnemyState.Battle:
                    //UpdateEnemyPath(currentTarget);
                    enemyReferences.vision.SetActive(true);

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
                        ToggleMovementMode(Enums.MovementMode.Stop);
                        currentEnemyState = Enums.EnemyState.DeepSearch;
                        //Reset();
                        Debug.Log("Battle --> DeepSearch");
                    }
                    else if (distanceToPlayer > stoppingDistance)
                    {
                        ToggleMovementMode(Enums.MovementMode.Run);

                        if (getCurrentVisionState(directionToPlayer, visionDistance) != Enums.VisionState.NotVisible)
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
                        ToggleMovementMode(Enums.MovementMode.Run);
                        
                        if (getCurrentVisionState(directionToPlayer, visionDistance) != Enums.VisionState.NotVisible)
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
                        if (getCurrentVisionState(directionToPlayer, visionDistance) != Enums.VisionState.NotVisible)
                        {
                            ToggleMovementMode(Enums.MovementMode.Stop);
                            currentTarget = enemyReferences.player.position;
                            //UpdateEnemyPath(currentTarget);
                            RotateTowardsTarget(currentTarget, 200f);
                            Attack();
                            extraDetectionDeadline = extraDetectionTime + Time.time;
                        }
                        else
                        { 
                            ToggleMovementMode(Enums.MovementMode.Run);
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
        //TODO
        currentTarget = enemyReferences.player.position;
        UpdateEnemyPath(currentTarget);
        Debug.Log("TakeDamage --> Battle");
        currentEnemyState = Enums.EnemyState.Battle;
    }
/*
    private void Reset()
    {
        timeSearched = 0f;
        relevantLocationIndex = 0;
        relevantLocations = null;
    }
*/
    private void IncreaseWaypointIndex()
    {
        waypointIndex++;
        if (waypointIndex >= enemyReferences.waypoints.Length)
        {
            waypointIndex = 0;
        }
    }

    private void ToggleMovementMode(Enums.MovementMode targetMovementMode)
    {
        if (currentMovementMode != targetMovementMode)
        {
            currentMovementMode = targetMovementMode;
            switch (targetMovementMode)
            {
                default:
                case Enums.MovementMode.Stop:
                    enemyReferences.agent.speed = 0;
                    
                    enemyReferences.movementAudioSource.Stop();

                    enemyReferences.bodyAnim.SetBool("isRunning", false);
                    enemyReferences.legsAnim.SetInteger("movementMode", 0);
                    break;

                case Enums.MovementMode.Walk:
                    enemyReferences.agent.speed = walkingSpeed;

                    enemyReferences.movementAudioSource.Stop();
                    enemyReferences.movementAudioSource.clip = enemyReferences.walkingSFX;
                    enemyReferences.movementAudioSource.volume = 0.125f;
                    enemyReferences.movementAudioSource.Play();

                    enemyReferences.bodyAnim.SetBool("isRunning", true);
                    enemyReferences.legsAnim.SetInteger("movementMode", 1);
                    break;

                case Enums.MovementMode.Run:
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

    private /*DetectionState*/bool SeekPlayer()
    {
        float detectionDeadline = midPFOVdetectionTime * farPFOVdetectionTime;
        
        if (Vector2.Distance(transform.position, enemyReferences.player.position) <= instantDetectionDistance)
        {
            currentTarget = enemyReferences.player.position;
            UpdateEnemyPath(currentTarget);
            Debug.Log("SeekPlayer(1): " + currentEnemyState + " --> Battle");
            currentEnemyState = Enums.EnemyState.Battle;
            detectionLevel -= detectionDeadline / midPFOVdetectionTime * Time.deltaTime;
            //Debug.Log("SeekPlayer :" + detectionLevel);
            //Reset();
            //return DetectionState.Found;
            return true;
        }

        if (Vector2.Distance(transform.position, enemyReferences.player.position) <= detectionDistance)
        {      
            Vector2 directionToPlayer = (enemyReferences.player.position - transform.position).normalized;
            Enums.VisionState currentVisionState = getCurrentVisionState(directionToPlayer, detectionDistance);

            switch (currentVisionState)
            {
                default:
                case Enums.VisionState.NotVisible:
                    if (detectionLevel > 0 && detectionLevel < detectionDeadline)
                    {
                        detectionLevel -= detectionDeadline / farPFOVdetectionTime * Time.deltaTime;
                        //Debug.Log("SeekPlayer1 :" + detectionLevel);
                    }
                    break;

                case Enums.VisionState.CentralFOV:
                    currentTarget = enemyReferences.player.position;
                    UpdateEnemyPath(currentTarget);
                    Debug.Log("SeekPlayer(3): " + currentEnemyState + " --> Battle");
                    currentEnemyState = Enums.EnemyState.Battle;
                    detectionLevel -= detectionDeadline / midPFOVdetectionTime * Time.deltaTime;
                    //Debug.Log("SeekPlayer :" + detectionLevel);
                    //Reset();
                    //return DetectionState.Found;
                    return true;
                    break;

                case Enums.VisionState.MidPeripheralFOV:
                    if (detectionLevel >= detectionDeadline)
                    {
                        currentTarget = enemyReferences.player.position;
                    }
                    else
                    {
                        detectionLevel += detectionDeadline / midPFOVdetectionTime * Time.deltaTime;
                        //Debug.Log("SeekPlayer2 :" + detectionLevel);
                    }
                    break;

                case Enums.VisionState.FarPeripheralFOV:
                    if (detectionLevel >= detectionDeadline)
                    {
                        currentTarget = enemyReferences.player.position;
                    }
                    else
                    {
                        detectionLevel += detectionDeadline / farPFOVdetectionTime * Time.deltaTime;
                        //Debug.Log("SeekPlayer3 :" + detectionLevel);
                    }
                    break;
            }
        }
        else
        {
            if (detectionLevel > 0 && detectionLevel < detectionDeadline)
            {
                detectionLevel -= detectionDeadline / farPFOVdetectionTime * Time.deltaTime;
                //Debug.Log("SeekPlayer4 :" + detectionLevel);
            }
        }

        if (detectionLevel >= detectionDeadline)
        {
            
            if (RotateTowardsTarget(currentTarget, 50f))
            {
                UpdateEnemyPath(currentTarget);
                ToggleMovementMode(Enums.MovementMode.Walk);
                Debug.Log("SeekPlayer(2): " + currentEnemyState + " --> QuickSearch");
                currentEnemyState = Enums.EnemyState.QuickSearch;
                detectionLevel -= detectionDeadline / midPFOVdetectionTime * Time.deltaTime;
                //Debug.Log("SeekPlayer :" + detectionLevel);
                //Reset();
            }
            
            //return DetectionState.Detected;
            return true;
        }
        
        //return DetectionState.NotDetected;
        return false;
    }

    private void UpdateEnemyPath(Vector3 target)
    {
        if (/*Time.time >= pathUpdateDeadline && */enemyReferences.agent.destination != target)
        {
            pathUpdateDeadline = Time.time + pathUpdateDelay;
            enemyReferences.agent.SetDestination(target);
            isTriggered = true;
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

    private Enums.VisionState getCurrentVisionState(Vector2 directionToTarget, float distance)
    {
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
            //Debug.DrawLine(transform.position, (Vector2)transform.position + directionToTarget * distance, Color.yellow);
            for (int i = 0; i < allHits.Length; i++)
            {
                if (allHits[i].collider.tag == "Solid")
                {
                    //Debug.DrawLine(transform.position, (Vector2)transform.position + directionToTarget * distance, Color.red);
                    //Debug.DrawLine(transform.position, allHits[i].point, Color.yellow);
                    break;
                }
                if (allHits[i].collider.tag == "Player")
                {
                    Debug.DrawLine(transform.position, allHits[i].point, Color.green);
                    if (angleToTarget <= centralFOV / 2)
                    {
                        return Enums.VisionState.CentralFOV;
                    }
                    if (angleToTarget <= midPeripheralFOV / 2)
                    {
                        return Enums.VisionState.MidPeripheralFOV;
                    }
                    if (angleToTarget <= farPeripheralFOV / 2)
                    {
                        return Enums.VisionState.FarPeripheralFOV;
                    }
                }
            } 
        }

        //Debug.DrawLine(transform.position, (Vector2)transform.position + directionToTarget * distance, Color.black);
        return Enums.VisionState.NotVisible;  
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
