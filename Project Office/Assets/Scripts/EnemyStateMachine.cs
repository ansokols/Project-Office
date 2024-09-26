using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStateMachine
{
    private EnemyState currentEnemyState {get; set;}

    /*
    private Dictionary<Type, EnemyState> enemyStates = new Dictionary<Type, EnemyState>();

    public void AddState(EnemyState enemyState)
    {
        enemyStates.Add(enemyState.GetType(), enemyState);
    }

    public void SetState<T>() where T : EnemyState
    {
        var type = typeof(T);

        if (currentEnemyState.GetType() == type)
        {
            return;
        }

        if (enemyStates.TryGetValue(type, out var newState))
        {
            currentEnemyState?.Exit();
            currentEnemyState = newState;
            currentEnemyState.Enter();
        }
    }
    */

    public void Initialize(EnemyState startingState)
    {
        currentEnemyState = startingState;
        currentEnemyState.Enter();
    }

    public void ChangeState(EnemyState newState)
    {
        if (currentEnemyState.GetType() == newState.GetType())
        {
            return;
        }

        currentEnemyState?.Exit();
        currentEnemyState = newState;
        currentEnemyState.Enter();
        Debug.Log("EnemyStateMachine: STATE CHANGED");
    }

    public void Update()
    {
        currentEnemyState?.Update();
    }
}
