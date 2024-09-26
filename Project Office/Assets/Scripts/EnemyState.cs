public abstract class EnemyState
{
    protected readonly EnemyStateMachine enemyStateMachine;
    protected readonly Enemy enemy;
    protected readonly EnemyReferences enemyReferences;


    public EnemyState(EnemyStateMachine enemyStateMachine, Enemy enemy, EnemyReferences enemyReferences)
    {
        this.enemyStateMachine = enemyStateMachine;
        this.enemy = enemy;
        this.enemyReferences = enemyReferences;
    }

    public virtual void Enter() {}
    public virtual void Exit() {}
    public virtual void Update() {}
}