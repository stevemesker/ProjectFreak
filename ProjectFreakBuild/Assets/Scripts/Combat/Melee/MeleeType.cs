using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeleeType
{
    //How a swing shape grows over its active time
    //Angle: the arc sweeps sideways from start to end angle (slashes, spins)
    //Reach: the arc grows outward from the wielder (thrusts)
    //Instant: the whole shape hits at once (slams, chops)
    public enum SweepMode { Angle, Reach, Instant }
}
