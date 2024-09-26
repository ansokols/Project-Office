using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemyDeepSearchState : EnemyState
{
    private List<Transform> relevantLocations;
    public int relevantLocationIndex;
    private float timeSearched;
    private bool isRotated;

    public EnemyDeepSearchState(EnemyStateMachine enemyStateMachine, Enemy enemy, EnemyReferences enemyReferences) : base(enemyStateMachine, enemy, enemyReferences)
    {
        relevantLocations = null;
        relevantLocationIndex = 0;
        timeSearched = 0f;
        isRotated = false;
    }

    public override void Enter()
    {
        Debug.Log("DeepSearch state [ENTER]");

        enemy.ToggleMovementMode(Enums.MovementMode.Stop);
        enemyReferences.vision.SetActive(true);

        timeSearched = 0f;
        relevantLocationIndex = 0;

        relevantLocations = enemyReferences.locations.Where(location => Vector2.Distance(enemy.transform.position, location.position) <= enemy.deepSearchRadius).ToList();
        relevantLocations.Sort((location1, location2) => Vector2.Distance(enemy.transform.position, location1.position).CompareTo(Vector2.Distance(enemy.transform.position, location2.position)));
        relevantLocations.ForEach(Debug.Log);

        if (relevantLocations.Count == 0)
        {
            enemy.enemyStateMachine.ChangeState(enemy.enemyQuickSearchState);
        }
    }

    public override void Exit()
    {
        Debug.Log("DeepSearch state [EXIT]");

        enemy.ToggleMovementMode(Enums.MovementMode.Stop);
    }

    public override void Update()
    {
        if (timeSearched >= enemy.deepSearchTime)
        {
            enemy.enemyStateMachine.ChangeState(enemy.enemyIdleState);
            return;
        }

        if (enemyReferences.agent.remainingDistance <= enemyReferences.agent.stoppingDistance) //done with path
        {
            if (relevantLocations.Count != 0)
            {
                enemy.UpdateEnemyPath(relevantLocations[relevantLocationIndex].position);
                enemy.ToggleMovementMode(Enums.MovementMode.Stop);
                enemy.enemyReferences.agent.speed = 0.01f;
                isRotated = false;
                IncreaseLocationIndex();
            }
        }
        else
        {
            if (!enemy.SeekPlayer())
            {
                if (isRotated)
                {
                    //Debug.Log("if 2 start");
                    enemy.ToggleMovementMode(Enums.MovementMode.Walk);
                    enemy.RotateTowardsMovement(150f);
                    //Debug.Log("if 2 end");
                }
                else if (enemy.RotateTowardsMovement(100f))
                {
                    //Debug.Log("if 3 start");
                    isRotated = true;
                    //Debug.Log("if 3 end");
                }
            }
        }

        timeSearched += Time.deltaTime;
    }

    private void IncreaseLocationIndex()
    {
        relevantLocationIndex++;
        if (relevantLocationIndex >= relevantLocations.Count)
        {
            relevantLocationIndex = 0;
        }
    }
}
