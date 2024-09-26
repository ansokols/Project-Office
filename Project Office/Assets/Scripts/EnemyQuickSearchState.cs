using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyQuickSearchState : EnemyState
{
    private float timeSearched;
    private bool isRotated;

    public EnemyQuickSearchState(EnemyStateMachine enemyStateMachine, Enemy enemy, EnemyReferences enemyReferences) : base(enemyStateMachine, enemy, enemyReferences)
    {
        timeSearched = 0f;
        isRotated = false;
    }

    public override void Enter()
    {
        Debug.Log("QuickSearch state [ENTER]");

        enemy.ToggleMovementMode(Enums.MovementMode.Stop);
        enemyReferences.vision.SetActive(true);
        timeSearched = 0f;
    }

    public override void Exit()
    {
        Debug.Log("QuickSearch state [EXIT]");

        enemy.ToggleMovementMode(Enums.MovementMode.Stop);
    }

    public override void Update()
    {
        if (timeSearched >= enemy.quickSearchTime)
        {
            enemy.enemyStateMachine.ChangeState(enemy.enemyIdleState);
            return;
        }

        if (enemyReferences.agent.remainingDistance <= enemyReferences.agent.stoppingDistance) //done with path
        {
            isRotated = false;
            enemy.ToggleMovementMode(Enums.MovementMode.Stop);
            Vector3 point;
            if (RandomPoint(enemy.currentTarget, enemy.quickSearchRadius, out point)) //pass in our centre point and radius of area
            {
                Debug.DrawRay(point, Vector3.up, Color.blue, 1.0f); //so you can see with gizmos
                enemy.UpdateEnemyPath(point);
            }
            enemyReferences.agent.speed = 0.01f;
            //Debug.Log("Enums.EnemyState.Search: " + "Path calculated");
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
}
