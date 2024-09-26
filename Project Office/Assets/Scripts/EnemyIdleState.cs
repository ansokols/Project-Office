using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyIdleState : EnemyState
{
    private int waypointIndex;

    public EnemyIdleState(EnemyStateMachine enemyStateMachine, Enemy enemy, EnemyReferences enemyReferences) : base(enemyStateMachine, enemy, enemyReferences)
    {
        waypointIndex = 0;
    }

    public override void Enter()
    {
        Debug.Log("Idle state [ENTER]");

        enemy.ToggleMovementMode(Enums.MovementMode.Stop);
        enemyReferences.vision.SetActive(true);
    }

    public override void Exit()
    {
        Debug.Log("Idle state [EXIT]");
    }

    public override void Update()
    {
        switch (enemy.idleMode)
        {
            default:
            case Enums.IdleMode.Passive:
                enemy.UpdateEnemyPath(enemy.home);

                if (enemyReferences.agent.remainingDistance <= enemyReferences.agent.stoppingDistance) //done with path
                {
                    enemy.ToggleMovementMode(Enums.MovementMode.Stop);
                    enemyReferences.vision.SetActive(false);
                }
                else
                {
                    enemy.ToggleMovementMode(Enums.MovementMode.Walk);
                    enemy.RotateTowardsMovement(150f);
                }

                enemy.SeekPlayer();
                break;
            
            case Enums.IdleMode.ConsistentPatrol:
                if (!enemy.SeekPlayer())
                {
                    if (enemyReferences.waypoints != null && enemyReferences.waypoints.Length != 0)
                    {
                        if ((Vector2)enemyReferences.agent.transform.position == (Vector2)enemyReferences.waypoints[waypointIndex].position) //done with path
                        {
                            IncreaseWaypointIndex();
                        }
                        else
                        {
                            enemy.UpdateEnemyPath(enemyReferences.waypoints[waypointIndex].position);
                            enemy.ToggleMovementMode(Enums.MovementMode.Walk);
                            enemy.RotateTowardsMovement(200f);
                        }
                    }
                }
                break;
            
            case Enums.IdleMode.RandomPatrol:
                if (!enemy.SeekPlayer())
                    {
                        if (enemyReferences.waypoints != null && enemyReferences.waypoints.Length != 0)
                        {
                            if ((Vector2)enemyReferences.agent.transform.position == (Vector2)enemyReferences.waypoints[waypointIndex].position) //done with path
                            {
                                waypointIndex = Random.Range(0, enemyReferences.waypoints.Length - 1);
                            }
                            else
                            {
                                enemy.UpdateEnemyPath(enemyReferences.waypoints[waypointIndex].position);
                                enemy.ToggleMovementMode(Enums.MovementMode.Walk);
                                enemy.RotateTowardsMovement(200f);
                            }
                        }
                    }
                break;
        }
    }

    private void IncreaseWaypointIndex()
    {
        waypointIndex++;
        if (waypointIndex >= enemyReferences.waypoints.Length)
        {
            waypointIndex = 0;
        }
    }
}
