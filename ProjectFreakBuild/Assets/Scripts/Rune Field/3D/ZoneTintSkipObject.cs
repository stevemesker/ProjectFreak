using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//marker: renderers on this object, or under it, stay on the default light layer even though they're under a zone (see Zone3DObject.ApplyZoneLayer)
//so the zone's open / frozen / locked light doesn't tint them. For glowing crystals, magic bits, anything that should look the same in every state
//it does nothing on its own, the zone just checks for it
[DisallowMultipleComponent]
public class ZoneTintSkipObject : MonoBehaviour
{
}
