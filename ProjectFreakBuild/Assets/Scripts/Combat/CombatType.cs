//Every enum value has an explicit number (= 3), so values can be added later without shifting saved assets. Same rule as DamageType.
namespace CombatType
{
    //where an attack or ability step is in its timing. Animations (and the greybox telegraph) listen for changes later
    public enum Phase
    {
        None = 0,     //not attacking
        WindUp = 1,   //getting ready, can't hit yet. The warning the player reacts to
        Active = 2,   //can hit
        Recovery = 3  //finished hitting, the opening to punish
    }
}
