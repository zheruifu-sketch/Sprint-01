public enum PlayerFormType
{
    Human = 1,
    Car = 2,
    Plane = 3,
    Boat = 4
}

public enum ZoneType
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
