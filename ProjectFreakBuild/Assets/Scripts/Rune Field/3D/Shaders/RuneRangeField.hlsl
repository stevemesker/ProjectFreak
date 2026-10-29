//shared functions for the rune field range overlay (the blue "you can bridge here" splotch on the floor)
//used by two Custom Function nodes in Shader Graph (Type: File). Graph steps: How To - Range Overlay in the GDD
//every value below is set from C# by RuneRangeOverlayObject with Shader.SetGlobal... calls, so the graph doesn't need these as properties

#ifndef RUNE_RANGE_FIELD_INCLUDED
#define RUNE_RANGE_FIELD_INCLUDED

//has to match MaxShownRanges in RuneRangeOverlayObject.cs
#define RUNE_RANGE_MAX 32

//the field's flat plane: its center, and its right / forward directions (world space, absolute)
float4 _RuneFieldCenter;
float4 _RuneFieldRight;
float4 _RuneFieldForward;

//one entry per range being shown. xy = spot on the field plane in meters, z = 1 if the placing rune is in this range (0 if only approaching), w = range in meters
float4 _RuneRangeData[RUNE_RANGE_MAX];
//0-1 strength of each range (fades in as the placing rune gets close)
float _RuneRangeIntensity[RUNE_RANGE_MAX];
//how many entries in the arrays are used this frame
float _RuneRangeCount;
//how far the approach halo reaches, as a multiple of the range (1.5 = 150%)
float _RuneApproachRatio;

//function that turns a world position into a spot on the rune field's plane, in meters (the core is 0, 0)
//WorldPos      - Position node set to ABSOLUTE World (HDRP's plain World is camera relative and would slide around)
//FieldPosition - the spot on the field plane. Also handy as UVs for noise, so noise stays stuck to the floor
void RuneFieldPosition_float(float3 WorldPos, out float2 FieldPosition)
{
    float3 offset = WorldPos - _RuneFieldCenter.xyz;
    FieldPosition = float2(dot(offset, _RuneFieldRight.xyz), dot(offset, _RuneFieldForward.xyz));
}

//function that works out which range bands this spot on the field is in
//overlapping ranges merge with max(), so there are no bright overlap blobs. Every output is multiplied by the range's fade-in (0 to 1 as the placing rune gets close)
//FieldPosition - from RuneFieldPosition above (offset it with noise first for wobbly edges, so every output wobbles together)
//EdgeSoftness  - how wide the feather is on InRange and Approach, in meters
//EdgeWidth     - how thick the Edge line is, in meters
//InRange       - 0 at the range's perimeter, rising to 1 at EdgeSoftness inward, then 1 all the way in. The placing rune would bridge to that rune from here
//Approach      - the same feather for the approach circle (150% with the default Approach Ratio), only outside InRange
//Edge          - 1 on a line EdgeWidth thick, centered on the merged range perimeter, crisp sides. Only around the outside of the merged shape
//                only ranges the placing rune is actually in count toward this line, so a range it's only approaching doesn't cut into it
//ApproachMask  - the approach circle with no feathering: 1 inside, 0 outside, crisp. Covers the range too, so it masks the whole effect
void RuneRangeField_float(float2 FieldPosition, float EdgeSoftness, float EdgeWidth, out float InRange, out float Approach, out float Edge, out float ApproachMask)
{
    InRange = 0;
    Approach = 0;
    Edge = 0;
    ApproachMask = 0;

    float softness = max(EdgeSoftness, 0.0001);
    float halfWidth = max(EdgeWidth, 0) * 0.5;
    int count = (int)_RuneRangeCount;

    float closestEdge = 100000; //distance to the merged range perimeter (negative = inside), used for one clean Edge line
    float closestIntensity = 0; //fade-in of the range that perimeter belongs to

    for (int i = 0; i < RUNE_RANGE_MAX; i++)
    {
        if (i >= count) break;

        float4 data = _RuneRangeData[i];
        float intensity = _RuneRangeIntensity[i];
        float haloRadius = data.w * _RuneApproachRatio;
        float distanceToRune = distance(FieldPosition, data.xy);

        //how far past each circle's perimeter the spot is, in meters. Negative means inside
        float pastEdge = distanceToRune - data.w;
        float pastHalo = distanceToRune - haloRadius;

        //smoothstep makes a soft ramp: 1 at EdgeSoftness inside the perimeter, 0 on the perimeter
        float inside = 1 - smoothstep(-softness, 0, pastEdge);
        float halo = 1 - smoothstep(-softness, 0, pastHalo);

        InRange = max(InRange, inside * intensity);
        Approach = max(Approach, halo * intensity);

        //step(a, b) is 1 when b >= a: here 1 inside the approach circle, 0 outside, hard edge
        ApproachMask = max(ApproachMask, step(distanceToRune, haloRadius) * intensity);

        //data.z is 1 only for ranges the placing rune is in. Approached ranges are left out of the perimeter line
        if (data.z > 0.5 && pastEdge < closestEdge)
        {
            closestEdge = pastEdge;
            closestIntensity = intensity;
        }
    }

    //the line follows the merged shape: inside an overlap some range is further "inside", so there's no line there
    if (count > 0) Edge = step(abs(closestEdge), halfWidth) * closestIntensity;

    //the halo only shows outside the filled area
    Approach *= 1 - InRange;
}

#endif
