public enum PlayerFormType
{
    Human = 1,
    Car = 2,
    Plane = 3,
    Boat = 4
}

[System.Flags]
public enum RuleTag
{
    None = 0,
    SupportsGroundedTravel = 1 << 0,
    SupportsBoat = 1 << 1,
    BlocksBoat = 1 << 2,
    BlocksPlane = 1 << 3,
    SlowsHuman = 1 << 4,
    HazardDamage = 1 << 5,
    InstantWaterDeath = 1 << 6,
    CliffDrop = 1 << 7,
    Obstacle = 1 << 8,
    HintTrigger = 1 << 9
}

public enum EnvironmentType
{
    None = 0,
    Road = 1,
    Water = 2,
    Cliff = 3,
    Blizzard = 4,
    Obstacle = 5
}

public enum FailureType
{
    None = 0,
    FellIntoWater = 1,
    FellFromCliff = 2,
    HitObstacle = 3,
    InvalidForm = 4,
    EnergyDepleted = 5
}

public enum GameRunState
{
    Idle = 0,
    Running = 1,
    Transitioning = 2,
    Completed = 3
}
