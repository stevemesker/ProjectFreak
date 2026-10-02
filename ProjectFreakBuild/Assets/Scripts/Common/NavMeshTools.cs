using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class NavMeshTools
{
    //Small shared helpers for anything that moves on the NavMesh (EnemyMovement, NavGuideDriver).
    //"static" means you call these straight off the class, like NavMeshTools.TryGetNavMeshPoint(...), without putting it on an object.

    //settings for TryGetRandomPoint
    const int RandomPointTries = 5; //how many random spots to try before giving up
    const float RandomPointSnapDistance = 2f; //how far from each random spot to look for the NavMesh, in meters

    public static bool IsWaitingOnFloor(GameObject unit, out DungeonFloorObject floor)
    {
        //function that checks if this unit's scene is a dungeon floor that's still loading (so its NavMesh isn't built yet)
        floor = DungeonFloorObject._Floor;
        if (floor == null) return false;
        if (floor.gameObject.scene != unit.scene) { floor = null; return false; } //a floor from a different scene doesn't count
        if (floor.IsFloorReady()) { floor = null; return false; }
        return true;
    }

    public static bool TryGetNavMeshPoint(Vector3 position, float maxDistance, out Vector3 point)
    {
        //function that finds the closest spot on the NavMesh within maxDistance meters. Returns false if there isn't one
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
        {
            point = hit.position;
            return true;
        }
        point = position;
        return false;
    }

    public static bool TryGetRandomPoint(Vector3 center, float radius, out Vector3 point)
    {
        //function that picks a random spot on the NavMesh within radius meters of center. Returns false if none of its tries landed on the NavMesh
        for (int i = 0; i < RandomPointTries; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius; //a random point inside a circle
            Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);
            if (TryGetNavMeshPoint(candidate, RandomPointSnapDistance, out point)) return true;
        }
        point = center;
        return false;
    }
}
