using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SummonShade : AbilityFunction
{
    public override void ActivateAbility(GameObject source, AbilityInterpreter interpreter)
    {
        interpreter.AbilIntLog($"{source.name} successfully used Summon Shade!");
        //find area in front of player to sapwn
        Vector3 summonPosition = FindSummonSpot(source);
        if (summonPosition == Vector3.zero)
        {
            Debug.Log("Warning! Could not find suitable position to summon shade...");
            return;
        }
        SpawnShade(summonPosition);
    }

    Vector3 FindSummonSpot(GameObject source)
    {
        float searchDistance = 5f;
        float minimumSpace = 2f;
        float shadeBuffer = 0.5f;
        float searchAngle = 2f;

        // Find the surface the player is standing on
        Vector3 groundOrigin = source.transform.position + Vector3.up * 0.5f;

        if (!Physics.Raycast(
            groundOrigin,
            Vector3.down,
            out RaycastHit groundHit,
            5f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            return Vector3.zero;
        }

        Vector3 origin = groundHit.point + groundHit.normal * 0.1f;

        for (float angle = 0f; angle < 360f; angle += searchAngle)
        {
            Vector3 direction = Vector3.ProjectOnPlane(Quaternion.Euler(0f, angle, 0f) * source.transform.forward,groundHit.normal).normalized;

            if (Physics.Raycast(
                origin,
                direction,
                out RaycastHit hit,
                searchDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
            {
                if (hit.distance < minimumSpace)
                    continue;

                return origin + direction * (hit.distance - shadeBuffer);
            }

            //return origin + direction * searchDistance;
            Vector3 openPosition = origin + direction * searchDistance;

            return source.transform.InverseTransformPoint(openPosition);
        }

        return Vector3.zero;
    }

    public void SpawnShade(Vector3 location)
    {
        GameManager._GameManager.GetComponent<ShadeManager>().summonShade(location);
    }
}
