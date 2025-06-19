using UnityEngine;

[CreateAssetMenu(fileName = "Pathfinding Preset")]
public class PathfindingPreset : ScriptableObject
{
    public int maxWalkableSteepness = 30;
    public int maxClimbableSteepness = 50;

    public int priorityBase = 100;
    public int priorityWalk = 100;
    public int priorityClimb = 100;
}
