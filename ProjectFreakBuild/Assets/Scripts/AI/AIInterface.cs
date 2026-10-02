using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUnitMover
{
    //anything an AI brain can steer around the NavMesh. EnemyMovement (enemies) and NavGuideDriver (shades) both use this,
    //so one brain works for either kind of unit
    void SetDestination(Vector3 destination);
    void StopMoving();
    bool HasArrived();
    bool IsActive(); //false while it can't take orders (no NavMesh yet, being knocked back, the player is driving it)
}
