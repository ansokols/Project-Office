using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemyDeepSearchState : EnemyState
{
    private List<Transform> relevantLocations;
    public int relevantLocationIndex;
    private float timeSearched;

    public EnemyDeepSearchState(EnemyStateMachine enemyStateMachine, Enemy enemy, EnemyReferences enemyReferences) : base(enemyStateMachine, enemy, enemyReferences)
    {
        relevantLocations = null;
        relevantLocationIndex = 0;
        timeSearched = 0f;
    }

    public override void Enter()
    {
        Debug.Log("DeepSearch state [ENTER]");

        enemy.ToggleMovementMode(Enums.MovementMode.Stop);
        enemyReferences.agent.ResetPath();
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

        if (enemyReferences.agent.remainingDistance <= enemyReferences.agent.stoppingDistance + 1) //done with path
        {
            if (relevantLocations.Count != 0)
            {
                enemy.UpdateEnemyPath(relevantLocations[relevantLocationIndex].position);
                enemy.ToggleMovementMode(Enums.MovementMode.Stop);
                enemyReferences.agent.speed = 0.01f;
                IncreaseLocationIndex();
            }
        }
        else
        {
            if (!enemy.SeekPlayer())
            {
                enemy.ToggleMovementMode(Enums.MovementMode.Walk);
                enemy.RotateTowardsMovement(200f);
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
