using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnitType
{
    //Which side a unit fights for. Units on different teams are hostile to each other, so Fiends and Lumina fight each other as well as the player
    //Order matters: Unity saves enums as numbers, so Fiend took over Enemy's spot (1) and every old enemy became a Fiend. Add new teams at the end
    public enum Team { Player, Fiend, Lumina }

    //What kind of unit it is. Some target rules care about this (like the player's empty body)
    public enum Role { PlayerBody, Shade, Enemy }

    //How important a target is, lowest to highest. A higher group always wins over a lower one, no matter the distance
    public enum TargetTier { None, VacantBody, Normal, Attacker }
}
