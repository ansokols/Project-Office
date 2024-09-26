using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Enemy : MonoBehaviour
{
    [field: Header("Enemy Characteristics")]
    [field: SerializeField] public Enums.IdleMode idleMode {get; private set;}
    [field: SerializeField] public int health {get; set;}
    [field: SerializeField] public float walkingSpeed {get; private set;}
    [field: SerializeField] public float runningSpeed {get; private set;}
    [field: SerializeField] public float instantDetectionDistance {get; private set;}
    [field: SerializeField] public float detectionDistance {get; private set;}
    [field: SerializeField] public float visionDistance {get; private set;}
    [field: SerializeField] public float stoppingDistance {get; private set;}
    [field: SerializeField] public float retreatDistance {get; private set;}
    [field: SerializeField] public float quickSearchRadius {get; private set;}
    [field: SerializeField] public float deepSearchRadius {get; private set;}
    [field: SerializeField] public float quickSearchTime {get; private set;}
    [field: SerializeField] public float deepSearchTime {get; private set;}
    [field: Range(0,360)]
    [field: SerializeField] public float centralFOV {get; private set;}
    [field: Range(0,360)]
    [field: SerializeField] public float midPeripheralFOV {get; private set;}
    [field: Range(0,360)]
    [field: SerializeField] public float farPeripheralFOV {get; private set;}
    [field: Range(0, 1)]
    [field: SerializeField] public float healDropChance {get; private set;}

    [field: Header("Shooting Characteristics")]
    [field: SerializeField] public float bulletForce {get; private set;}
    [field: SerializeField] public float bulletSpread {get; private set;}
    [field: SerializeField] public float shellForce {get; private set;}
    [field: SerializeField] public float reloadTime {get; private set;}
    [field: SerializeField] public float minCooldownTime {get; private set;}
    [field: SerializeField] public float maxCooldownTime {get; private set;}
    [field: SerializeField] public int magSize {get; private set;}

    public EnemyReferences enemyReferences {get; private set;}

    public static int enemyAmount {get; private set;}

    public Vector3 currentTarget {get; set;}
    public Vector3 home {get; private set;}
    public int enemiesLayer {get; set;}
    public int layerMask {get; set;}

    public float pathUpdateDelay {get; set;}
    public float pathUpdateDeadline {get; set;}
    public float midPFOVdetectionTime {get; set;}
    public float farPFOVdetectionTime {get; set;}
    public float movementPauseDeadline {get; set;}

    public float detectionLevel {get; set;}

    public Enums.MovementMode currentMovementMode {get; set;}


    public EnemyStateMachine enemyStateMachine {get; set;}
    public EnemyIdleState enemyIdleState {get; set;}
    public EnemyBattleState enemyBattleState {get; set;}
    public EnemyQuickSearchState enemyQuickSearchState {get; set;}
    public EnemyDeepSearchState enemyDeepSearchState {get; set;}

    private void Awake()
    {
        enemyReferences = GetComponent<EnemyReferences>();

        enemyStateMachine = new EnemyStateMachine();
        enemyIdleState = new EnemyIdleState(enemyStateMachine, this, enemyReferences);
        enemyBattleState = new EnemyBattleState(enemyStateMachine, this, enemyReferences);
        enemyQuickSearchState = new EnemyQuickSearchState(enemyStateMachine, this, enemyReferences);
        enemyDeepSearchState = new EnemyDeepSearchState(enemyStateMachine, this, enemyReferences);
    }

    // Start is called before the first frame update
    private void Start()
    {
        enemyReferences.agent.updateRotation = false;
        enemyReferences.agent.updateUpAxis = false;
        pathUpdateDelay = 0.2f;
        enemiesLayer = 8;
        layerMask = ~(1 << enemiesLayer);

        home = transform.position;
        detectionLevel = 0f;
        midPFOVdetectionTime = 4f;
        farPFOVdetectionTime = 7f;

        enemyAmount = 14;
        enemyReferences.restartButton.enabled = false;
        enemyReferences.restartText.enabled = false;
        enemyReferences.winText.enabled = false;
        enemyReferences.hud.SetActive(true);

        enemyStateMachine.Initialize(enemyIdleState);
    }

    // Update is called once per frame
    private void Update()
    {
        enemyStateMachine.Update();

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

    public void TakeDamage(int damage)
    {
        health -= damage;
        //TODO
        currentTarget = enemyReferences.player.position;
        UpdateEnemyPath(currentTarget);
        enemyStateMachine.ChangeState(enemyBattleState);
    }

    public void ToggleMovementMode(Enums.MovementMode targetMovementMode)
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

    public /*DetectionState*/bool SeekPlayer()
    {
        float detectionDeadline = midPFOVdetectionTime * farPFOVdetectionTime;
        
        if (Vector2.Distance(transform.position, enemyReferences.player.position) <= instantDetectionDistance)
        {
            currentTarget = enemyReferences.player.position;
            UpdateEnemyPath(currentTarget);
            enemyStateMachine.ChangeState(enemyBattleState);
            detectionLevel -= detectionDeadline / midPFOVdetectionTime * Time.deltaTime;
            Debug.Log("SeekPlayer :" + detectionLevel);
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
                        Debug.Log("SeekPlayer1 :" + detectionLevel);
                    }
                    break;

                case Enums.VisionState.CentralFOV:
                    currentTarget = enemyReferences.player.position;
                    UpdateEnemyPath(currentTarget);
                    enemyStateMachine.ChangeState(enemyBattleState);
                    detectionLevel -= detectionDeadline / midPFOVdetectionTime * Time.deltaTime;
                    Debug.Log("SeekPlayer :" + detectionLevel);
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
                        Debug.Log("SeekPlayer2 :" + detectionLevel);
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
                        Debug.Log("SeekPlayer3 :" + detectionLevel);
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
            ToggleMovementMode(Enums.MovementMode.Stop);

            if (RotateTowardsTarget(currentTarget, 50f))
            {
                UpdateEnemyPath(currentTarget);
                ToggleMovementMode(Enums.MovementMode.Walk);
                enemyStateMachine.ChangeState(enemyQuickSearchState);
                detectionLevel -= detectionDeadline / midPFOVdetectionTime * Time.deltaTime;
                //Debug.Log("SeekPlayer :" + detectionLevel);
            }
            
            //return DetectionState.Detected;
            return true;
        }
        
        //return DetectionState.NotDetected;
        return false;
    }

    public void UpdateEnemyPath(Vector3 target)
    {
        if (/*Time.time >= pathUpdateDeadline && */enemyReferences.agent.destination != target)
        {
            pathUpdateDeadline = Time.time + pathUpdateDelay;
            enemyReferences.agent.SetDestination(target);
        }
    }

    public Enums.VisionState getCurrentVisionState(Vector2 directionToTarget, float distance)
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

    public bool RotateTowardsMovement(float rotationSpeed)
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

    public bool RotateTowardsTarget(Vector3 target, float rotationSpeed)
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

    private Vector2 DirectionFromAngle(float eulerY, float angleInDegrees)
    {
        angleInDegrees -= eulerY - 90;

        return new Vector2(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
