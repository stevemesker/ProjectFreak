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
            Debug.LogWarning("Warning! Could not find suitable position to summon shade, skipping the summon...");
            return;
        }
        Debug.LogWarning($"{source.name} is summoning a shade | Player location: {source.transform.position} | Shade location: {source.transform.position + summonPosition}");
        Debug.LogWarning($"Correct numbers on a flat plane should be : {source.transform.position + (source.transform.forward * 5)}");
        SpawnShade(summonPosition);
    }

    Vector3 FindSummonSpot(GameObject source)
    {
        float searchDistance = 5f;
        float minimumSpace = 2f;
        float shadeBuffer = 0.5f;
        float searchAngle = 2f;

        Vector3 finalPosition = Vector3.zero;
        float smallestDistance = searchDistance;


        // Find the surface the player is standing on
        Vector3 groundOrigin = source.transform.position + Vector3.up * 0.5f;

        if (!Physics.Raycast(groundOrigin,Vector3.down,out RaycastHit groundHit,5f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
        {
            return Vector3.zero;
        }

        Vector3 origin = groundHit.point + groundHit.normal * 0.1f;

        for (float angle = 0f; angle < 360f; angle += searchAngle)
        {
            Vector3 direction = Quaternion.AngleAxis(angle, groundHit.normal) * source.transform.forward;

            direction = Vector3.ProjectOnPlane(direction,groundHit.normal).normalized;

            RaycastHit[] hits = Physics.RaycastAll(origin, direction, searchDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            RaycastHit finalHit;
            foreach(RaycastHit hit in hits)
            {
                if (hit.transform.gameObject == source) { Debug.LogWarning("Found Myself..."); continue; }
                if (hit.distance < smallestDistance)
                {
                    smallestDistance = hit.distance;
                }
                    //return Vector3.zero;
            }
            if (smallestDistance < minimumSpace)
            {
                Debug.LogWarning($"Warning! Not enough room to summon shade! Range: {smallestDistance}");
                continue;
            }

            finalPosition = direction * (smallestDistance - shadeBuffer);

            return finalPosition;
            //return direction * searchDistance;
            /*
            if (Physics.Raycast(origin + source.transform.forward,direction,out RaycastHit hit,searchDistance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {
                if (hit.distance < minimumSpace)
                    continue;

                Vector3 worldPosition = origin + direction * (hit.distance - shadeBuffer);
                Debug.LogWarning($"Direction: {direction} | WorldPosition: {worldPosition}");
                // Convert the world position to a position relative to the source
                return source.transform.InverseTransformPoint(worldPosition);
            }
            

            Vector3 openWorldPosition = origin + direction * searchDistance;

            // Convert the world position to a position relative to the source
            return source.transform.InverseTransformPoint(openWorldPosition);
            */
        }

        return Vector3.zero;
    }

    public void SpawnShade(Vector3 location)
    {
        //spawns the shade through the shade manager singleton
        if (ShadeManager._ShadeManager == null) { Debug.LogError("Error! Shade Manager not found, can't summon a shade"); return; }
        ShadeManager._ShadeManager.SummonShade(location);
    }
}
