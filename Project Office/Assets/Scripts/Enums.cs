
public class Enums
{
    public enum EnemyState
    {
        Idle,
        Battle,
        QuickSearch,
        DeepSearch
    }

    public enum VisionState
    {
        NotVisible,
        CentralFOV,
        MidPeripheralFOV,
        FarPeripheralFOV
    }

    public enum DetectionState
    {
        NotDetected,
        Detected,
        Found
    }

    public enum IdleMode
    {
        Passive,
        ConsistentPatrol,
        RandomPatrol
    }

    public enum MovementMode
    {
        Stop,
        Walk,
        Run
    }
}